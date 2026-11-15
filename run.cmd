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
  BackupDotNet.exe --console
) else (
  BackupDotNet.exe %*
)

endlocal
