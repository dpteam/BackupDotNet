@echo off
rem ============================================================
rem  BackupDotNet - открыть веб-интерфейс для доступа по сети
rem
rem  Запускать ОТ ИМЕНИ АДМИНИСТРАТОРА один раз (и после смены порта).
rem
rem  Без этого сервер слушает только localhost: интерфейс открывается
rem  на самой машине, а с других компьютеров приходит 400, и агенты
rem  не могут отчитаться.
rem
rem  Usage: allow-network.cmd            (порт 8080)
rem         allow-network.cmd 8090       (другой порт)
rem ============================================================
setlocal

set "PORT=%~1"
if "%PORT%"=="" set "PORT=8080"

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0tools\allow-network.ps1" -Port %PORT%
pause

endlocal
