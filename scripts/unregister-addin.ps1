<#
Entfernt die Registry-Einträge und versucht, die DLL mit regasm zu de-registrieren.
Benutzung:
  powershell -ExecutionPolicy Bypass -File .\scripts\unregister-addin.ps1
Optional: -DllPath "C:\voll\er\pfad\BannerBuddy.AddIn.dll"
#>

param(
    [string]$DllPath
)

function Write-Info($m){ Write-Host $m -ForegroundColor Cyan }
function Write-Err($m){ Write-Host $m -ForegroundColor Red }

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
if (-not $DllPath) {
    $candidate = Join-Path $scriptDir "..\BannerBuddy.AddIn\bin\Debug\net48\BannerBuddy.AddIn.dll"
    try { $DllPath = (Resolve-Path $candidate).ProviderPath } catch { $DllPath = $null }
}

$regKeys = @(
    'HKCU:\Software\Microsoft\Office\16.0\Outlook\Addins\BannerBuddy.AddIn',
    'HKCU:\Software\Microsoft\Office\Outlook\Addins\BannerBuddy.AddIn'
)

foreach ($regKey in $regKeys) {
    if (Test-Path $regKey) {
        Write-Info "Entferne Registry-Key: $regKey"
        Remove-Item -Path $regKey -Recurse -Force
    } else {
        Write-Info "Registry-Key nicht vorhanden: $regKey"
    }
}

# Remove dev override
$dndKey = 'HKCU:\Software\Microsoft\Office\16.0\Outlook\Resiliency\DoNotDisableAddinList'
if (Test-Path $dndKey) {
    Remove-ItemProperty -Path $dndKey -Name 'BannerBuddy.AddIn' -ErrorAction SilentlyContinue
}

# Remove policy allow-list
$policyKey = 'HKCU:\Software\Policies\Microsoft\Office\16.0\Outlook\Resiliency\AddinList'
if (Test-Path $policyKey) {
    Remove-ItemProperty -Path $policyKey -Name 'BannerBuddy.AddIn' -ErrorAction SilentlyContinue
}

# regasm uninstall
$regasmPath = Join-Path $env:windir 'Microsoft.NET\Framework64\v4.0.30319\regasm.exe'
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (Test-Path $regasmPath -and $isAdmin -and (Test-Path $DllPath)) {
    Write-Info "Führe regasm /unregister aus"
    & "$regasmPath" "$DllPath" /unregister
    if ($LASTEXITCODE -eq 0) { Write-Info 'regasm /unregister erfolgreich.' } else { Write-Err "regasm exitcode: $LASTEXITCODE"; exit $LASTEXITCODE }
} elseif (-not $isAdmin) {
    Write-Host "Um die DLL komplett zu entfernen, führe diese Shell als Administrator aus und dann:`n`""$regasmPath"" `""$DllPath"" /unregister`" -ForegroundColor Yellow
}

Write-Info "Fertig. Outlook neu starten und Add-Ins prüfen."
