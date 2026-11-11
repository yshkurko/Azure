# Общие переменные для уроков 04–10 (ручное создание ресурсов).
# Использование в PowerShell из корня репозитория:  . .\Study\scripts\env.ps1
# Имена SQL, Storage, Web App и т.п. глобально уникальны в Azure — поэтому нужен свой суффикс.

$SUFFIX = "abc12"   # ЗАМЕНИТЕ: 5 строчных латинских букв/цифр, например инициалы + цифры

$LOC = "swedencentral"
$RG = "rg-shop-manual"

$VNET = "vnet-shop"
$SQL_SERVER = "sql-shop-$SUFFIX"
$SQL_DB = "shopdb"
$STORAGE = "stshop$SUFFIX"
$SB = "sb-shop-$SUFFIX"
$PLAN = "asp-shop"
$APP_CATALOG = "app-shop-catalog-$SUFFIX"
$APP_ORDERS = "app-shop-orders-$SUFFIX"
$APP_PROCESSOR = "app-shop-processor-$SUFFIX"
$SWA = "stapp-shop-$SUFFIX"
$LAW = "log-shop"
$APPI = "appi-shop"
$KV = "kv-shop-$SUFFIX"

$ME_UPN = az ad signed-in-user show --query userPrincipalName -o tsv
$ME_ID = az ad signed-in-user show --query id -o tsv
$MY_IP = (Invoke-RestMethod -Uri "https://api.ipify.org").Trim()

Write-Host "Подписка: $(az account show --query name -o tsv)"
Write-Host "Пользователь: $ME_UPN, IP: $MY_IP, суффикс: $SUFFIX"
