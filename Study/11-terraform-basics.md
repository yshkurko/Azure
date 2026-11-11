# Урок 11. Terraform: основы

## Цель
Понять, как Terraform описывает инфраструктуру кодом, научиться циклу `init → plan → apply → destroy`
и настроить удалённое хранение state в Azure.

## Теория
- **IaC (Infrastructure as Code)** — инфраструктура описана в файлах, хранится в Git, создаётся повторяемо.
- **Provider** — плагин для API (у нас `azurerm` и `random`).
- **Resource** — то, что Terraform создаёт (`azurerm_resource_group`). **Data source** — то, что только читает.
- **Variables / outputs / locals** — входы, выходы и вычисляемые значения.
- **Module** — переиспользуемая папка с ресурсами (`src/infra/terraform/modules/*`).
- **State** (`terraform.tfstate`) — «память» Terraform: соответствие кода и реальных ресурсов.
  Может содержать секреты → **не коммитить** (уже в `.gitignore`), хранить удалённо с блокировкой.
- **Plan** — показывает, что будет создано (`+`), изменено (`~`), удалено (`-`) **до** применения. Читайте его всегда.

## 1. Первая конфигурация (5 минут, бесплатно)

Создайте временную папку **вне репозитория**, например `C:\temp\tf-hello`, и файл `main.tf`:

```hcl
terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.0"
    }
  }
}

provider "azurerm" {
  features {}
  subscription_id = var.subscription_id
}

variable "subscription_id" {
  type = string
}

resource "azurerm_resource_group" "hello" {
  name     = "rg-shop-tf-hello"
  location = "swedencentral"
  tags     = { course = "azure-learning" }
}

output "rg_id" {
  value = azurerm_resource_group.hello.id
}
```

```powershell
cd C:\temp\tf-hello
$env:TF_VAR_subscription_id = az account show --query id -o tsv
terraform init
terraform fmt
terraform validate
terraform plan          # прочитайте: 1 to add
terraform apply         # введите yes
terraform state list
terraform show
```

Эксперимент с «дрейфом»: добавьте тег вручную в Portal, затем `terraform plan` — Terraform предложит его убрать.
Измените `tags` в коде → `plan` → `apply` — изменение «на месте» (`~`).
Измените `name` → `plan` покажет **replace** (`-/+`): некоторые свойства нельзя поменять без пересоздания.

```powershell
terraform destroy       # yes
```

## 2. Remote state в Azure Storage

```powershell
cd <репозиторий>\src\infra\terraform\bootstrap
$env:TF_VAR_subscription_id = az account show --query id -o tsv
terraform init
terraform apply
terraform output -raw backend_config
```

Скопируйте вывод в `envs/paas/backend.hcl` и `envs/aks/backend.hcl` (шаблоны — `backend.hcl.example`).

Дайте себе права на данные в контейнере (backend использует Entra ID, а не ключи):
```powershell
$ST = terraform output -raw backend_config | Select-String 'storage_account_name\s*=\s*"(.+)"' | ForEach-Object { $_.Matches[0].Groups[1].Value }
$ST_ID = az storage account show -n $ST --query id -o tsv
az role assignment create --assignee (az ad signed-in-user show --query id -o tsv) `
  --role "Storage Blob Data Contributor" --scope $ST_ID
```

> Группа `rg-shop-tfstate` стоит центы в месяц. **Не удаляйте её**, пока проходите уроки 12–15.
> Bootstrap хранит свой state локально — это нормально, он создаётся один раз.

## 3. Обзор кода курса

```
src/infra/terraform/
  bootstrap/            # state storage
  modules/
    network/            # VNet, подсети (app/pe/aks), NSG
    sql/                # SQL server (Entra-only), база, firewall, Private Endpoint + DNS
    messaging/          # Storage + очереди, Service Bus Basic
    monitoring/         # Log Analytics (daily cap) + App Insights
  envs/
    paas/               # урок 12: App Service + SWA
    aks/                # урок 14: ACR + AKS + Workload Identity
```

Откройте `envs/paas/main.tf` и найдите, какие команды из уроков 04–10 соответствуют каким ресурсам.

## Очистка
- `C:\temp\tf-hello`: `terraform destroy` выполнен.
- `rg-shop-tfstate` — оставляем.

## Вопросы для самопроверки
1. Что будет, если удалить файл state и снова выполнить `apply`?
2. Почему state нельзя коммитить в Git?
3. Как понять по `plan`, что ресурс будет пересоздан?
