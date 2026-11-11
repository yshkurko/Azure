# Показывает все учебные ресурсные группы и (после подтверждения) удаляет их.
# Использование:  .\Study\scripts\cleanup.ps1
# Группу rg-shop-tfstate (Terraform state, копейки в месяц) по умолчанию не трогаем.

param(
    [switch]$IncludeTfState
)

$groups = az group list --tag course=azure-learning --query "[].name" -o tsv |
    Where-Object { $IncludeTfState -or $_ -ne "rg-shop-tfstate" }

if (-not $groups) {
    Write-Host "Учебных ресурсных групп нет. Ничего не списывается (кроме того, что создано без тега)." -ForegroundColor Green
    Write-Host "Проверьте все группы: az group list -o table"
    return
}

Write-Host "Будут удалены группы:" -ForegroundColor Yellow
$groups | ForEach-Object { Write-Host "  $_" }

$answer = Read-Host "Удалить? Введите 'yes'"
if ($answer -ne "yes") {
    Write-Host "Отменено."
    return
}

foreach ($g in $groups) {
    az group delete --name $g --yes --no-wait
    Write-Host "Запущено удаление $g"
}

Write-Host "Удаление идёт в фоне. Проверка: az group list -o table" -ForegroundColor Green
Write-Host "Удалённые Key Vault остаются в soft-delete: az keyvault list-deleted -o table"
