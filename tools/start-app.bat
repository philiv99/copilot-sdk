@echo off
setlocal enabledelayedexpansion

echo ============================================
echo Starting Copilot SDK Application...
echo ============================================
echo.

:: Start backend in background
echo Starting Backend API...
echo Backend will run in a separate window...
start "CopilotSdk.Api" cmd /c "%~dp0start-backend.bat"
if errorlevel 1 (
    echo WARNING: Failed to start backend window
)

:: Wait a few seconds for the backend to initialize
echo.
echo Waiting for backend to start (5 seconds)...
timeout /t 5 /nobreak > nul
echo Backend should be ready.

:: Check if backend is responding
echo.
echo Checking if backend is responding...
powershell -Command "try { $response = Invoke-WebRequest -Uri 'http://localhost:5139/api/copilot/client/status' -Method GET -TimeoutSec 5 -ErrorAction Stop; Write-Host 'Backend is responding!' } catch { Write-Host 'WARNING: Backend may not be ready yet. Error:' $_.Exception.Message }"

:: Open browser to frontend URL
:: echo Opening browser...
:: start http://localhost:3000

:: Start frontend (this will block until closed)
echo.
echo ============================================
echo Starting Frontend...
echo ============================================
call "%~dp0start-frontend.bat"

echo.
echo Frontend has exited.
pause
