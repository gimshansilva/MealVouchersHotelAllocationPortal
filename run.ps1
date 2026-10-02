$ErrorActionPreference = 'Stop'
$port = 5080
Write-Host 'Meal Vouchers & Hotel Allocation Portal' -ForegroundColor Cyan
& powershell -NoProfile -ExecutionPolicy Bypass -File "$PSScriptRoot\scripts\free-port.ps1" -Port $port
if ($LASTEXITCODE -ne 0) { exit 1 }
Start-Job { Start-Sleep -Seconds 6; Start-Process 'http://localhost:5080' } | Out-Null
Set-Location "$PSScriptRoot\backend"
dotnet run --urls "http://localhost:$port"
