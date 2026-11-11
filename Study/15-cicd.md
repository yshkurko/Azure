# Урок 15. CI/CD в GitHub Actions

## Цель
Автоматически проверять каждый коммит и разворачивать приложение в AKS по кнопке — без паролей Azure в GitHub.

## Теория
- **CI** ([.github/workflows/ci.yml](../.github/workflows/ci.yml)) — на каждый push/PR: сборка .NET, сборка Angular,
  `terraform fmt -check` и `validate`. Бесплатно (минуты GitHub Actions для публичных репо не ограничены,
  для приватных — 2000 мин/мес бесплатно).
- **CD** ([.github/workflows/deploy-aks.yml](../.github/workflows/deploy-aks.yml)) — ручной запуск (`workflow_dispatch`):
  `az acr build` → подстановка значений в kustomization → `kubectl apply -k`.
- **OIDC (federated credentials)**: GitHub выдаёт job'у короткоживущий токен, Entra ID доверяет ему
  для конкретного репозитория/ветки и выдаёт токен Azure. **Нет секретов, которые могут утечь.**

## 1. Репозиторий на GitHub

```powershell
cd <репозиторий>
git add .
git commit -m "Azure learning: initial"
gh repo create azure-learning --private --source . --push   # или через сайт GitHub
```

Вкладка **Actions** — workflow **CI** должен пройти.

## 2. Identity для GitHub (бесплатно, переживает удаление стенда)

```powershell
$GH_REPO = "<ваш-логин>/azure-learning"
az group create -n rg-shop-cicd -l swedencentral --tags course=azure-learning
az identity create -g rg-shop-cicd -n id-github-deploy
$GH_CLIENT_ID = az identity show -g rg-shop-cicd -n id-github-deploy --query clientId -o tsv
$GH_PRINCIPAL = az identity show -g rg-shop-cicd -n id-github-deploy --query principalId -o tsv

az identity federated-credential create -g rg-shop-cicd --identity-name id-github-deploy -n github-main `
  --issuer https://token.actions.githubusercontent.com `
  --subject "repo:$($GH_REPO):ref:refs/heads/main" `
  --audiences api://AzureADTokenExchange
```

## 3. Развернуть стенд и выдать права

Выполните шаги 1–2 урока 14 (`terraform apply` и пользователь SQL). Затем:

```powershell
$RG_ID = az group show -n rg-shop-aks --query id -o tsv
az role assignment create --assignee-object-id $GH_PRINCIPAL --assignee-principal-type ServicePrincipal `
  --role Contributor --scope $RG_ID
```

> Роль выдаётся **на группу стенда**, а не на подписку: принцип минимальных привилегий.
> После `terraform destroy` назначение исчезнет вместе с группой — при следующем стенде выдайте снова.

## 4. Переменные GitHub

Repository → **Settings → Secrets and variables → Actions → Variables** (не Secrets — значения не секретны):

```powershell
cd src\infra\terraform\envs\aks
@{
  AZURE_CLIENT_ID             = $GH_CLIENT_ID
  AZURE_TENANT_ID             = (az account show --query tenantId -o tsv)
  AZURE_SUBSCRIPTION_ID       = (az account show --query id -o tsv)
  AKS_RESOURCE_GROUP          = (terraform output -raw resource_group_name)
  AKS_NAME                    = (terraform output -raw aks_name)
  ACR_NAME                    = (terraform output -raw acr_name)
  SQL_SERVER_FQDN             = (terraform output -raw sql_server_fqdn)
  SERVICEBUS_FQDN             = (terraform output -raw servicebus_fqdn)
  STORAGE_ACCOUNT             = (terraform output -raw storage_account_name)
  WORKLOAD_IDENTITY_CLIENT_ID = (terraform output -raw workload_identity_client_id)
}.GetEnumerator() | ForEach-Object { gh variable set $_.Key --body $_.Value --repo $GH_REPO }
```

## 5. Запуск
**Actions → Deploy to AKS → Run workflow**. После завершения:
```powershell
kubectl -n shop get svc web
kubectl -n shop get deploy -o wide     # образы с тегом = SHA коммита
```

## Упражнения
1. Измените текст на главной странице Angular → commit → push → CI → Deploy → обновлённая страница.
2. Посмотрите, как `rollout status` ждёт готовности подов (readiness probe).
3. (Продвинутое) Добавьте job с `terraform plan` в CI: потребуется ещё одна federated credential для `pull_request`
   и роль **Reader** на подписку.

## Очистка — обязательно
```powershell
cd src\infra\terraform\envs\aks
terraform destroy
```
`rg-shop-cicd` (identity) и `rg-shop-tfstate` бесплатны/почти бесплатны — можно оставить.
В конце курса: `.\Study\scripts\cleanup.ps1 -IncludeTfState`.

## Что дальше
- Azure Container Apps — «Kubernetes без Kubernetes», с масштабированием до нуля.
- Azure Functions с триггером Service Bus вместо Order Processor.
- Entra ID аутентификация пользователей в Angular (MSAL) и защита API.
- Helm, GitOps (Flux) для AKS.
- Transactional Outbox для надёжной отправки сообщений.

## Вопросы для самопроверки
1. Почему OIDC безопаснее, чем секрет сервисного принципала в GitHub Secrets?
2. Что ограничивает `subject` в federated credential?
3. Почему права выдаются на группу ресурсов, а не на подписку?
