# BannerBuddy_AddIn

BannerBuddy — Outlook COM Add-in (NET Framework 4.8) zur zeitgesteuerten Einfügung von Bannern und Urlaubs-Hinweisen in Outlook-Signaturen.

Dieses Repository enthält die Arbeitskopie des Add-Ins inklusive WPF-UI, Tray-Service und Ribbon-Integration.

## Lokale Vorbereitung

1. Build:

```powershell
# im Projektordner
dotnet build -c Debug
```

2. Registrierung (nur einmal, erfordert Admin für regasm):

```powershell
# Aus dem Projekt-Root
powershell -ExecutionPolicy Bypass -File .\scripts\register-addin.ps1
```

## Auf GitHub pushen

Du kannst das Repo manuell auf GitHub anlegen oder `gh` verwenden.

Mit GitHub CLI (`gh` angemeldet):

```powershell
gh repo create MichaelHoffmann76/BannerBuddy_New --public --source . --remote origin --push
```

Oder manuell: Erstelle ein neues Repo `BannerBuddy_New` unter deinem GitHub-Account, dann:

```powershell
git remote add origin https://github.com/MichaelHoffmann76/BannerBuddy_New.git
git branch -M main
git push -u origin main
```

## Hinweise
- Die DLL muss per `regasm /codebase` registriert werden, damit Outlook das COM-Addin lädt.
- Die eingebaute Logging-Datei: `%TEMP%\BannerBuddy.AddIn.log` hilft beim Debugging.
