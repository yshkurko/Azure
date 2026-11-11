# Урок 13. Контейнеры и Azure Container Registry

## Цель
Понять Dockerfile курса, собрать образы локально и в облаке (ACR Tasks).

## Теория
- **Образ** — неизменяемый «снимок» приложения с зависимостями. **Контейнер** — запущенный образ.
- **Multi-stage build**: стадия `build` (SDK, ~800 MB) компилирует, стадия `final` (runtime, ~100 MB) только запускает.
- **Registry** — хранилище образов. **ACR Basic** ~0.17 USD/сутки, 10 GB.
- **ACR Tasks** (`az acr build`) — сборка в облаке, без локального Docker. Платится посекундно (центы).
- Узлы AKS скачивают образы из ACR по Managed Identity (роль **AcrPull**) — без паролей.

## 1. Разбор Dockerfile

Откройте [src/Dockerfile](../src/Dockerfile):
- один файл для трёх сервисов: `--build-arg PROJECT=...`;
- сначала копируются только `.csproj` и делается `restore` — слой кэшируется;
- `USER $APP_UID` — контейнер работает **не от root**;
- порт **8080** (по умолчанию для .NET 8+ контейнеров).

[src/shop-web/Dockerfile](../src/shop-web/Dockerfile): Node собирает Angular → `nginx-unprivileged` раздаёт статику
и проксирует `/api/products` и `/api/orders` на сервисы (адреса — переменные окружения в шаблоне nginx).

## 2. Локальная сборка

```powershell
cd src
docker build -f Dockerfile --build-arg PROJECT=Shop.Catalog.Api -t shop-catalog-api:dev .
docker images shop-*
docker history shop-catalog-api:dev
```

Это же делает `docker compose up --build` из урока 03.

## 3. ACR (временный, только на этот урок)

```powershell
$SUFFIX = "abc12"   # ваш суффикс
$ACR = "acrshop$SUFFIX"
az group create -n rg-shop-acr -l swedencentral --tags course=azure-learning
az acr create -g rg-shop-acr -n $ACR --sku Basic --admin-enabled false --tags course=azure-learning

# Вариант А: сборка в облаке (не нужен локальный Docker)
az acr build -r $ACR -t shop-catalog-api:v1 --build-arg PROJECT=Shop.Catalog.Api -f Dockerfile .

# Вариант Б: push локального образа
az acr login -n $ACR
docker tag shop-catalog-api:dev "$ACR.azurecr.io/shop-catalog-api:dev"
docker push "$ACR.azurecr.io/shop-catalog-api:dev"

az acr repository list -n $ACR -o table
az acr repository show-tags -n $ACR --repository shop-catalog-api -o table
cd ..
```

## Очистка

```powershell
az group delete -n rg-shop-acr --yes --no-wait
```
В уроке 14 ACR создаётся Terraform'ом заново.

## Вопросы для самопроверки
1. Зачем копировать `.csproj` отдельно перед `COPY . .`?
2. Почему контейнер не должен работать от root?
3. Чем `az acr build` отличается от `docker push`?
