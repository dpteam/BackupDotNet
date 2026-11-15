@echo off
rem ============================================================
rem  BackupDotNet - show the roadmap in a browser
rem
rem  Renders ROADMAP.md into artifacts\roadmap.html and opens it.
rem  Use when the OpenCode review pane is unavailable.
rem ============================================================
setlocal

set "ROOT=%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -File "%ROOT%tools\show-roadmap.ps1" %*
if errorlevel 1 (
  echo.
  echo [ERROR] roadmap render failed.
  exit /b 1
)

endlocal
