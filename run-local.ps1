param(
    [switch]$NoRestore
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$solution = Join-Path $repoRoot "HomeLabManager.slnx"
$apiProject = Join-Path $repoRoot "HomeLabManager.API/HomeLabManager.API.csproj"
$webProject = Join-Path $repoRoot "HomeLabManager.WEBUI/HomeLabManager.WEBUI.csproj"
$apiUrl = "http://localhost:5015"
$webUrl = "http://localhost:5282"
$apiOutLog = Join-Path $repoRoot ".homelab-api.log"
$apiErrLog = Join-Path $repoRoot ".homelab-api.err.log"
$webOutLog = Join-Path $repoRoot ".homelab-webui.log"
$webErrLog = Join-Path $repoRoot ".homelab-webui.err.log"
$localAppData = Join-Path $repoRoot ".localappdata"

Write-Host "Starting HomeLabManager local stack..." -ForegroundColor Cyan

if (-not $NoRestore) {
    Write-Host "Running dotnet restore..." -ForegroundColor Yellow
    dotnet restore $solution
}

foreach ($log in @($apiOutLog, $apiErrLog, $webOutLog, $webErrLog)) {
    if (Test-Path $log) {
        Remove-Item $log -Force
    }
}

New-Item -ItemType Directory -Path $localAppData -Force | Out-Null

$apiArgs = "run --no-launch-profile --project `"$apiProject`" --no-restore -p:UseSharedCompilation=false"
$webArgs = "run --no-launch-profile --project `"$webProject`" --no-restore -p:UseSharedCompilation=false"

$apiEnvironment = @{
    ASPNETCORE_ENVIRONMENT = "Development"
    DOTNET_ENVIRONMENT = "Development"
    ASPNETCORE_URLS = $apiUrl
    ASPNETCORE_HTTPS_PORT = ""
    LOCALAPPDATA = $localAppData
}

$webEnvironment = @{
    ASPNETCORE_ENVIRONMENT = "Development"
    DOTNET_ENVIRONMENT = "Development"
    ASPNETCORE_URLS = $webUrl
    ASPNETCORE_HTTPS_PORT = ""
    Api__BaseUrl = $apiUrl
    LOCALAPPDATA = $localAppData
}

$apiProcess = Start-Process -FilePath "dotnet" -ArgumentList $apiArgs -WorkingDirectory $repoRoot -Environment $apiEnvironment -RedirectStandardOutput $apiOutLog -RedirectStandardError $apiErrLog -WindowStyle Hidden -PassThru
$webProcess = Start-Process -FilePath "dotnet" -ArgumentList $webArgs -WorkingDirectory $repoRoot -Environment $webEnvironment -RedirectStandardOutput $webOutLog -RedirectStandardError $webErrLog -WindowStyle Hidden -PassThru

Write-Host "API PID: $($apiProcess.Id)" -ForegroundColor Green
Write-Host "WEBUI PID: $($webProcess.Id)" -ForegroundColor Green
Write-Host "API URL: $apiUrl" -ForegroundColor Green
Write-Host "WEBUI URL: $webUrl" -ForegroundColor Green
Write-Host "Press Ctrl+C to stop. If one process exits, the script stops the other." -ForegroundColor DarkYellow

try {
    while ($true) {
        if ($apiProcess.HasExited -or $webProcess.HasExited) {
            break
        }
        Start-Sleep -Seconds 1
        $apiProcess.Refresh()
        $webProcess.Refresh()
    }
}
finally {
    foreach ($p in @($apiProcess, $webProcess)) {
        if ($null -ne $p -and -not $p.HasExited) {
            Stop-Process -Id $p.Id -Force
        }
    }
}

if ($apiProcess.HasExited) {
    Write-Warning "API process exited with code $($apiProcess.ExitCode)."
    if (Test-Path $apiErrLog) {
        Get-Content $apiErrLog -Tail 40
    }
}
if ($webProcess.HasExited) {
    Write-Warning "WEBUI process exited with code $($webProcess.ExitCode)."
    if (Test-Path $webErrLog) {
        Get-Content $webErrLog -Tail 40
    }
}
