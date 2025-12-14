# Diagnose: Signature-Datei analysieren

$sigFolder = Join-Path $env:APPDATA "Microsoft\Signatures"
Write-Host "Signature-Ordner: $sigFolder" -ForegroundColor Cyan

if (-not (Test-Path $sigFolder)) {
    Write-Host "Ordner existiert nicht!" -ForegroundColor Red
    exit 1
}

$htmFiles = Get-ChildItem -Path $sigFolder -Filter "*.htm" | Sort-Object LastWriteTime -Descending
if ($htmFiles.Count -eq 0) {
    Write-Host "Keine .htm Dateien gefunden!" -ForegroundColor Red
    exit 1
}

$primary = $htmFiles[0]
Write-Host "`nPrimäre Signatur: $($primary.Name)" -ForegroundColor Green
Write-Host "Letzte Änderung: $($primary.LastWriteTime)" -ForegroundColor Gray

$content = Get-Content -Path $primary.FullName -Raw -Encoding UTF8

Write-Host "`n=== MARKER-STATUS ===" -ForegroundColor Yellow
Write-Host "BANNER_START:    $($content.Contains('<!-- BANNER_BUDDY_BANNER_START -->'))" -ForegroundColor $(if ($content.Contains('<!-- BANNER_BUDDY_BANNER_START -->')) { 'Green' } else { 'Red' })
Write-Host "BANNER_END:      $($content.Contains('<!-- BANNER_BUDDY_BANNER_END -->'))" -ForegroundColor $(if ($content.Contains('<!-- BANNER_BUDDY_BANNER_END -->')) { 'Green' } else { 'Red' })
Write-Host "VACATION_START:  $($content.Contains('<!-- BANNER_BUDDY_VACATION_START -->'))" -ForegroundColor $(if ($content.Contains('<!-- BANNER_BUDDY_VACATION_START -->')) { 'Green' } else { 'Red' })
Write-Host "VACATION_END:    $($content.Contains('<!-- BANNER_BUDDY_VACATION_END -->'))" -ForegroundColor $(if ($content.Contains('<!-- BANNER_BUDDY_VACATION_END -->')) { 'Green' } else { 'Red' })
Write-Host "SIGNATURE_START: $($content.Contains('<!-- BANNER_BUDDY_SIGNATURE_START -->'))" -ForegroundColor $(if ($content.Contains('<!-- BANNER_BUDDY_SIGNATURE_START -->')) { 'Green' } else { 'Red' })
Write-Host "SIGNATURE_END:   $($content.Contains('<!-- BANNER_BUDDY_SIGNATURE_END -->'))" -ForegroundColor $(if ($content.Contains('<!-- BANNER_BUDDY_SIGNATURE_END -->')) { 'Green' } else { 'Red' })

Write-Host "`n=== SIGNATUR-BLOCK ===" -ForegroundColor Yellow
$sigStart = $content.IndexOf('<!-- BANNER_BUDDY_SIGNATURE_START -->')
$sigEnd = $content.IndexOf('<!-- BANNER_BUDDY_SIGNATURE_END -->')

if ($sigStart -ge 0 -and $sigEnd -gt $sigStart) {
    $blockStart = $sigStart + '<!-- BANNER_BUDDY_SIGNATURE_START -->'.Length
    $block = $content.Substring($blockStart, $sigEnd - $blockStart)
    Write-Host "Inhalt zwischen Markern:" -ForegroundColor Cyan
    Write-Host $block -ForegroundColor White
} else {
    Write-Host "Signatur-Block NICHT GEFUNDEN!" -ForegroundColor Red
}

Write-Host "`n=== DATEI-GRÖSSE ===" -ForegroundColor Yellow
Write-Host "$($content.Length) Zeichen" -ForegroundColor Gray
