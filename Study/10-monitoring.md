# Урок 10. Мониторинг: Application Insights и Log Analytics

## Цель
Видеть запросы, ошибки, зависимости (SQL, HTTP, очереди) и логи всех сервисов в одном месте,
не рискуя получить счёт за объём телеметрии.

## Теория
- **Log Analytics Workspace** — хранилище логов, язык запросов **KQL**. Платим за приём данных (~2.3 USD/GB);
  первые 5 GB в месяц бесплатны. Ставим **дневной лимит** (daily cap).
- **Application Insights** (workspace-based) — APM поверх Log Analytics: запросы, зависимости, исключения, карта приложения.
- В коде: `AddShopTelemetry()` в `Shop.Shared/Hosting` включает OpenTelemetry + Azure Monitor,
  **только если** задана переменная `APPLICATIONINSIGHTS_CONNECTION_STRING`. Локально телеметрия не отправляется.

## 1. Ресурсы с лимитами

```powershell
. .\Study\scripts\env.ps1

az monitor log-analytics workspace create -g $RG -n $LAW -l $LOC --retention-time 30 `
  --quota 0.1 --tags course=azure-learning

$LAW_ID = az monitor log-analytics workspace show -g $RG -n $LAW --query id -o tsv
az extension add --name application-insights --upgrade
az monitor app-insights component create -g $RG -a $APPI -l $LOC --workspace $LAW_ID `
  --application-type web --tags course=azure-learning
az monitor app-insights component billing update -g $RG -a $APPI --cap 0.1

$AI_CS = az monitor app-insights component show -g $RG -a $APPI --query connectionString -o tsv
foreach ($app in $APP_CATALOG, $APP_ORDERS, $APP_PROCESSOR) {
  az webapp config appsettings set -g $RG -n $app --settings "APPLICATIONINSIGHTS_CONNECTION_STRING=$AI_CS"
}
```

`--quota 0.1` — 100 MB в день: когда лимит достигнут, приём данных прекращается до следующих суток.

## 2. Что посмотреть
Сделайте несколько заказов, подождите 2–5 минут.

- **Application Map** — граф: Angular → Orders API → Catalog API / SQL / очередь.
- **Live Metrics** — запросы в реальном времени.
- **Transaction search** — найдите `POST /api/orders` и посмотрите цепочку зависимостей.
- **Failures** — ошибки (например, попытки из урока 08/09 с битым сообщением).

## 3. KQL

App Insights → **Logs**:

```kusto
// Самые медленные запросы
requests
| where timestamp > ago(1h)
| summarize count(), avg(duration), percentile(duration, 95) by name, cloud_RoleName
| order by percentile_duration_95 desc

// Логи обработчика заказов
traces
| where cloud_RoleName contains "OrderProcessor"
| where message has "processed"
| project timestamp, message
| order by timestamp desc

// Зависимости: SQL, HTTP, Service Bus
dependencies
| where timestamp > ago(1h)
| summarize count(), avg(duration) by type, target
```

## 4. Логи App Service без App Insights

```powershell
az webapp log config -g $RG -n $APP_ORDERS --docker-container-logging filesystem
az webapp log tail -g $RG -n $APP_ORDERS
```

## 5. Простой алерт (~0.10 USD/мес за правило метрики)
Portal → App Insights → **Alerts → Create → Alert rule** → сигнал *Failed requests* > 5 за 5 минут → email.

## Очистка — конец ручной части курса

Уроки 04–10 завершены. **Удалите всю группу** — всё это мы воссоздадим Terraform'ом в уроке 12:

```powershell
az group delete -n $RG --yes --no-wait
az keyvault list-deleted -o table
az keyvault purge -n $KV         # освобождает имя Key Vault
```

Через 5–10 минут: `az group list -o table` — группы `rg-shop-manual` нет.
В Cost Management проверьте расходы за последние дни.

## Вопросы для самопроверки
1. Что произойдёт при достижении daily cap?
2. Где посмотреть, сколько времени занял запрос к SQL при создании заказа?
3. Почему телеметрия не отправляется при локальном запуске?
