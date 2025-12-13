param(
    [string]$GitHubUser = 'MichaelHoffmann76',
    [string]$RepoName = 'BannerBuddy_New',
    [switch]$Public
)

# Script to create a GitHub repo using gh (GitHub CLI) or instruct manual push.
# Usage:
#   .\push-to-github.ps1 -Public

if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    Write-Host "GitHub CLI (gh) nicht gefunden. Führe stattdessen die manuellen Schritte in README.md aus." -ForegroundColor Yellow
    return
}

$visibility = $Public.IsPresent ? '--public' : '--private'
$cmd = "gh repo create $GitHubUser/$RepoName $visibility --source . --remote origin --push"
Write-Host "Executing: $cmd"
Invoke-Expression $cmd
