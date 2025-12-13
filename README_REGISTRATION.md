# Schritt 1.8 — Registrierung des Outlook COM-Addins

Kurzanleitung:

- Erstelle zuerst die DLL mit

```powershell
dotnet build BannerBuddy.AddIn\BannerBuddy.AddIn.csproj
```

- Registry-Einträge anlegen und DLL registrieren (wenn möglich als Administrator):

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\register-addin.ps1
# oder mit eigenem Pfad zur DLL:
powershell -ExecutionPolicy Bypass -File .\scripts\register-addin.ps1 -DllPath "C:\voll\er\pfad\BannerBuddy.AddIn.dll"
```

- Falls du nicht als Administrator ausführst: Das Skript legt die HKCU-Einträge an, zeigt aber den `regasm`-Befehl, den du dann als Admin laufen lassen musst:

```
"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\regasm.exe" "C:\Pfad\zu\BannerBuddy.AddIn.dll" /codebase
```

- Zum Entfernen:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\unregister-addin.ps1
# oder mit Pfad:
powershell -ExecutionPolicy Bypass -File .\scripts\unregister-addin.ps1 -DllPath "C:\voll\er\pfad\BannerBuddy.AddIn.dll"
```

Hinweis:
- `regasm` benötigt Administratorrechte.
- Die Registry-Einträge werden unter `HKCU\Software\Microsoft\Office\Outlook\Addins\BannerBuddy.AddIn` angelegt (rückbaubar).
- Nach Registrierung Outlook komplett neu starten und unter Datei → Optionen → Add-Ins prüfen.
