param([int]$Port = 5080)
$conn = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $conn) { Write-Host "Port $Port is free."; exit 0 }
$proc = Get-Process -Id $conn.OwningProcess -ErrorAction SilentlyContinue
if ($proc -and ($proc.ProcessName -like 'MealVouchersHotelAllocation*' -or $proc.ProcessName -eq 'dotnet')) {
    Write-Host "Port $Port was held by an old run ('$($proc.ProcessName)', PID $($proc.Id)). Stopping it."
    Stop-Process -Id $proc.Id -Force
    Start-Sleep -Seconds 1
    exit 0
}
Write-Host "Port $Port is used by '$($proc.ProcessName)' (PID $($conn.OwningProcess)). Close that program or change the port." -ForegroundColor Red
exit 1
