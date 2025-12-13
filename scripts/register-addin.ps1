<#
Registriert das BannerBuddy Outlook COM-Addin (HKCU) und ruft regasm auf.
Benutzung (aus dem Projektordner):
  powershell -ExecutionPolicy Bypass -File .\scripts\register-addin.ps1
Optional: -DllPath "C:\voll\er\pfad\BannerBuddy.AddIn.dll"

Hinweis: regasm erfordert Administratorrechte für die DLL-Registrierung (/codebase).
Das Skript legt die Registry-Einträge in HKCU an (kein Admin benötigt),
versucht aber nur regasm auszuführen, wenn die Shell als Administrator läuft.
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

if (-not (Test-Path $DllPath)) {
    Write-Err "DLL nicht gefunden: $DllPath"
    Write-Host "Bitte DLL-Pfad angeben, z.B. -DllPath 'C:\...\BannerBuddy.AddIn.dll'"
    exit 2
}

$regKeys = @(
    'HKCU:\Software\Microsoft\Office\16.0\Outlook\Addins\BannerBuddy.AddIn',
    'HKCU:\Software\Microsoft\Office\Outlook\Addins\BannerBuddy.AddIn'
)

foreach ($regKey in $regKeys) {
    Write-Info "Erstelle/aktualisiere Registry-Key: $regKey"
    # Ensure the key is clean (Outlook sometimes leaves binary values behind)
    if (Test-Path $regKey) {
        Remove-Item -Path $regKey -Recurse -Force
    }
    New-Item -Path $regKey -Force | Out-Null
    New-ItemProperty -Path $regKey -Name FriendlyName -PropertyType String -Value 'BannerBuddy - Signature Banner Manager' -Force | Out-Null
    New-ItemProperty -Path $regKey -Name Description -PropertyType String -Value 'Zeitgesteuerte Banner und Urlaubsclaims fuer Outlook-Signaturen' -Force | Out-Null
    New-ItemProperty -Path $regKey -Name LoadBehavior -PropertyType DWord -Value 3 -Force | Out-Null
}

# Outlook Resiliency: remove entries that disable/crash this add-in (development convenience)
$resBase = 'HKCU:\Software\Microsoft\Office\16.0\Outlook\Resiliency'
$crashKey = Join-Path $resBase 'CrashingAddinList'
$disabledItemsKey = Join-Path $resBase 'DisabledItems'

function Decode-UnicodeFromBinary([byte[]]$bytes) {
    try { [System.Text.Encoding]::Unicode.GetString($bytes) } catch { '' }
}

foreach ($key in @($crashKey, $disabledItemsKey)) {
    if (Test-Path $key) {
        $props = Get-ItemProperty -Path $key
        foreach ($p in $props.PSObject.Properties) {
            if ($p.Name -in @('PSPath','PSParentPath','PSChildName','PSDrive','PSProvider')) { continue }
            $value = $p.Value
            if ($value -is [byte[]]) {
                $text = (Decode-UnicodeFromBinary $value).ToLowerInvariant()
                if ($text.Contains('bannerbuddy.addin') -or $text.Contains($DllPath.ToLowerInvariant())) {
                    Write-Info "Entferne Resiliency-Eintrag $($p.Name) aus $key"
                    Remove-ItemProperty -Path $key -Name $p.Name -ErrorAction SilentlyContinue
                }
            }
        }
    }
}

# Prevent auto-disable while developing
$dndKey = Join-Path $resBase 'DoNotDisableAddinList'
New-Item -Path $dndKey -Force | Out-Null
New-ItemProperty -Path $dndKey -Name 'BannerBuddy.AddIn' -PropertyType DWord -Value 1 -Force | Out-Null

# Policy allow-list (some Outlook builds only honor resiliency exemptions via Policies)
$policyKey = 'HKCU:\Software\Policies\Microsoft\Office\16.0\Outlook\Resiliency\AddinList'
# Policy-Allowlist (optional; kann durch IT-Richtlinien gesperrt sein)
try {
    New-Item -Path $policyKey -Force -ErrorAction Stop | Out-Null
    New-ItemProperty -Path $policyKey -Name 'BannerBuddy.AddIn' -PropertyType DWord -Value 1 -Force -ErrorAction Stop | Out-Null
}
catch {
    Write-Warning "Konnte Policy-Allowlist nicht setzen (vermutlich gesperrt): $($_.Exception.Message)"
}

# regasm
$regasmPath = Join-Path $env:windir 'Microsoft.NET\Framework64\v4.0.30319\regasm.exe'
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not (Test-Path $regasmPath)) {
    Write-Err "regasm nicht gefunden: $regasmPath"
    Write-Host "Installiere das .NET Framework Developer Pack bzw. verwende die Visual Studio Developer PowerShell."
    Write-Host "Du kannst die DLL manuell registrieren (als Administrator):"
    Write-Host ('"' + $regasmPath + '" "' + $DllPath + '" /codebase')
    exit 3
}

if ($isAdmin) {
    Write-Info "Shell ist Administrator - fuehre regasm aus"
    & "$regasmPath" "$DllPath" /codebase
    if ($LASTEXITCODE -eq 0) { Write-Info 'regasm erfolgreich.' } else { Write-Err "regasm exitcode: $LASTEXITCODE"; exit $LASTEXITCODE }
} else {
    Write-Host "Registry-Eintraege angelegt. regasm konnte nicht ausgefuehrt werden, weil diese Shell keine Administratorrechte hat." -ForegroundColor Yellow
    Write-Host "Fuehre als Administrator aus, um die DLL zu registrieren:" -ForegroundColor Cyan
    Write-Host ('"' + $regasmPath + '" "' + $DllPath + '" /codebase') -ForegroundColor White
}

Write-Info "Fertig. Outlook neu starten und unter Datei -> Optionen -> Add-Ins pruefen."
