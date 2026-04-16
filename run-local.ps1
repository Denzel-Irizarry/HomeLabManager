param(
    [switch]$NoRestore
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$apiProject = Join-Path $repoRoot "HomeLabManager.API/HomeLabManager.API.csproj"
$webProject = Join-Path $repoRoot "HomeLabManager.WEBUI/HomeLabManager.WEBUI.csproj"

Write-Host "Starting HomeLabManager local stack..." -ForegroundColor Cyan

if (-not $NoRestore) {
    Write-Host "Running dotnet restore..." -ForegroundColor Yellow
    dotnet restore $repoRoot
}

$apiArgs = "run --project `"$apiProject`""
$webArgs = "run --project `"$webProject`""

$apiProcess = Start-Process -FilePath "dotnet" -ArgumentList $apiArgs -WorkingDirectory $repoRoot -PassThru
$webProcess = Start-Process -FilePath "dotnet" -ArgumentList $webArgs -WorkingDirectory $repoRoot -PassThru

Write-Host "API PID: $($apiProcess.Id)" -ForegroundColor Green
Write-Host "WEBUI PID: $($webProcess.Id)" -ForegroundColor Green
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
}
if ($webProcess.HasExited) {
    Write-Warning "WEBUI process exited with code $($webProcess.ExitCode)."
}
