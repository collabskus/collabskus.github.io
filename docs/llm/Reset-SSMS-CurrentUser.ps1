# Reset-SSMS-CurrentUser.ps1
# Resets SSMS settings for the CURRENT USER only. No admin rights needed.

# Make sure SSMS is closed
Get-Process -Name "Ssms" -ErrorAction SilentlyContinue | Stop-Process -Force

$stamp     = Get-Date -Format "yyyyMMdd-HHmmss"
$backupDir = Join-Path $env:USERPROFILE "Desktop\SSMS-UserData-Backup-$stamp"
New-Item -ItemType Directory -Path $backupDir -Force | Out-Null

# --- 1. File system locations (per-user only) ---
$paths = @(
    "$env:APPDATA\Microsoft\SQL Server Management Studio",
    "$env:APPDATA\Microsoft\SSMS",
    "$env:LOCALAPPDATA\Microsoft\SQL Server Management Studio",
    "$env:LOCALAPPDATA\Microsoft\SSMS",
    "$env:LOCALAPPDATA\Microsoft\SSMSBackup"
)

foreach ($p in $paths) {
    if (Test-Path $p) {
        $dest = Join-Path $backupDir ($p -replace '[:\\]', '_')
        Write-Host "Moving $p" -ForegroundColor Yellow
        Move-Item -Path $p -Destination $dest -Force
    }
}

# --- 2. Registry (HKEY_CURRENT_USER only) ---
$regKeys = @(
    "HKCU:\Software\Microsoft\SQL Server Management Studio",
    "HKCU:\Software\Microsoft\SSMS",
    "HKCU:\Software\Microsoft\SSMSE"
)

foreach ($k in $regKeys) {
    if (Test-Path $k) {
        $name    = ($k -replace '[:\\]', '_')
        $regFile = Join-Path $backupDir "$name.reg"
        $nativeKey = $k -replace '^HKCU:', 'HKCU'
        Write-Host "Exporting and removing $k" -ForegroundColor Yellow
        reg export $nativeKey $regFile /y | Out-Null
        Remove-Item -Path $k -Recurse -Force
    }
}

Write-Host ""
Write-Host "Done. Backup saved to: $backupDir" -ForegroundColor Green
Write-Host "Start SSMS again; it will rebuild fresh settings for your user."
