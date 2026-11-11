# Урок 01. Инструменты

## Цель
Установить всё необходимое и войти в Azure из командной строки.

## Уже установлено
- .NET 10 SDK — проверка: `dotnet --version`
- Docker Desktop — проверка: `docker version` (Docker Desktop должен быть **запущен**)
- kubectl — проверка: `kubectl version --client`

## Установка недостающего (через winget)

Откройте PowerShell **от имени обычного пользователя** и выполните:

```powershell
winget install --id Microsoft.AzureCLI -e
winget install --id Hashicorp.Terraform -e
winget install --id OpenJS.NodeJS.LTS -e
```

Закройте и заново откройте терминал (и VS Code), чтобы обновился `PATH`. Затем:

```powershell
az version
terraform version
node --version     # ожидается v22.x или новее LTS
npm --version
npm install -g @angular/cli
ng version
```

## Расширения VS Code (рекомендуется)
- **C# Dev Kit** — .NET
- **Angular Language Service**
- **HashiCorp Terraform**
- **Kubernetes** (ms-kubernetes-tools)
- **Azure Resources** и **Azure App Service**
- **REST Client** или встроенная поддержка `.http` файлов

## Вход в Azure

```powershell
az login
az account list -o table
# если подписок несколько — выберите нужную:
az account set --subscription "<имя или id>"
az account show -o table
```

## Регистрация провайдеров ресурсов

В новой подписке некоторые сервисы нужно «включить» один раз (бесплатно):

```powershell
$providers = "Microsoft.Network","Microsoft.Sql","Microsoft.Web","Microsoft.Storage",
             "Microsoft.ServiceBus","Microsoft.OperationalInsights","Microsoft.Insights",
             "Microsoft.ContainerRegistry","Microsoft.ContainerService","Microsoft.KeyVault",
             "Microsoft.ManagedIdentity"
foreach ($p in $providers) { az provider register --namespace $p }
az provider list --query "[?registrationState=='Registered'].namespace" -o tsv
```

## Значения по умолчанию для CLI

```powershell
az config set defaults.location=swedencentral
az config set core.output=table
```

## Проверка
- `az account show` показывает вашу подписку.
- `terraform version`, `node --version`, `ng version` работают.
- Выполнен урок 00 (бюджет создан, тип подписки известен).

## Очистка
Нечего удалять.

## Вопросы для самопроверки
1. Как переключиться между подписками в Azure CLI?
2. Зачем регистрировать resource providers?
