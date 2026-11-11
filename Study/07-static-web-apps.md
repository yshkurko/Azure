# Урок 07. Фронтенд в Azure Static Web Apps

## Цель
Разместить Angular-приложение в Static Web Apps (бесплатный план) и настроить CORS в API.

## Теория
- **Static Web Apps (SWA)** — хостинг статических файлов + глобальный CDN + бесплатный HTTPS.
- **Free plan**: 100 GB трафика/мес, 2 кастомных домена, без SLA. **Standard** (~9 USD/мес) добавляет
  «linked backend» (проксирование `/api` на App Service) и приватные эндпоинты — нам не нужен.
- Так как фронт и API на разных доменах, браузер применяет **CORS**: API должен явно разрешить
  домен фронтенда (`Cors:AllowedOrigins` в наших API).
- SWA доступен не во всех регионах — используем `westeurope` (это регион только для метаданных; контент раздаётся глобально).

## 1. Создание Static Web App

```powershell
. .\Study\scripts\env.ps1
az staticwebapp create -g $RG -n $SWA -l westeurope --sku Free --tags course=azure-learning
$SWA_HOST = az staticwebapp show -g $RG -n $SWA --query defaultHostname -o tsv
$SWA_TOKEN = az staticwebapp secrets list -g $RG -n $SWA --query properties.apiKey -o tsv
"https://$SWA_HOST"
```

## 2. Сборка с адресами API

Angular читает `config.json` при старте. Укажем адреса API в собранной версии:

```powershell
cd src/shop-web
npm install
npx ng build --configuration production

@{
  catalogApiUrl = "https://$APP_CATALOG.azurewebsites.net"
  ordersApiUrl  = "https://$APP_ORDERS.azurewebsites.net"
} | ConvertTo-Json | Set-Content dist/shop-web/browser/config.json
```

Файл `public/staticwebapp.config.json` уже лежит в сборке — он включает SPA-fallback на `index.html`.

## 3. Публикация

```powershell
npx @azure/static-web-apps-cli deploy ./dist/shop-web/browser --deployment-token $SWA_TOKEN --env production
cd ../..
```

## 4. CORS в API

```powershell
foreach ($app in $APP_CATALOG, $APP_ORDERS) {
  az webapp config appsettings set -g $RG -n $app --settings "Cors__AllowedOrigins__0=https://$SWA_HOST"
}
```

> Не используйте `az webapp cors add` вместе с CORS в коде — будет два источника правды.
> У нас CORS настраивается в приложении (`app.UseCors()`).

## Проверка
Откройте `https://<SWA_HOST>` → каталог должен загрузиться. В DevTools (F12 → Network) видно запросы на
`app-shop-catalog-*.azurewebsites.net`. Если ошибка CORS — проверьте `Cors__AllowedOrigins__0` и перезапустите приложение
(`az webapp restart`).

Оформление заказа заработает после урока 08.

## Очистка
SWA Free бесплатен — оставьте до урока 10.

## Вопросы для самопроверки
1. Зачем фронтенду `config.json`, если можно «вшить» URL при сборке?
2. Что такое CORS и почему без него браузер блокирует запросы?
3. Что даёт план Standard по сравнению с Free?
