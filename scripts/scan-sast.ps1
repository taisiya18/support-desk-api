# Статический анализ кода. Запуск: .\scripts\scan-sast.ps1 -Label before
# Label попадает в имя отчета и различает запуски до и после исправления.
param(
    [ValidateSet('before', 'after')][string]$Label = 'before',
    [ValidateSet('project', 'community')][string]$Rules = 'project'
)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$image = 'semgrep/semgrep:1.175.0@sha256:b94b53d02fd4a022f9eac4e2af1380f5c3c4c21400e79d3336bdff1d1db5e796'
$outDir = Join-Path $root 'reports/local'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null
$name = "semgrep-$Rules-$Label.json"
$report = Join-Path $outDir $name
if (Test-Path $report) { Remove-Item -LiteralPath $report }

# Локальные правила не требуют сети, наборы из реестра требуют.
if ($Rules -eq 'project') {
    $configArgs = @('--config', 'semgrep.yml')
    $netArgs = @('--network', 'none')
} else {
    $configArgs = @('--config', 'p/csharp', '--config', 'p/dockerfile',
                    '--config', 'p/github-actions', '--config', 'p/secrets',
                    '--config', 'p/security-audit', '--config', 'p/owasp-top-ten')
    $netArgs = @()
}

& docker run --rm @netArgs --mount "type=bind,source=$root,target=/src" --workdir /src `
    --env SEMGREP_SEND_METRICS=off $image semgrep scan @configArgs `
    --metrics=off --disable-version-check --json --output "reports/local/$name" .
$code = $LASTEXITCODE
if (-not (Test-Path $report)) { throw "Semgrep не создал отчет, код выхода $code. Прочитай его вывод." }
$data = Get-Content -Raw -LiteralPath $report | ConvertFrom-Json
if (@($data.errors).Count -gt 0) { throw "Semgrep сообщил об ошибках разбора. Смотри $report" }
Write-Host "Semgrep отработал. Сигналов: $(@($data.results).Count). Отчет: $report"
Write-Host 'Код выхода 0 означает, что сканирование завершилось, а не что в коде нет дефектов.'
$data.results | ForEach-Object { Write-Host "$($_.check_id)  $($_.path):$($_.start.line)" }
