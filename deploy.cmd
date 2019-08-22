@if "%SCM_TRACE_LEVEL%" NEQ "4" @echo off

IF "%SITE_ROLE%" == "app" (
  deploy.app.cmd
) ELSE (
  IF "%SITE_ROLE%" == "function" (
    deploy.function.cmd
  ) ELSE (
    echo You have to set SITE_ROLE setting to either "app" or "function"
    exit /b 1
  )
)