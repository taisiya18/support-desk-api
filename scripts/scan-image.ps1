# Поиск известных уязвимостей в собранном образе.
# Запуск: .\scripts\scan-image.ps1 -Label before
# Проверяет системные пакеты и среду выполнения внутри образа, а не исходный код.
param(
    [ValidateSet('before', 'after')][string]$Label = 'before',
    [string]$Image = 'support-desk-api:local',
    # База уязвимостей по умолчанию не обновляется, чтобы запуски до и после
    # сравнивались на одних данных. Для свежей базы вызвать с -SkipDbUpdate:$false
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

# Все аргументы собираются в один список. Строка 'HIGH,CRITICAL' взята в кавычки,
# иначе PowerShell может разобрать запятую как разделитель элементов массива.
$dockerArgs = @(
    'run', '--rm',
    '-v', '/var/run/docker.sock:/var/run/docker.sock',
    '-v', 'trivy-cache:/root/.cache',
    '--mount', "type=bind,source=$root,target=/repo",
    $scanner,
    'image', $Image,
    '--scanners', 'vuln',
    '--severity', 'HIGH,CRITICAL',
    '--format', 'json',
    '--output', "/repo/reports/local/$name"
)
if ($SkipDbUpdate) { $dockerArgs += '--skip-db-update' }

Write-Host "docker $($dockerArgs -join ' ')"
& docker @dockerArgs
$code = $LASTEXITCODE
if (-not (Test-Path $report)) { throw "Trivy не создал отчет, код выхода $code. Прочитай его вывод." }

$data = Get-Content -Raw -LiteralPath $report | ConvertFrom-Json
$total = 0
foreach ($result in @($data.Results)) {
    # У цели без уязвимостей ключ Vulnerabilities отсутствует,
    # а @($null).Count в PowerShell дает 1, поэтому проверяется явно.
    $vulns = $result.Vulnerabilities
    $n = if ($null -eq $vulns) { 0 } else { @($vulns).Count }
    Write-Host "$($result.Target): $n"
    $total += $n
}
Write-Host "Trivy отработал. Уязвимостей уровня HIGH и CRITICAL: $total. Отчет: $report"
Write-Host 'Область: системные пакеты и среда выполнения внутри образа. Исходный код не анализируется.'
