<#
.SYNOPSIS
    Сборка и упаковка BackupDotNet в готовый к установке архив.

.DESCRIPTION
    Собирает решение в Release, готовит каталог артефактов с шаблонами
    конфигурации и упаковывает всё в ZIP.

    Результат:
        artifacts/BackupDotNet-<версия>/          — распакованный комплект
        artifacts/BackupDotNet-<версия>.zip       — архив для переноса

.PARAMETER Version
    Версия продукта. По умолчанию берётся из Directory.Build.props.

.PARAMETER NoZip
    Не создавать ZIP (только каталог).

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File tools\package.ps1
#>

[CmdletBinding()]
param(
    [string]$Version = "",
    [switch]$NoZip
)

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
if (-not $Version) {
    $props = Get-Content (Join-Path $root 'Directory.Build.props') -Raw
    if ($props -match '<VersionPrefix>([^<]+)</VersionPrefix>') {
        $Version = $Matches[1].Trim()
    } else {
        $Version = '0.0.0'
    }
}

$artifacts = Join-Path $root 'artifacts'
$stage = Join-Path $artifacts "BackupDotNet-$Version"
$buildOut = Join-Path $root 'src\BackupDotNet.App\bin\Release'

Write-Host "Версия: $Version"
Write-Host "Сборка..."

& dotnet build (Join-Path $root 'BackupDotNet.sln') -c Release -v m
if ($LASTEXITCODE -ne 0) {
    throw "Сборка завершилась с ошибкой (код $LASTEXITCODE)"
}

if (Test-Path $stage) {
    Remove-Item $stage -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $stage | Out-Null

# --- бинарники и веб-интерфейс -------------------------------------------
Get-ChildItem $buildOut -Exclude '*.pdb', '*.xml' | ForEach-Object {
    Copy-Item $_.FullName -Destination $stage -Recurse -Force
}

# --- конфигурация: только шаблоны ----------------------------------------
$cfg = Join-Path $stage 'config'
New-Item -ItemType Directory -Force -Path $cfg, (Join-Path $cfg 'companies') | Out-Null
Copy-Item (Join-Path $root 'config\*.example.json') $cfg -Force
Copy-Item (Join-Path $root 'config\companies\*.example.json') (Join-Path $cfg 'companies') -Force

# --- документация --------------------------------------------------------
Copy-Item (Join-Path $root 'README.md') $stage -Force
Copy-Item (Join-Path $root 'ROADMAP.md') $stage -Force
Copy-Item (Join-Path $root 'CHANGELOG.md') $stage -Force
Copy-Item (Join-Path $root 'LICENSE') $stage -Force
Copy-Item (Join-Path $root 'docs') $stage -Recurse -Force

# --- скрипт установки ----------------------------------------------------
$install = @'
# Установка BackupDotNet. Запускать от имени администратора.
$ErrorActionPreference = 'Stop'
$src = Split-Path -Parent $MyInvocation.MyCommand.Path
$dir = 'C:\BackupDotNet'
$data = 'C:\ProgramData\BackupDotNet'

Write-Host "Установка в $dir ..."
New-Item -ItemType Directory -Force -Path $dir, $data | Out-Null
Copy-Item "$src\*" $dir -Recurse -Force -Exclude 'install.ps1'

# шаблоны -> рабочие файлы (существующие не перетираем)
Push-Location "$dir\config"
foreach ($t in Get-ChildItem *.example.json) {
    $target = $t.Name -replace '\.example\.json$', '.json'
    if (-not (Test-Path $target)) { Copy-Item $t.Name $target }
}
Pop-Location

# абсолютные пути для службы
@"
{
  "installDir": "C:\\BackupDotNet\\",
  "dataDir": "C:\\ProgramData\\BackupDotNet",
  "serviceName": "BackupDotNetService",
  "serviceNameRemote": "BackupDotNetRemote"
}
"@ | Set-Content "$dir\config\env.json" -Encoding UTF8

Write-Host 'Регистрация службы ...'
sc.exe stop BackupDotNetService | Out-Null
sc.exe delete BackupDotNetService | Out-Null
sc.exe create BackupDotNetService binPath= "`"$dir\BackupDotNet.exe`" --service" start= auto | Out-Null
sc.exe description BackupDotNetService "Мониторинг бэкапов, доступности и железа" | Out-Null
sc.exe failure BackupDotNetService reset= 86400 actions= restart/60000/restart/60000/restart/60000 | Out-Null

netsh http add urlacl url=http://+:8080/ user="NT AUTHORITY\SYSTEM" | Out-Null
New-NetFirewallRule -DisplayName 'BackupDotNet' -Direction Inbound -Protocol TCP -LocalPort 8080 -Action Allow -ErrorAction SilentlyContinue | Out-Null

sc.exe start BackupDotNetService | Out-Null
Start-Sleep -Seconds 3

Write-Host ''
Write-Host 'Готово. Веб-интерфейс: http://localhost:8080/'
if (Test-Path "$dir\auth\AGENT.md") {
    Write-Host 'Пароль администратора:'
    Get-Content "$dir\auth\AGENT.md" | Select-String 'admin'
} else {
    Write-Host "Пароль появится в $dir\auth\AGENT.md после первого запуска."
}
'@
Set-Content (Join-Path $stage 'install.ps1') $install -Encoding UTF8

# --- архив ---------------------------------------------------------------
if (-not $NoZip) {
    $zip = Join-Path $artifacts "BackupDotNet-$Version.zip"
    if (Test-Path $zip) { Remove-Item $zip -Force }
    Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $zip -CompressionLevel Optimal
    Write-Host "Архив: $zip"
}

$size = (Get-ChildItem $stage -Recurse -File | Measure-Object -Property Length -Sum).Sum
Write-Host ("Комплект: {0} ({1:N1} МБ)" -f $stage, ($size / 1MB))
Write-Host 'Установка: распаковать и запустить install.ps1 от администратора.'
