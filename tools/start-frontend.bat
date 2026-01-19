@echo off
setlocal enabledelayedexpansion

echo ============================================
echo Starting React Frontend...
echo ============================================
echo.

:: Change to frontend directory
set "FRONTEND_DIR=%~dp0..\src\CopilotSdk.Web"
echo Changing to directory: %FRONTEND_DIR%
cd /d "%FRONTEND_DIR%"
if errorlevel 1 (
    echo ERROR: Failed to change to frontend directory!
    echo Directory may not exist: %FRONTEND_DIR%
    pause
    exit /b 1
)
echo Current directory: %cd%
echo.

:: Check if node_modules exists
if not exist "node_modules\" (
    echo WARNING: node_modules not found! Running npm install...
    call npm install
    if errorlevel 1 (
        echo ERROR: npm install failed!
        pause
        exit /b 1
    )
)

:: Check if package.json exists
if not exist "package.json" (
    echo ERROR: package.json not found in %cd%!
    pause
    exit /b 1
)
echo Found package.json

:: Check Node.js version
echo.
echo Checking Node.js version...
call node --version
echo Node check completed

:: Check npm version
echo Checking npm version...
call npm --version
echo npm check completed

:: Set environment variables for better logging
:: set "BROWSER=none"
:: set "CI=false"

echo.
echo ============================================
echo Running: npm start
echo ============================================
echo.

:: Run npm start - use call to ensure batch continues
call npm start
set NPM_EXIT_CODE=%errorlevel%

echo.
echo ============================================
echo npm start exited with code: %NPM_EXIT_CODE%
echo ============================================

if %NPM_EXIT_CODE% neq 0 (
    echo.
    echo ERROR: Frontend failed to start!
    echo Exit code: %NPM_EXIT_CODE%
    echo.
    echo Possible causes:
    echo - Port 3000 may already be in use
    echo - Missing dependencies (try: npm install)
    echo - Compilation errors in the React code
    echo - Node.js version incompatibility
    echo.
)

pause
exit /b %NPM_EXIT_CODE%

pause
exit /b !NPM_EXIT_CODE!
