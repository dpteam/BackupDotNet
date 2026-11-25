<#
.SYNOPSIS
    Разрешает доступ к веб-интерфейсу BackupDotNet по сети.

.DESCRIPTION
    Зачем это нужно. HTTP.sys отдаёт порт процессу только при одном из двух
    условий: процесс запущен от администратора или на порт выдан urlacl.
    Без этого сервер не может занять http://+:8080/ и отступает на
    http://localhost:8080/ — интерфейс открывается только на самой машине,
    а с любого другого компьютера приходит 400 «Неправильный запрос».
    Агенты при этом не могут отчитаться на сервер.

    Скрипт выдаёт urlacl на порт и открывает его в брандмауэре.
    Запускать от имени администратора один раз на каждую смену порта.

.PARAMETER Port
    Порт веб-интерфейса. По умолчанию 8080.

.PARAMETER User
    От чьего имени работает служба. По умолчанию NT AUTHORITY\SYSTEM —
    так запускается служба, установленная установщиком.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File tools\allow-network.ps1

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File tools\allow-network.ps1 -Port 8090
#>

param(
    [int]$Port = 8080,
    [string]$User = 'NT AUTHORITY\SYSTEM'
)

$ErrorActionPreference = 'Stop'

# Проверка прав: без администратора netsh не изменит urlacl, а ошибка
# будет невнятной, поэтому говорим прямо.
$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole(
    [Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host 'Нужны права администратора: щёлкните по PowerShell правой кнопкой' -ForegroundColor Red
    Write-Host 'и выберите «Запуск от имени администратора», затем повторите команду.' -ForegroundColor Red
    exit 1
}

$url = "http://+:$Port/"
Write-Host "Разрешаю $url для $User ..."

# urlacl: без него HttpListener не отдаёт порт неадминистративному процессу.
& netsh http delete urlacl url=$url 2>$null | Out-Null
& netsh http add urlacl url=$url user="$User" | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Host "Не удалось выдать urlacl на $url" -ForegroundColor Red
    exit 1
}
Write-Host '  urlacl выдан' -ForegroundColor Green

# Брандмауэр: без правила порт закрыт снаружи даже при рабочем urlacl.
$ruleName = "BackupDotNet web $Port"
Get-NetFirewallRule -DisplayName $ruleName -ErrorAction SilentlyContinue | Remove-NetFirewallRule -ErrorAction SilentlyContinue
New-NetFirewallRule -DisplayName $ruleName -Direction Inbound -Protocol TCP -LocalPort $Port -Action Allow -Profile Any | Out-Null
Write-Host "  правило брандмауэра «$ruleName» создано" -ForegroundColor Green

# Проверка: слушает ли кто-то порт прямо сейчас и на каком адресе.
$conn = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue
if ($conn) {
    $addrs = ($conn | Select-Object -ExpandProperty LocalAddress -Unique) -join ', '
    Write-Host "Сейчас порт слушает: $addrs"
    if ($addrs -match '127\.0\.0\.1|::1' -and $addrs -notmatch '0\.0\.0\.0|::') {
        Write-Host '  Это только локальная привязка. Перезапустите сервер, чтобы он занял http://+:порт/' -ForegroundColor Yellow
    }
} else {
    Write-Host 'Порт сейчас никто не слушает — запустите сервер.'
}

$hostName = $env:COMPUTERNAME
Write-Host ''
Write-Host "Готово. Проверка: http://${hostName}:$Port/api/health"
Write-Host 'В ответе должно быть "localOnly": false.'
