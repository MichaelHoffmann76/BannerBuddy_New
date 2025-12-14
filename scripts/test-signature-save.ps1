# Test: Signatur-Speicherung direkt testen (ohne UI)

$dllPath = Join-Path $PSScriptRoot "..\BannerBuddy.AddIn\bin\Debug\net48\BannerBuddy.AddIn.dll"

if (-not (Test-Path $dllPath)) {
    Write-Host "DLL nicht gefunden: $dllPath" -ForegroundColor Red
    exit 1
}

Write-Host "Lade DLL: $dllPath" -ForegroundColor Cyan
Add-Type -Path $dllPath

Write-Host "Erstelle Test-DTO..." -ForegroundColor Cyan
$dto = New-Object BannerBuddy.AddIn.Models.SignatureInputDto
$dto.Greeting = "Beste Grüße"
$dto.Hashtag = "#TestHashtag"
$dto.Prefix = "i. V."
$dto.Name = "TEST ERFOLREICH - $(Get-Date -Format 'HH:mm:ss')"
$dto.Role = "Test-Rolle"
$dto.ShowSkyline = $false
$dto.Company = "Test GmbH"
$dto.Branch = "Test Branch"
$dto.Street = "Teststr. 123"
$dto.ZipCity = "12345 Teststadt"
$dto.Phone = "+49 123 456789"
$dto.Mobile = "+49 987 654321"
$dto.Email = "test@test.de"
$dto.Website = "www.test.de"
$dto.ManagingDirectors = "Test Director"
$dto.RegisterInfo = "Test Register"
$dto.Jurisdiction = "Test Stadt"
$dto.LinkedInUrl = "https://linkedin.com/test"
$dto.TwitterUrl = "https://twitter.com/test"
$dto.ShowEnvironmentHint = $true

Write-Host "Rufe SaveSignature auf..." -ForegroundColor Cyan
try {
    $coordinator = New-Object BannerBuddy.AddIn.Core.SignatureCoordinator
    $coordinator.SaveSignature($dto)
    Write-Host "SUCCESS! SaveSignature wurde ohne Fehler ausgeführt." -ForegroundColor Green
} catch {
    Write-Host "FEHLER beim Speichern:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host $_.Exception.StackTrace -ForegroundColor Gray
    exit 1
}

Write-Host "`nPrüfe Signatur-Datei..." -ForegroundColor Cyan
Start-Sleep -Milliseconds 500

& "$PSScriptRoot\diagnose-signature.ps1"
