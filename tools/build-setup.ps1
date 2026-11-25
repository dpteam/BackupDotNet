# Сборка пакета установщика.
#
# Раскладывает рядом с BackupDotNet.Setup.exe каталог payload с файлами агента
# и упаковывает всё в один zip. Так установщик можно перенести на другую
# машину одним файлом.
#
# Использование:
#   powershell -ExecutionPolicy Bypass -File tools\build-setup.ps1
#   powershell -ExecutionPolicy Bypass -File tools\build-setup.ps1 -Zip

param(
    [switch]$Zip,
    [string]$OutDir = "artifacts\setup"
)

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$setupProj = Join-Path $root 'src\BackupDotNet.Setup\BackupDotNet.Setup.csproj'
$setupBin = Join-Path $root 'src\BackupDotNet.Setup\bin\Release'
$agentBin = Join-Path $root 'bin'
$out = Join-Path $root $OutDir

Write-Host 'Сборка установщика...'
& dotnet build $setupProj -c Release -v q --nologo
if ($LASTEXITCODE -ne 0) {
    throw 'сборка установщика не удалась'
}

# Агент должен быть уже собран в bin (build.cmd).
$agentExe = Join-Path $agentBin 'BackupDotNet.exe'
if (-not (Test-Path $agentExe)) {
    throw "нет собранного агента: $agentExe (сначала build.cmd)"
}

Write-Host "Собираю пакет в $out ..."
if (Test-Path $out) {
    Remove-Item $out -Recurse -Force
}
New-Item -ItemType Directory -Path $out -Force | Out-Null

# 1. Сам установщик и его зависимости.
Copy-Item (Join-Path $setupBin 'BackupDotNet.Setup.exe') $out
Copy-Item (Join-Path $setupBin 'BackupDotNet.Setup.exe.config') $out -ErrorAction SilentlyContinue
Copy-Item (Join-Path $setupBin 'setup-ui') $out -Recurse

# Зависимости установщика (Core — ради Log и Json). Без них exe не запустится:
# проверено на практике, падает с FileNotFoundException ещё до окна.
Get-ChildItem $setupBin -Filter '*.dll' | ForEach-Object {
    Copy-Item $_.FullName $out
}

# 2. payload: файлы агента.
#
# Берём только то, что нужно для работы: exe, dll, public, config-шаблоны.
# Каталог data не копируем — это данные конкретной машины, их не должно
# быть в дистрибутиве.
$payload = Join-Path $out 'payload'
New-Item -ItemType Directory -Path $payload -Force | Out-Null

$copyFiles = @('BackupDotNet.exe', 'BackupDotNet.exe.config',
    'BackupDotNet.Core.dll', 'BackupDotNet.Web.dll',
    'BackupDotNet.Deploy.dll', 'BackupDotNet.Manage.dll',
    'install.ps1', 'LICENSE', 'README.md')

foreach ($name in $copyFiles) {
    $src = Join-Path $agentBin $name
    if (Test-Path $src) {
        Copy-Item $src $payload
    }
}

# public и config — целиком, но без *.json из config (это рабочие настройки).
foreach ($dir in @('public')) {
    $src = Join-Path $agentBin $dir
    if (Test-Path $src) {
        Copy-Item $src $payload -Recurse
    }
}

$cfgSrc = Join-Path $agentBin 'config'
if (Test-Path $cfgSrc) {
    $cfgDst = Join-Path $payload 'config'
    New-Item -ItemType Directory -Path $cfgDst -Force | Out-Null
    # Только шаблоны: *.example.json и branding.json (он не секрет).
    Get-ChildItem $cfgSrc -File | Where-Object {
        $_.Name -like '*.example.json' -or $_.Name -eq 'branding.json'
    } | ForEach-Object { Copy-Item $_.FullName $cfgDst }
}

$payloadFiles = (Get-ChildItem $payload -Recurse -File | Measure-Object).Count
Write-Host ("  payload: {0} файлов" -f $payloadFiles)

# 3. Проверка, что главное на месте.
$need = Join-Path $payload 'BackupDotNet.exe'
if (-not (Test-Path $need)) {
    throw 'в payload нет BackupDotNet.exe'
}

if ($Zip) {
    $zipPath = Join-Path $root "$OutDir.zip"
    if (Test-Path $zipPath) {
        Remove-Item $zipPath -Force
    }
    Write-Host "Упаковываю в $zipPath ..."
    Compress-Archive -Path (Join-Path $out '*') -DestinationPath $zipPath
    $mb = [math]::Round((Get-Item $zipPath).Length / 1MB, 1)
    Write-Host ("  архив: {0} МБ" -f $mb)
}

Write-Host ''
Write-Host 'Готово:'
Write-Host ("  установщик: " + (Join-Path $out 'BackupDotNet.Setup.exe'))
Write-Host ("  payload:    " + $payload)
if ($Zip) {
    Write-Host ("  архив:      " + (Join-Path $root "$OutDir.zip"))
}
