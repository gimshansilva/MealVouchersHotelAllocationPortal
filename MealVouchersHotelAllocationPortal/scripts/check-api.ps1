# Tests the API and prints PASS / FAIL for each call.
# Usage (app must be running with F5):   .\scripts\check-api.ps1
#        add -Write to also add and then hide a test flight.
param(
    [string]$Base = "http://localhost:5080",
    [string]$Username = "pramodi",
    [string]$Password = "Password@123",
    [switch]$Write
)

function Test-Call([string]$Name, [scriptblock]$Call) {
    try {
        $result = & $Call
        Write-Host "PASS  $Name" -ForegroundColor Green
        return $result
    } catch {
        $detail = $_.ErrorDetails.Message
        if (-not $detail) { $detail = $_.Exception.Message }
        if ($detail.Length -gt 400) { $detail = $detail.Substring(0, 400) }
        Write-Host "FAIL  $Name" -ForegroundColor Red
        Write-Host "      $detail" -ForegroundColor Yellow
        return $null
    }
}

$health = Test-Call "GET    /api/health" { Invoke-RestMethod "$Base/api/health" }
if (-not $health) {
    Write-Host "`nThe API is not running at $Base. Press F5 in VS Code and wait for 'Now listening on', then run this again." -ForegroundColor Red
    exit 1
}

$login = Test-Call "POST   /api/auth/login" {
    Invoke-RestMethod -Method Post -Uri "$Base/api/auth/login" -ContentType 'application/json' `
        -Body (@{ username = $Username; password = $Password } | ConvertTo-Json)
}
if (-not $login) {
    Write-Host "`nLogin failed, so every other call would fail too. Read the message above and the Debug Console in VS Code." -ForegroundColor Red
    exit 1
}
Write-Host "      logged in as $($login.user.username) (role: $($login.user.role))"
$h = @{ Authorization = "Bearer $($login.token)" }

$flights = Test-Call "GET    /api/flights" { Invoke-RestMethod "$Base/api/flights" -Headers $h }
if ($flights) { Write-Host "      $(@($flights).Count) flight(s)" }
$mine = Test-Call "GET    /api/meal-requests" { Invoke-RestMethod "$Base/api/meal-requests" -Headers $h }
if ($null -ne $mine) { Write-Host "      $(@($mine).Count) request(s) for this user" }
$users = Test-Call "GET    /api/users" { Invoke-RestMethod "$Base/api/users" -Headers $h }
if ($users) { Write-Host "      $(@($users).Count) user(s)" }
$all = Test-Call "GET    /api/admin/meal-requests" { Invoke-RestMethod "$Base/api/admin/meal-requests" -Headers $h }
if ($null -ne $all) { Write-Host "      $(@($all).Count) request(s) in total" }

if ($Write) {
    $body = '{"flightNo":"TEST1","from":"CMB","to":"MLE","pax":100,"std":"2026-12-01T08:00:00","etd":"2026-12-01T09:00:00"}'
    $new = Test-Call "POST   /api/flights" { Invoke-RestMethod -Method Post -Uri "$Base/api/flights" -Headers $h -ContentType 'application/json' -Body $body }
    if ($new) {
        Test-Call "DELETE /api/flights/$($new.id)" { Invoke-RestMethod -Method Delete -Uri "$Base/api/flights/$($new.id)" -Headers $h } | Out-Null
    }
}
Write-Host "`nDone." -ForegroundColor Cyan
