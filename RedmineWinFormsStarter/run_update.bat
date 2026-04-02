@echo off
setlocal EnableExtensions

REM LINKTAG RMBAT002
REM LINKTAG RMPY001
REM Usage: run_update.bat <RedmineURL> <ApiKey> <CsvPath>
set "BASE=%~1"
set "KEY=%~2"
set "CSV=%~3"

if "%BASE%"=="" goto :usage
if "%KEY%"=="" goto :usage
if "%CSV%"=="" goto :usage

if not exist "%CSV%" (
  echo [ERROR] CSV not found: %CSV%
  exit /b 2
)

set "SCRIPT_DIR=%~dp0Scripts"
set "PY=%SCRIPT_DIR%\python.exe"
set "PY_SCRIPT=%SCRIPT_DIR%\redmine_update_from_csv.py"

if not exist "%PY%" (
  echo [ERROR] python.exe not found: %PY%
  exit /b 3
)
if not exist "%PY_SCRIPT%" (
  echo [ERROR] redmine_update_from_csv.py not found: %PY_SCRIPT%
  exit /b 4
)

echo === RUN --apply ===
"%PY%" "%PY_SCRIPT%" --base-url "%BASE%" --api-key "%KEY%" --csv "%CSV%" --apply --ignore-version
set "RC=%ERRORLEVEL%"
echo === EXIT %RC% ===
exit /b %RC%

:usage
echo [USAGE] %~nx0 "RedmineURL" "ApiKey" "CsvPath"
exit /b 1