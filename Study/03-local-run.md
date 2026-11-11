# Урок 03. Приложение локально

## Цель
Запустить весь магазин на своём компьютере и разобраться в коде, прежде чем переносить его в облако.

## Архитектура кода

- **Shop.Catalog.Api** — `GET /api/products`, `GET /api/products/{id}`. EF Core, схема `catalog`, начальные данные (seed) в миграции.
- **Shop.Orders.Api** — `POST /api/orders`, `GET /api/orders`, `GET /api/orders/{id}`.
  При создании заказа берёт цены из Catalog API по HTTP (цене от клиента доверять нельзя),
  сохраняет заказ в статусе `Pending` и кладёт в очередь сообщение `OrderSubmitted`.
- **Shop.OrderProcessor** — фоновый сервис: читает очередь, переводит заказ в `Processing` → `Completed`.
- **Shop.Shared** — `AddShopMessaging()`: по настройке `Messaging:Provider` подключает Storage Queue или Service Bus.
  Если указана строка подключения — использует её, иначе `DefaultAzureCredential` (Managed Identity).
- **shop-web** — Angular. Адреса API берёт из `/config.json` (пустая строка = тот же хост через прокси).

Обе API применяют миграции при старте (`Database:ApplyMigrationsOnStartup`). Catalog и Orders
живут в одной базе `shopdb`, но в разных схемах и с разными таблицами истории миграций.

## Вариант А: всё в Docker

> Docker Desktop должен быть запущен.

```powershell
cd src
docker compose up -d --build
docker compose ps
```

Откройте http://localhost:8080 → добавьте товары в корзину → оформите заказ → вкладка «Заказы».
Через несколько секунд статус сменится с `Pending` на `Completed` — это поработал Order Processor.

Логи:
```powershell
docker compose logs -f order-processor
```

> При самом первом запуске Catalog и Orders одновременно создают базу — один из них может упасть
> и автоматически перезапуститься (`restart: on-failure`). Это нормально.

Остановить: `docker compose down` (данные SQL сохраняются в volume), полностью: `docker compose down -v`.

## Вариант Б: инфраструктура в Docker, код — из IDE (удобно для отладки)

```powershell
cd src
docker compose up -d sqlserver azurite

# в трёх отдельных терминалах:
dotnet run --project Shop.Catalog.Api      # http://localhost:5101
dotnet run --project Shop.Orders.Api       # http://localhost:5102
dotnet run --project Shop.OrderProcessor   # http://localhost:5103

# фронтенд (Node.js из урока 01):
cd shop-web
npm install
npm start                                  # http://localhost:4200 (прокси на 5101/5102)
```

Проверьте API через файлы `Shop.Catalog.Api.http` и `Shop.Orders.Api.http` (кнопка **Send Request** в VS Code).
OpenAPI-описание: http://localhost:5101/openapi/v1.json.

## Миграции EF Core

Инструмент `dotnet-ef` установлен локально (`src/dotnet-tools.json`):

```powershell
cd src
dotnet tool restore
# пример: новая миграция каталога
dotnet ef migrations add AddSomething -p Shop.Catalog.Api -s Shop.Catalog.Api -o Data/Migrations
# миграция заказов (модель в Shop.Orders.Data, запуск из Orders.Api)
dotnet ef migrations add AddSomething -p Shop.Orders.Data -s Shop.Orders.Api -o Migrations
```

## Упражнения
1. Остановите `order-processor` (`docker compose stop order-processor`), создайте 2 заказа — они остаются `Pending`.
   Запустите обработчик снова — заказы обработаются. Так очередь развязывает сервисы.
2. Найдите в коде, где настраивается poison-очередь (`StorageQueueOrderListener`). Что будет с «плохим» сообщением?
3. Посмотрите очередь в Azurite через **Azure Storage Explorer** (бесплатная программа): Local & Attached → Emulator.

## Очистка
`docker compose down` — облако не использовалось.

## Вопросы для самопроверки
1. Почему Orders API запрашивает цену у Catalog API, а не принимает её от браузера?
2. Почему обработка заказа должна быть идемпотентной?
3. Как приложение выбирает между строкой подключения и Managed Identity?
