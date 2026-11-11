# Урок 12. Terraform: вся PaaS-инфраструктура

## Цель
Одной командой создать всё, что в уроках 04–10 делалось вручную, развернуть код и одной командой всё удалить.

## Что создаётся (`src/infra/terraform/envs/paas`)
- `rg-shop-paas`, VNet с подсетями и NSG
- SQL Server (Entra-only) + база **Basic** (~0.16 USD/сутки) + Private Endpoint + Private DNS
- Storage Account + очереди `orders`, `orders-poison`; Service Bus Basic (если `messaging_provider = "ServiceBus"`)
- Log Analytics (лимит 0.1 GB/день) + Application Insights
- App Service Plan **B1** + 3 Web App (VNet Integration, Managed Identity, health check, app settings)
- Static Web App (Free)
- Назначения ролей для очередей

> Почему в Terraform база **Basic**, а не free offer: стенд живёт несколько часов и удаляется,
> а free offer — один на подписку и уже может быть занят базой из урока 05.

**Стоимость стенда:** ~0.03 USD/час (план B1 + PE + SQL Basic). За вечер — меньше 0.2 USD.

## 1. Подготовка

```powershell
cd src\infra\terraform\envs\paas
Copy-Item terraform.tfvars.example terraform.tfvars
# Заполните terraform.tfvars:
az account show --query id -o tsv                          # subscription_id
az ad signed-in-user show --query userPrincipalName -o tsv # sql_admin_login
(Invoke-RestMethod https://api.ipify.org)                  # client_ip
```

`backend.hcl` — из урока 11.

## 2. Создание

```powershell
terraform init -backend-config=backend.hcl
terraform plan -out tfplan
terraform apply tfplan
terraform output
```

Создание занимает 5–10 минут.

## 3. Пользователи SQL

```powershell
terraform output -raw sql_create_users_script
```
Выполните скрипт в Portal → SQL database → **Query editor** (вход под своей учётной записью).

## 4. Публикация кода

```powershell
$RG = terraform output -raw resource_group_name
$apps = terraform output -json web_app_names | ConvertFrom-Json
$urls = terraform output -json web_app_urls | ConvertFrom-Json
$SWA_TOKEN = terraform output -raw static_web_app_deployment_token
Push-Location ..\..\..\..   # папка src

function Publish-App($project, $app) {
  dotnet publish $project -c Release -o "out/$project"
  Compress-Archive -Path "out/$project/*" -DestinationPath "out/$project.zip" -Force
  az webapp deploy -g $RG -n $app --src-path "out/$project.zip" --type zip
}
Publish-App Shop.Catalog.Api    $apps.catalog
Publish-App Shop.Orders.Api     $apps.orders
Publish-App Shop.OrderProcessor $apps.processor

cd shop-web
npm install
npx ng build --configuration production
@{ catalogApiUrl = $urls.catalog; ordersApiUrl = $urls.orders } | ConvertTo-Json |
  Set-Content dist/shop-web/browser/config.json
npx @azure/static-web-apps-cli deploy ./dist/shop-web/browser --deployment-token $SWA_TOKEN --env production
Pop-Location
```

## Проверка
`terraform output static_web_app_url` → открыть → оформить заказ → статус `Completed`.

## Упражнения
1. Переключите очереди на Service Bus: в `terraform.tfvars` `messaging_provider = "ServiceBus"` →
   `terraform plan` (посмотрите, что изменится) → `apply`.
2. Найдите в плане, какие ресурсы зависят от `module.network` (`terraform graph` — граф зависимостей).
3. Поменяйте в Portal вручную любую App Setting и посмотрите, как `terraform plan` её «откатывает».

## Очистка — обязательно

```powershell
terraform destroy
az group list -o table     # rg-shop-paas должна исчезнуть
```

## Вопросы для самопроверки
1. Как Terraform узнаёт, в каком порядке создавать ресурсы?
2. Зачем `random_string` в именах?
3. Почему пользователей SQL создаём не Terraform'ом?
