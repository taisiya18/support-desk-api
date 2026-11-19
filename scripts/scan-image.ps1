# Поиск известных уязвимостей в собранном образе.
# Запуск: .\scripts\scan-image.ps1 -Label before
# Проверяет системные пакеты и среду выполнения внутри образа, а не исходный код.
param(
    [ValidateSet('before', 'after')][string]$Label = 'before',
    [string]$Image = 'support-desk-api:local',
    # База уязвимостей не обновляется, чтобы запуски до и после сравнивались
    # на одних и тех же данных. Снять флаг, когда нужна свежая база.
    [bool]$SkipDbUpdate = $true
)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$scanner = 'aquasec/trivy@sha256:8f6bb947dbc528902e11f2f5be52f7b519f6a39d5d148f41166f2040499768fb'
$outDir = Join-Path $root 'reports/local'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null
$name = "trivy-$Label.json"
$report = Join-Path $outDir $name
if (Test-Path $report) { Remove-Item -LiteralPath $report }

$dbArgs = if ($SkipDbUpdate) { @('--skip-db-update') } else { @() }

& docker run --rm `
    -v /var/run/docker.sock:/var/run/docker.sock `
    -v trivy-cache:/root/.cache `
    --mount "type=bind,source=$root,target=/repo" `
    $scanner image $Image --scanners vuln --severity HIGH,CRITICAL @dbArgs `
    --format json --output "/repo/reports/local/$name"
$code = $LASTEXITCODE
if (-not (Test-Path $report)) { throw "Trivy не создал отчет, код выхода $code. Прочитай его вывод." }
$data = Get-Content -Raw -LiteralPath $report | ConvertFrom-Json
$total = 0
foreach ($result in @($data.Results)) {
    $n = @($result.Vulnerabilities).Count
    if ($n -gt 0) { Write-Host "$($result.Target): $n" }
    $total += $n
}
Write-Host "Trivy отработал. Уязвимостей уровня HIGH и CRITICAL: $total. Отчет: $report"
Write-Host 'Область: системные пакеты и среда выполнения внутри образа. Исходный код не анализируется.'
