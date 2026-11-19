# Поиск секретов. Запуск: .\scripts\scan-secrets.ps1 -Mode tree
# tree смотрит текущие файлы, history смотрит все коммиты всех веток.
param(
    [ValidateSet('tree', 'history')][string]$Mode = 'tree',
    [ValidateSet('before', 'after')][string]$Label = 'before'
)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$image = 'ghcr.io/gitleaks/gitleaks:v8.30.1@sha256:c00b6bd0aeb3071cbcb79009cb16a60dd9e0a7c60e2be9ab65d25e6bc8abbb7f'
$outDir = Join-Path $root 'reports/local'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null
$name = "gitleaks-$Mode-$Label.json"
$report = Join-Path $outDir $name
if (Test-Path $report) { Remove-Item -LiteralPath $report }

$scanArgs = if ($Mode -eq 'history') { @('git', '/repo', '--log-opts=--all') } else { @('dir', '/repo') }

& docker run --rm --network none --mount "type=bind,source=$root,target=/repo" $image @scanArgs `
    --redact --no-banner --report-format json --report-path "/repo/reports/local/$name"
$code = $LASTEXITCODE
if (-not (Test-Path $report)) { throw "Gitleaks не создал отчет, код выхода $code. Прочитай его вывод." }
# Windows PowerShell 5.1 разбирает пустой массив и одиночный объект по-разному.
$data = @(Get-Content -Raw -LiteralPath $report | ConvertFrom-Json | Where-Object { $null -ne $_ })
$count = $data.Count
if (($code -eq 1 -and $count -gt 0) -or ($code -eq 0 -and $count -eq 0)) {
    Write-Host "Gitleaks отработал. Находок: $count, код выхода: $code. Отчет: $report"
    foreach ($f in $data) { Write-Host "$($f.RuleID)  $($f.File):$($f.StartLine)  commit $($f.Commit)" }
} else {
    throw "Неожиданный результат Gitleaks: код $code, находок $count. Прочитай его вывод."
}
