@echo off
rem ============================================================
rem  BackupDotNet - build the production instance into bin\
rem
rem  bin\ is the working copy you actually run: binaries, live
rem  config and data. It is fully ignored by git.
rem
rem  Usage: build.cmd
rem         build.cmd -NoBuild          (sync only)
rem         build.cmd -ResetBranding    (restore DPT branding)
rem ============================================================
setlocal

set "ROOT=%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -File "%ROOT%tools\build-prod.ps1" %*
if errorlevel 1 (
  echo.
  echo [ERROR] build failed.
  exit /b 1
)

endlocal
