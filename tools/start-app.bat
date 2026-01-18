@echo off
echo Starting Copilot SDK Application...
echo.

:: Start backend in background
echo Starting Backend API...
start "CopilotSdk.Api" cmd /c "%~dp0start-backend.bat"

:: Wait a few seconds for the backend to initialize
echo Waiting for backend to start...
timeout /t 5 /nobreak > nul

:: Open browser to frontend URL
:: echo Opening browser...
:: start http://localhost:3000

:: Start frontend (this will block until closed)
echo Starting Frontend...
call "%~dp0start-frontend.bat"
