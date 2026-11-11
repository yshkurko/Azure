# Урок 00. Защита от расходов

## Цель

До создания первого ресурса:
- узнать тип подписки и есть ли у неё лимит расходов;
- настроить бюджет с оповещениями;
- выучить правила, которые не дают «забытым» ресурсам списывать деньги.

## 1. Какой у меня тип подписки

### Portal
1. Откройте https://portal.azure.com → в поиске введите **Subscriptions** → выберите подписку (например, *Azure subscription 1*).
2. В блоке **Essentials** посмотрите поле **Offer** / **Plan**. В современных аккаунтах там часто написано просто
   **Azure Plan** — это ещё не говорит, бесплатная подписка или платная. Точный тип покажет `quotaId` (ниже).

### Azure Cloud Shell (ничего не нужно устанавливать)
1. Нажмите иконку терминала **`>_`** в верхней панели портала → **Bash** → **No storage account required**
   (эфемерная сессия, бесплатно).
2. Если появилось предупреждение *«not registered to Microsoft.CloudShell Namespace»* — выполните один раз
   `az provider register --namespace Microsoft.CloudShell` (бесплатно).
3. Выполните команду из следующего раздела.

> `az account show` **не** показывает тип предложения (`offerId`/`quotaId`) — для этого нужен запрос `az rest` ниже.

### CLI (Cloud Shell или локально после урока 01)

```powershell
az login
$SUB_ID = az account show --query id -o tsv
az rest --method get --url "https://management.azure.com/subscriptions/$($SUB_ID)?api-version=2022-12-01" `
  --query "{name:displayName, state:state, quotaId:subscriptionPolicies.quotaId, spendingLimit:subscriptionPolicies.spendingLimit}"
```

### Как понять результат

| quotaId / Offer | Что это | Лимит расходов (Spending limit) |
|---|---|---|
| `FreeTrial_*` (MS-AZR-0044P) | Бесплатная пробная: кредит (обычно 200 USD) на 30 дней | **Есть.** Когда кредит кончается, ресурсы останавливаются, денег с карты не снимают |
| `MSDN_*`, `VisualStudio*` (MS-AZR-0063P и др.) | Подписка Visual Studio: ежемесячный кредит | **Есть** по умолчанию. Не снимайте его |
| `AzurePass_*`, `Sponsored_*` | Учебный/спонсорский кредит | Обычно есть |
| `PayAsYouGo_*` (MS-AZR-0003P) | Оплата по факту | **Нет!** Все расходы списываются с карты |
| Offer показывает **Azure Plan** | Подписка по Microsoft Customer Agreement — может быть и пробной, и платной | Смотрите `quotaId` и `spendingLimit` |
| `CSP_*`, `EnterpriseAgreement_*` | Через партнёра / корпоративная | Зависит от договора |

> **Если у вас Pay-As-You-Go — будьте особенно внимательны:** бюджеты в Azure только **оповещают**,
> но **не останавливают** ресурсы. Единственная реальная защита — удалять ресурсы после занятия.

Проверить, включён ли лимит: Portal → **Cost Management + Billing** → ваша подписка →
**Overview** → блок **Spending limit**.

### Ваша подписка: Free Trial (`FreeTrial_2014-09-01`, `spendingLimit: On`)

Проверено 11.11.2026 через Cloud Shell. Что это значит для курса:

- **Деньги с карты не снимаются**, пока вы сами не нажмёте **Upgrade**. Все расходы курса идут из кредита.
- Кредит (обычно 200 USD) действует **30 дней** с момента активации. Когда кредит кончится **или** пройдут 30 дней,
  ресурсы будут отключены, а подписка — заблокирована до апгрейда.
- **После Upgrade до Pay-As-You-Go лимит расходов снимается** — тогда правила этого урока становятся критичными.
  Не делайте апгрейд «на всякий случай».
- Квоты Free Trial небольшие: обычно **4 vCPU на регион**. Нашему AKS (1 узел B2s = 2 vCPU) хватает,
  но второй пул узлов или более крупная ВМ упрутся в квоту. Проверка:
  `az vm list-usage -l swedencentral --query "[?contains(name.value,'cores')]" -o table`.
- Остаток кредита и дату окончания смотрите в Portal → **Cost Management + Billing** → **Azure credits**
  (или плашка на странице подписки).

**План прохождения под Free Trial:**

| Когда | Уроки | Комментарий |
|---|---|---|
| Дни 1–2 | 00–03 | Без облачных расходов |
| Дни 3–14 | 04–10 | Ручная часть, кредит расходуется копейками |
| Дни 15–25 | 11–15 | Terraform, AKS, CI/CD — самые «дорогие» уроки, но кредита хватает с большим запасом |
| До дня 30 | — | Удалить всё (`Study/scripts/cleanup.ps1`) и решить, нужен ли апгрейд |

Если не успеете за 30 дней — после апгрейда до Pay-As-You-Go появятся бесплатные на 12 месяцев сервисы
и бесплатные навсегда (SQL free offer, Static Web Apps Free и т.д.), но всё остальное будет списываться с карты.

## 2. Бюджет с оповещениями

### Portal
1. Поиск → **Cost Management** → слева **Budgets** → **+ Add**.
2. Scope: ваша подписка.
3. Name: `budget-learning`, Reset period: **Monthly**, Amount: **10** (валюта — валюта подписки).
4. **Alert conditions**:
   - Actual, 50 %
   - Actual, 80 %
   - Actual, 100 %
   - Forecasted, 100 %
5. Alert recipients: ваш email. **Create**.

> Данные о расходах обновляются с задержкой до 8–24 часов. Бюджет — это «сигнализация», а не «предохранитель».

### Дополнительно: оповещение об аномалиях
Cost Management → **Cost alerts** → **Anomaly alerts** → **+ Add** → ежедневное письмо, если
расходы резко выросли. Бесплатно.

## 3. Где смотреть расходы

- **Cost Management → Cost analysis**: вид **Daily costs**, группировка по **Resource group** или **Resource**.
- **Cost analysis → Group by: Tag `course`** — увидите всё, что создано в рамках курса.
- Привычка: открывать Cost analysis в начале каждого занятия.

## 4. Правила курса

1. **Каждый ресурс получает тег `course=azure-learning`.** Terraform и примеры CLI делают это за вас.
2. **Ресурсы живут в отдельных ресурсных группах** (`rg-shop-manual`, `rg-shop-paas`, `rg-shop-aks`).
   Удаление группы удаляет всё внутри — это самый надёжный способ «прибраться».
3. **В конце занятия — раздел «Очистка».** Если перерыв больше 1–2 дней, удаляйте всё платное.
4. **Только самые дешёвые SKU.** Никогда в этом курсе не создавайте (только читаем теорию):
   Application Gateway, Azure Firewall, VPN Gateway, Azure Bastion (Standard), Front Door Premium,
   Service Bus Premium, SQL Business Critical/Hyperscale, AKS Standard/Premium tier, ВМ серий D/E/F.
5. **Перед `apply`/`create` смотрим цену** в калькуляторе: https://azure.microsoft.com/pricing/calculator/
6. **Финальная проверка** после занятия:
   ```powershell
   az group list -o table
   az resource list --tag course=azure-learning -o table
   ```

## 5. Что продолжает списывать деньги, даже если «ничего не делаешь»

| Ресурс | Почему платим | Как остановить |
|---|---|---|
| App Service Plan (B1 и выше) | Платим за план, **даже если приложения остановлены** | Удалить план |
| Виртуальные машины / узлы AKS | «Stop» в ОС не помогает — нужна **Deallocate** | Удалить или `az vm deallocate` |
| Managed Disks | Диски платные, даже если ВМ удалена | Удалить диски |
| Public IP (Standard) | Почасово | Удалить |
| Load Balancer (Standard) | Почасово за правила | Удалить (в AKS — удалить кластер) |
| Private Endpoint | Почасово + трафик | Удалить |
| Azure SQL (не free offer) | Почасово за DTU/vCore | Удалить базу |
| Container Registry | Ежедневно | Удалить |
| Log Analytics | За объём данных | Дневной лимит (урок 10) |
| Key Vault | После удаления — soft-delete (не платный, но имя занято) | `az keyvault purge` |

## 6. Ориентировочные цены ресурсов курса

Цены примерные (регион Sweden Central, USD). Проверяйте в калькуляторе — они меняются.

| Ресурс | SKU | ~ в час | ~ в месяц, если забыть |
|---|---|---|---|
| VNet, подсети, NSG, Private DNS zone | — | 0 | ~0.5 (DNS zone) |
| Azure SQL Database | Free offer | 0 | 0 (в пределах лимита) |
| Azure SQL Database | Basic (Terraform) | ~0.007 | ~5 |
| Private Endpoint | — | ~0.01 | ~7.5 |
| App Service Plan | B1 Linux | ~0.018 | ~13 |
| Static Web Apps | Free | 0 | 0 |
| Storage Account + очереди | Standard LRS | ≈0 | < 0.5 |
| Service Bus | Basic | ≈0 | < 0.1 |
| Log Analytics + App Insights | с лимитом 0.1 GB/день | ≈0 | < 1 (первые 5 GB/мес бесплатны) |
| Container Registry | Basic | ~0.007 | ~5 |
| AKS control plane | Free tier | 0 | 0 |
| AKS узел | Standard_B2s ×1 | ~0.05 | ~35 |
| Standard Load Balancer + Public IP | — | ~0.03 | ~22 |

**Вывод:** уроки 04–10 при аккуратности укладываются в 1–3 USD. Урок 14 (AKS) — около
0.1 USD/час за весь стенд, поэтому кластер создаём только на время занятия.

## Очистка

В этом уроке ничего не создано, кроме бюджета (бесплатно).

## Вопросы для самопроверки

1. Какой у вашей подписки тип и есть ли лимит расходов?
2. Остановит ли бюджет ресурсы, если расходы превысят 10 USD?
3. Почему остановленное приложение в App Service всё равно стоит денег?
4. Какая команда покажет все ресурсы курса?
