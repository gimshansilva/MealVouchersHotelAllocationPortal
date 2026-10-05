# Imports flights from a JSON file through the API.  Usage:  .\scripts\import-flights.ps1 -File .\scripts\flights-sample.json
param(
    [string]$File = "$PSScriptRoot\flights-sample.json",
    [string]$Base = "http://localhost:5080",
    [string]$Username = "pramodi",
    [string]$Password = "Password@123"
)
$ErrorActionPreference = 'Stop'
$login = Invoke-RestMethod -Method Post -Uri "$Base/api/auth/login" -ContentType 'application/json' `
    -Body (@{ username = $Username; password = $Password } | ConvertTo-Json)
$headers = @{ Authorization = "Bearer $($login.token)" }
$result = Invoke-RestMethod -Method Post -Uri "$Base/api/flights/bulk" -Headers $headers -ContentType 'application/json' `
    -Body (Get-Content $File -Raw)
Write-Host "Added $(@($result).Count) flight(s)." -ForegroundColor Green
