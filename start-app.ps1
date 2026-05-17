param(
    [int]$TimeoutSeconds = 60,
    [string]$BackendUrl = "http://localhost:5139",
    [string]$HealthPath = "/api/copilot/client/status"
)

$ErrorActionPreference = "Stop"

$repoRoot = $PSScriptRoot
$backendDir = Join-Path $repoRoot "src\CopilotSdk.Api"
$frontendDir = Join-Path $repoRoot "src\CopilotSdk.Web"
$healthUrl = "$BackendUrl$HealthPath"

function Test-BackendReady {
    param([string]$Uri)

    try {
        Invoke-WebRequest -Uri $Uri -Method Get -TimeoutSec 3 -UseBasicParsing | Out-Null
        return $true
    }
    catch {
        return $false
    }
}

if (-not (Test-Path $backendDir)) {
    throw "Backend directory not found: $backendDir"
}

if (-not (Test-Path $frontendDir)) {
    throw "Frontend directory not found: $frontendDir"
}

Write-Host "Starting Copilot SDK backend..."

$backendProcess = $null
if (Test-BackendReady -Uri $healthUrl) {
    Write-Host "Backend is already responding at $healthUrl"
}
else {
    $backendCommand = "Set-Location -LiteralPath '$backendDir'; dotnet run --launch-profile http"
    $backendProcess = Start-Process powershell `
        -ArgumentList "-NoExit", "-NoProfile", "-ExecutionPolicy", "Bypass", "-Command", $backendCommand `
        -PassThru

    Write-Host "Waiting for backend readiness at $healthUrl..."
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)

    while ((Get-Date) -lt $deadline) {
        if (Test-BackendReady -Uri $healthUrl) {
            Write-Host "Backend is ready."
            break
        }

        if ($backendProcess.HasExited) {
            throw "Backend process exited before becoming ready. Exit code: $($backendProcess.ExitCode)"
        }

        Start-Sleep -Seconds 1
    }

    if (-not (Test-BackendReady -Uri $healthUrl)) {
        throw "Backend did not become ready within $TimeoutSeconds seconds."
    }
}

Write-Host "Starting Copilot SDK frontend..."
Push-Location -LiteralPath $frontendDir
try {
    if (-not (Test-Path "node_modules")) {
        Write-Host "node_modules not found. Running npm install..."
        npm install
    }

    npm start
}
finally {
    Pop-Location
}
