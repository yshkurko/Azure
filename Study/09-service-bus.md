# Урок 09. Очереди: Azure Service Bus

## Цель
Перевести обмен сообщениями на Service Bus, не меняя код (только настройки), и разобраться с dead-letter очередью.

## Теория: Storage Queue vs Service Bus

| | Storage Queue | Service Bus Basic | Service Bus Standard | Premium |
|---|---|---|---|---|
| Цена | ≈0 | ~0.05 USD / 1 млн операций | ~10 USD/мес + операции | от ~670 USD/мес |
| Размер сообщения | 64 KB | 256 KB | 256 KB | до 100 MB |
| Получение | опрос (poll) | push / long-poll | push / long-poll | push |
| Dead-letter | вручную (наш `-poison`) | **встроено** | встроено | встроено |
| Порядок (FIFO), sessions | нет | нет | да | да |
| Topics / subscriptions (pub/sub) | нет | **нет** | да | да |
| Дубли (duplicate detection), транзакции | нет | нет | да | да |
| Private Endpoint / VNet | да | нет | нет | да |

Мы используем **Basic** — его хватает для очереди «точка-точка». Topics (одно сообщение → много подписчиков)
изучаем теоретически: они понадобились бы, если бы на `OrderSubmitted` реагировали ещё «Склад» и «Email».

- **Peek-lock**: получатель блокирует сообщение, затем `Complete` (удалить) или `Abandon` (вернуть).
- **MaxDeliveryCount** (у нас 5): после стольких неудач Service Bus сам переносит сообщение в `orders/$deadletterqueue`.
- `--disable-local-auth` отключает SAS-ключи: доступ только через Entra ID.

## 1. Namespace и очередь

```powershell
. .\Study\scripts\env.ps1

az servicebus namespace create -g $RG -n $SB -l $LOC --sku Basic --disable-local-auth true `
  --tags course=azure-learning
az servicebus queue create -g $RG --namespace-name $SB -n orders `
  --max-delivery-count 5 --enable-dead-lettering-on-message-expiration true

$SB_ID = az servicebus namespace show -g $RG -n $SB --query id -o tsv
az role assignment create --assignee $ME_ID --role "Azure Service Bus Data Owner" --scope $SB_ID
```

## 2. Права и переключение приложений

```powershell
$ORDERS_PID = az webapp identity show -g $RG -n $APP_ORDERS --query principalId -o tsv
$PROC_PID   = az webapp identity show -g $RG -n $APP_PROCESSOR --query principalId -o tsv

az role assignment create --assignee-object-id $ORDERS_PID --assignee-principal-type ServicePrincipal `
  --role "Azure Service Bus Data Sender" --scope $SB_ID
az role assignment create --assignee-object-id $PROC_PID --assignee-principal-type ServicePrincipal `
  --role "Azure Service Bus Data Receiver" --scope $SB_ID

foreach ($app in $APP_ORDERS, $APP_PROCESSOR) {
  az webapp config appsettings set -g $RG -n $app --settings `
    "Messaging__Provider=ServiceBus" `
    "Messaging__ServiceBus__FullyQualifiedNamespace=$SB.servicebus.windows.net"
}
```

Изменение App Settings перезапускает приложение. Главная страница обработчика покажет провайдер:
`Invoke-RestMethod "https://$APP_PROCESSOR.azurewebsites.net/"`.

## Проверка
1. Оформите заказ во фронтенде → статус `Completed`.
2. Portal → Service Bus → `orders` → **Service Bus Explorer** → **Peek from start** (остановив обработчик, увидите сообщения).
3. Метрики: Service Bus → **Metrics** → *Incoming Messages*, *Outgoing Messages*.

## Упражнение: dead-letter
1. Остановите обработчик: `az webapp stop -g $RG -n $APP_PROCESSOR`.
2. В **Service Bus Explorer → Send messages** отправьте тело `{"orderId":"bad"}` (Content type `application/json`).
3. Запустите обработчик. Через 5 попыток сообщение окажется в **Dead-letter** (вкладка в Explorer),
   с причиной `MaxDeliveryCountExceeded`.

## Вернуться на Storage Queue
Просто поменяйте `Messaging__Provider=StorageQueue` — код поддерживает оба варианта.

## Очистка
Basic namespace без трафика стоит ≈0 — оставьте до урока 10.

## Вопросы для самопроверки
1. Когда стоит выбрать Service Bus вместо Storage Queue?
2. Чем `Abandon` отличается от `Complete` и от `DeadLetter`?
3. Почему Topics недоступны в Basic и когда они нужны?
