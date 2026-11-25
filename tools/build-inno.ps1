# Сборка двух установщиков Inno Setup: сервер и агент.
#
# Зачем отдельно от build-setup.ps1: тот собирает мастер на WebView2 для
# сетевой установки (SMB/WinRM/AD). Inno даёт то, чего у мастера нет:
# нормальное удаление, запись в «Установка и удаление программ», ярлыки
# и тихую установку /SILENT из групповой политики.
#
# Использование:
#   powershell -ExecutionPolicy Bypass -File tools\build-inno.ps1
#   powershell -ExecutionPolicy Bypass -File tools\build-inno.ps1 -Branding config\branding.dpt.json

param(
    # Брендинг. По умолчанию дебрендированный: в релиз не должно уезжать
    # имя чьей-то инсталляции.
    [string]$Branding = "config\branding.example.json",
    [string]$OutDir = "artifacts\inno",
    # Пропустить пересборку payload, если он уже есть.
    [switch]$ReusePayload
)

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$payload = Join-Path $root 'artifacts\setup\payload'
$out = Join-Path $root $OutDir

# --- компилятор -------------------------------------------------------------
$iscc = $null
$candidates = @(
    'C:\Program Files (x86)\Inno Setup 6\ISCC.exe',
    'C:\Program Files\Inno Setup 6\ISCC.exe'
)
foreach ($c in $candidates) {
    if (Test-Path $c) { $iscc = $c; break }
}
if (-not $iscc) {
    $cmd = Get-Command ISCC.exe -ErrorAction SilentlyContinue
    if ($cmd) { $iscc = $cmd.Source }
}
if (-not $iscc) {
    throw 'не найден ISCC.exe (Inno Setup 6). Установите Inno Setup или добавьте его в PATH.'
}
Write-Host "Компилятор: $iscc"

# --- брендинг ---------------------------------------------------------------
# Путь может быть относительным (от корня репозитория) или абсолютным.
$brandSrc = if ([System.IO.Path]::IsPathRooted($Branding)) { $Branding } else { Join-Path $root $Branding }
if (-not (Test-Path $brandSrc)) {
    throw "нет файла брендинга: $brandSrc"
}
$brand = (Get-Content $brandSrc -Raw -Encoding UTF8 | ConvertFrom-Json).brand
$productName = if ($brand.productName) { $brand.productName } else { 'BackupDotNet' }
$installDir = if ($brand.installDir) { $brand.installDir.TrimEnd('\') } else { 'C:\BackupDotNet' }
$dataDir = if ($brand.dataDir) { $brand.dataDir.TrimEnd('\') } else { 'C:\ProgramData\BackupDotNet' }
$serviceName = if ($brand.serviceName) { $brand.serviceName } else { 'BackupDotNetService' }
$serviceRemote = if ($brand.serviceNameRemote) { $brand.serviceNameRemote } else { 'BackupDotNetRemote' }
$description = if ($brand.description) { $brand.description } else { 'Мониторинг бэкапов, доступности и железа' }

$version = ([xml](Get-Content (Join-Path $root 'Directory.Build.props') -Raw)).Project.PropertyGroup.VersionPrefix |
    Where-Object { $_ } | Select-Object -First 1
if (-not $version) { $version = '1.0.0' }

Write-Host "  продукт:    $productName $version"
Write-Host "  каталог:    $installDir"
Write-Host "  службы:     $serviceName / $serviceRemote"

# --- payload ----------------------------------------------------------------
# Переиспользуем payload из build-setup.ps1: один список файлов на все
# установщики. Дублировать его здесь значит рано или поздно разойтись.
if (-not $ReusePayload -or -not (Test-Path (Join-Path $payload 'BackupDotNet.exe'))) {
    Write-Host 'Сборка payload...'
    & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'build-setup.ps1') -Branding $Branding | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'не удалось собрать payload' }
}
if (-not (Test-Path (Join-Path $payload 'BackupDotNet.exe'))) {
    throw "в payload нет BackupDotNet.exe: $payload"
}
$agentVer = (Get-Item (Join-Path $payload 'BackupDotNet.exe')).VersionInfo.FileVersion
Write-Host "  агент:      версия $agentVer"

# --- компиляция -------------------------------------------------------------
if (Test-Path $out) { Remove-Item $out -Recurse -Force }
New-Item -ItemType Directory -Path $out -Force | Out-Null

function Compile($issName, $extra) {
    $iss = Join-Path $root "installer\$issName"
    if (-not (Test-Path $iss)) { throw "нет файла: $iss" }

    # Кавычки вокруг значений с пробелами: Inno разбирает аргументы сам.
    $args = @(
        "/DMyProductName=$productName",
        "/DMyVersion=$version",
        "/DMyInstallDir=$installDir",
        "/DMyDataDir=$dataDir",
        "/DMyServiceName=$serviceName",
        "/DMyServiceNameRemote=$serviceRemote",
        "/DMyDescription=$description",
        "/DMyPayload=$payload",
        "/DMyOutputDir=$out"
    )
    $args += $extra
    $args += $iss

    Write-Host "Сборка $issName ..."
    & $iscc @args | ForEach-Object { if ($_ -match 'error|Error|ошибка') { Write-Host "  $_" } }
    if ($LASTEXITCODE -ne 0) {
        throw "компиляция $issName не удалась (код $LASTEXITCODE)"
    }
}

Compile 'server.iss' @()
Compile 'agent.iss' @()

# --- проверка ---------------------------------------------------------------
$made = Get-ChildItem $out -Filter '*.exe'
if ($made.Count -lt 2) {
    throw "ожидались два установщика, получилось $($made.Count)"
}

Write-Host ''
Write-Host 'Готово:'
foreach ($f in $made) {
    Write-Host ("  {0,-40} {1,6} МБ" -f $f.Name, [math]::Round($f.Length / 1MB, 2))
}
