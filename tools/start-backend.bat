@echo off
echo Starting .NET Web API Backend...

REM Kill any existing process on port 5139
echo Checking for existing process on port 5139...
powershell -Command "Stop-Process -Id (Get-NetTCPConnection -LocalPort 5139 -ErrorAction SilentlyContinue).OwningProcess -Force -ErrorAction SilentlyContinue"

cd /d "%~dp0..\src\CopilotSdk.Api"
dotnet run
