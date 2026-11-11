# Урок 01. Инструменты

## Цель
Установить всё необходимое и войти в Azure из командной строки.

## Уже установлено
- .NET 10 SDK — проверка: `dotnet --version`
- Docker Desktop — проверка: `docker version` (Docker Desktop должен быть **запущен**)
- kubectl — проверка: `kubectl version --client`

## Вход в GitHub в VS Code

Нужен для GitHub Copilot, синхронизации настроек и урока 15 (CI/CD):

1. Нажмите иконку **аккаунта** (или шестерёнку) в левом нижнем углу VS Code.
2. Выберите **Sign in to Sync Settings** или пункт **GitHub**.
3. В браузере подтвердите **Authorize Visual Studio Code**.
4. Вернитесь в редактор — аккаунт подключён.

> На компьютере может быть сохранён вход другого GitHub-аккаунта. Для этого репозитория имя пользователя
> указано прямо в адресе remote (`https://yshkurko@github.com/...`), поэтому Git спросит вход именно для него.

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

### Ошибка «running scripts is disabled on this system»

Если `npm` или `ng` в PowerShell падают с этой ошибкой — разрешите запуск подписанных скриптов для текущего пользователя:

```powershell
Set-ExecutionPolicy RemoteSigned -Scope CurrentUser
# подтвердите: Y + Enter
Get-ExecutionPolicy -List
npm --version
```

`-Scope CurrentUser` обычно не требует прав администратора; если всё же требует — выполните команду
в PowerShell, запущенном **от имени администратора**, затем вернитесь в обычный терминал.

### Альтернатива: Azure Cloud Shell
Команды `az` из уроков можно выполнять и в Cloud Shell (иконка **`>_`** в портале) — там Azure CLI, Terraform
и kubectl уже установлены. Выбирайте **No storage account required**, чтобы не создавать платный Storage.
Но Docker и сборка .NET/Angular в Cloud Shell недоступны — основная работа идёт локально.

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
             "Microsoft.ManagedIdentity","Microsoft.CloudShell"
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
