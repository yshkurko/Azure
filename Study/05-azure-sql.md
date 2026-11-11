# Урок 05. Azure SQL Database

## Цель
Создать сервер и базу Azure SQL **по бесплатному предложению (free offer)**, разрешить вход только через Entra ID,
подключить базу к VNet через Private Endpoint и применить миграции.

## Теория
- **Logical server** (`sql-shop-xxx.database.windows.net`) — точка входа и настройки безопасности. Сам по себе бесплатен.
- **Database** — платится по модели:
  - DTU (Basic/Standard/Premium) — простая, Basic ~5 USD/мес;
  - vCore (General Purpose, Serverless) — гибкая, Serverless умеет **засыпать** (auto-pause);
  - **Free offer** — бесплатная база Serverless: 100 000 vCore-секунд и 32 GB в месяц, до 10 баз на подписку.
    При исчерпании лимита база **ставится на паузу** до следующего месяца (если выбрано AutoPause) — денег не снимают.
- **Entra-only authentication** — нет SQL-логинов/паролей; приложение входит по Managed Identity.
- **Firewall** — список публичных IP, которым можно подключаться. **Private Endpoint** — доступ изнутри VNet.

## 1. Сервер с Entra-only аутентификацией

```powershell
. .\Study\scripts\env.ps1

az sql server create -g $RG -n $SQL_SERVER -l $LOC `
  --enable-ad-only-auth `
  --external-admin-principal-type User `
  --external-admin-name $ME_UPN `
  --external-admin-sid $ME_ID `
  --minimal-tls-version 1.2

az resource tag --tags course=azure-learning -g $RG -n $SQL_SERVER --resource-type Microsoft.Sql/servers
```

## 2. Бесплатная база (free offer)

```powershell
az sql db create -g $RG -s $SQL_SERVER -n $SQL_DB `
  --edition GeneralPurpose --family Gen5 --capacity 2 --compute-model Serverless `
  --use-free-limit --free-limit-exhaustion-behavior AutoPause `
  --auto-pause-delay 60 --backup-storage-redundancy Local `
  --tags course=azure-learning
```

Проверьте в Portal: **SQL database → Overview** — должна быть плашка **Free amount remaining**.

> Если free offer недоступен (уже использован лимит баз), замените на самый дешёвый вариант
> `--edition Basic --capacity 5` (~5 USD/мес) и удаляйте базу после занятий.

## 3. Доступ с вашего компьютера

```powershell
az sql server firewall-rule create -g $RG -s $SQL_SERVER -n my-ip `
  --start-ip-address $MY_IP --end-ip-address $MY_IP
```

Портал: **shopdb → Query editor (preview)** → войти через Entra ID → выполните `SELECT @@VERSION;`.

## 4. Миграции с локального компьютера

Приложения применяют миграции при старте, поэтому достаточно запустить Catalog API локально,
указав строку подключения к Azure (токен берётся из `az login`):

```powershell
cd src
$env:ConnectionStrings__ShopDb = "Server=tcp:$SQL_SERVER.database.windows.net,1433;Database=$SQL_DB;Authentication=Active Directory Default;Encrypt=True"
dotnet run --project Shop.Catalog.Api
# в другом терминале: Invoke-RestMethod http://localhost:5101/api/products
# Ctrl+C, затем то же для Orders (схема orders):
dotnet run --project Shop.Orders.Api
Remove-Item Env:\ConnectionStrings__ShopDb
cd ..
```

В Query editor: `SELECT * FROM catalog.Products;` и `SELECT name FROM sys.schemas;`.

## 5. Private Endpoint (доступ из VNet)

```powershell
$SQL_ID = az sql server show -g $RG -n $SQL_SERVER --query id -o tsv

az network private-endpoint create -g $RG -n pe-sql-shop `
  --vnet-name $VNET --subnet snet-pe `
  --private-connection-resource-id $SQL_ID --group-id sqlServer `
  --connection-name sql-connection --tags course=azure-learning

az network private-dns zone create -g $RG -n privatelink.database.windows.net
az network private-dns link vnet create -g $RG -n sql-dns-link `
  --zone-name privatelink.database.windows.net --virtual-network $VNET --registration-enabled false
az network private-endpoint dns-zone-group create -g $RG --endpoint-name pe-sql-shop `
  -n sql-zone-group --private-dns-zone privatelink.database.windows.net --zone-name sql
```

Проверка: **privatelink.database.windows.net → Recordsets** — A-запись с адресом 10.20.2.x.

> Публичный доступ оставляем включённым **только для вашего IP** — чтобы работал Query editor
> и создание пользователей. В «боевой» системе его отключают: `az sql server update --enable-public-network false`.

## Проверка
- Query editor показывает таблицы в схемах `catalog` и `orders`.
- `nslookup $SQL_SERVER.database.windows.net` с вашего компьютера отдаёт публичный адрес
  (через CNAME `*.privatelink.*`), а изнутри VNet — частный.

## Очистка
- **Free offer** база бесплатна — можно оставить до урока 10.
- **Private Endpoint** платный (~0.24 USD/сутки). Если перерыв больше пары дней:
  ```powershell
  az network private-endpoint delete -g $RG -n pe-sql-shop
  ```
  (в уроке 06 его можно создать заново той же командой).

## Вопросы для самопроверки
1. Что произойдёт, когда закончится бесплатный лимит базы?
2. Почему у приложения нет пароля к базе?
3. Как приложение внутри VNet узнаёт частный IP базы?
