# Урок 06. API в Azure App Service

## Цель
Разместить Catalog API, Orders API и Order Processor в App Service на Linux, подключить их к VNet
и дать доступ к базе через Managed Identity. Познакомиться с Key Vault.

## Теория
- **App Service Plan** — «сервер», за который платим. На одном плане может жить несколько Web App.
  **B1** — самый дешёвый уровень с VNet Integration и Always On (~0.018 USD/ч).
- **Web App** — приложение на плане. Бесплатно само по себе.
- **Always On** — не даёт приложению «засыпать». Нужен Order Processor, который слушает очередь.
- **Health check** — App Service опрашивает `/health` и перезапускает нездоровые экземпляры.
- **App Settings** — переменные окружения. `ConnectionStrings__ShopDb` = `ConnectionStrings:ShopDb` в .NET.

> ⚠ **План платный, пока существует** — даже если все приложения остановлены.

## 1. План и три приложения

```powershell
. .\Study\scripts\env.ps1
$TAGS = "course=azure-learning"

az appservice plan create -g $RG -n $PLAN --is-linux --sku B1 --tags $TAGS
az webapp list-runtimes --os linux --query "[?contains(@, 'DOTNET')]" -o tsv

foreach ($app in $APP_CATALOG, $APP_ORDERS, $APP_PROCESSOR) {
  az webapp create -g $RG -p $PLAN -n $app --runtime "DOTNETCORE:10.0" --tags $TAGS
  az webapp identity assign -g $RG -n $app
  az webapp update -g $RG -n $app --https-only true
  az webapp config set -g $RG -n $app --always-on true --ftps-state Disabled `
    --min-tls-version 1.2 --generic-configurations healthCheckPath=/health
  az webapp vnet-integration add -g $RG -n $app --vnet $VNET --subnet snet-app
  az webapp config set -g $RG -n $app --vnet-route-all-enabled true
}
```

## 2. Настройки приложений

```powershell
$SQL_CS = "Server=tcp:$SQL_SERVER.database.windows.net,1433;Database=$SQL_DB;Authentication=Active Directory Managed Identity;Encrypt=True"

az webapp config appsettings set -g $RG -n $APP_CATALOG --settings `
  "ConnectionStrings__ShopDb=$SQL_CS"

az webapp config appsettings set -g $RG -n $APP_ORDERS --settings `
  "ConnectionStrings__ShopDb=$SQL_CS" `
  "Services__CatalogApiUrl=https://$APP_CATALOG.azurewebsites.net/"

az webapp config appsettings set -g $RG -n $APP_PROCESSOR --settings `
  "ConnectionStrings__ShopDb=$SQL_CS"
```

Очереди пока не настроены — Orders API упадёт при создании заказа, а Processor при старте.
Это исправим в уроке 08. Сейчас проверяем каталог и чтение заказов.

## 3. Пользователи базы для Managed Identity

Portal → **shopdb → Query editor** (под своей учётной записью) → выполните, подставив имена приложений:

```sql
CREATE USER [app-shop-catalog-abc12] FROM EXTERNAL PROVIDER;
ALTER ROLE db_datareader ADD MEMBER [app-shop-catalog-abc12];
ALTER ROLE db_datawriter ADD MEMBER [app-shop-catalog-abc12];
ALTER ROLE db_ddladmin   ADD MEMBER [app-shop-catalog-abc12];

CREATE USER [app-shop-orders-abc12] FROM EXTERNAL PROVIDER;
ALTER ROLE db_datareader ADD MEMBER [app-shop-orders-abc12];
ALTER ROLE db_datawriter ADD MEMBER [app-shop-orders-abc12];
ALTER ROLE db_ddladmin   ADD MEMBER [app-shop-orders-abc12];

CREATE USER [app-shop-processor-abc12] FROM EXTERNAL PROVIDER;
ALTER ROLE db_datareader ADD MEMBER [app-shop-processor-abc12];
ALTER ROLE db_datawriter ADD MEMBER [app-shop-processor-abc12];
```

> `db_ddladmin` нужен API только потому, что они применяют миграции при старте. В реальных проектах
> миграции запускают отдельным шагом в CI/CD, а приложению дают только чтение/запись.

## 4. Публикация кода (zip deploy)

```powershell
cd src
function Publish-App($project, $app) {
  dotnet publish $project -c Release -o "out/$project"
  Compress-Archive -Path "out/$project/*" -DestinationPath "out/$project.zip" -Force
  az webapp deploy -g $RG -n $app --src-path "out/$project.zip" --type zip
}
Publish-App Shop.Catalog.Api    $APP_CATALOG
Publish-App Shop.Orders.Api     $APP_ORDERS
Publish-App Shop.OrderProcessor $APP_PROCESSOR
cd ..
```

## 5. Проверка

```powershell
Invoke-RestMethod "https://$APP_CATALOG.azurewebsites.net/health"
Invoke-RestMethod "https://$APP_CATALOG.azurewebsites.net/api/products"
Invoke-RestMethod "https://$APP_ORDERS.azurewebsites.net/api/orders"
az webapp log tail -g $RG -n $APP_CATALOG     # Ctrl+C для выхода
```

Убедитесь, что трафик к SQL идёт через Private Endpoint: Portal → Web App → **Advanced Tools (Kudu) → SSH**:
```bash
nslookup sql-shop-abc12.database.windows.net   # должен вернуть 10.20.2.x
```

## 6. Key Vault (знакомство)

У нас нет секретов (всё через Managed Identity), но в реальных проектах они бывают: ключи внешних API и т.п.

```powershell
az keyvault create -g $RG -n $KV -l $LOC --enable-rbac-authorization true --tags course=azure-learning
$KV_ID = az keyvault show -n $KV --query id -o tsv
az role assignment create --assignee $ME_ID --role "Key Vault Secrets Officer" --scope $KV_ID
az keyvault secret set --vault-name $KV -n DemoSecret --value "hello-from-key-vault"

$PRINCIPAL = az webapp identity show -g $RG -n $APP_CATALOG --query principalId -o tsv
az role assignment create --assignee-object-id $PRINCIPAL --assignee-principal-type ServicePrincipal `
  --role "Key Vault Secrets User" --scope $KV_ID
az webapp config appsettings set -g $RG -n $APP_CATALOG --settings `
  "DemoSecret=@Microsoft.KeyVault(VaultName=$KV;SecretName=DemoSecret)"
```

Portal → Web App → **Environment variables** → у `DemoSecret` должен быть зелёный значок **Key vault Reference**.
Key Vault Standard тарифицируется за операции (~0.03 USD за 10 000) — фактически бесплатно.

## Очистка

App Service Plan — главный расход этого урока. Если перерыв больше дня:

```powershell
foreach ($app in $APP_CATALOG, $APP_ORDERS, $APP_PROCESSOR) { az webapp delete -g $RG -n $app }
az appservice plan delete -g $RG -n $PLAN --yes
```

Пересоздать — шаги 1, 2, 4 (пользователей SQL надо удалить и создать заново: `DROP USER [...]`,
т.к. у новых приложений новые identity).

## Вопросы для самопроверки
1. Почему мы не используем бесплатный план F1?
2. Что такое `vnet-route-all-enabled` и зачем он нужен для Private Endpoint?
3. Как приложение получает токен для SQL без пароля?
4. Чем Key Vault reference лучше, чем значение секрета прямо в App Settings?
