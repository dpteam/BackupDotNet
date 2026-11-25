@echo off
rem ============================================================
rem  BackupDotNet - run the production instance from bin\
rem
rem  Usage: run.cmd                (web UI on http://localhost:8080/)
rem         run.cmd --once         (single pass over all checks)
rem         run.cmd --check acme   (single company)
rem         run.cmd --help
rem ============================================================
setlocal

set "ROOT=%~dp0"
cd /d "%ROOT%bin"

if not exist "BackupDotNet.exe" (
  echo BackupDotNet.exe not found. Run build.cmd first.
  exit /b 1
)

if "%~1"=="" (
  rem Предупреждаем заранее: без прав администратора HTTP.sys не отдаст
  rem порт на все адреса, и сервер отступит на localhost. Тогда интерфейс
  rem открывается только здесь, а с других машин приходит 400.
  net session >nul 2>&1
  if errorlevel 1 (
    echo [i] Запуск без прав администратора: сервер сможет занять порт
    echo     только на localhost. Для доступа по сети один раз запустите
    echo     allow-network.cmd от имени администратора.
    echo.
  )
  BackupDotNet.exe --console
) else (
  BackupDotNet.exe %*
)

endlocal
