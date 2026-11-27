# Obevalon Audio Control Panel

A small portable utility for Windows 11 and 10 for quickly switching the default audio output
(e.g. speakers ↔ HDMI projector) and controlling the microphone (volume, mute) –
from a compact window, from the system tray or with global hotkeys.

> Status: feature-complete, before the first stable release. See [docs/SPEC.md](docs/SPEC.md) (in Czech).

### Features
- **Audio outputs**: switch the default output with one click; **Next output** cycles through the outputs you mark
- Custom **names and notes**, your own **order**, hidden outputs; disconnected outputs (projector off) are shown greyed out
- Also switches the default **communications device** (Teams, Discord…) – optional
- **Microphone**: volume slider and one-click mute/unmute, follows the system default microphone or a chosen one
- **System tray**: left click opens the window, right click has quick actions; the icon shows when the microphone is muted
- **Start at sign-in** into the tray – no administrator rights, **no UAC prompts**
- **Global hotkeys** (off by default): next output, mute/unmute microphone, show window
- **Czech and English** UI (automatic or chosen), **light/dark** theme (automatic or chosen)
- **Portable**: settings in `settings.ini` next to `OACP.exe`
- **Automatic updates** from GitHub Releases: you are asked first; downloads are verified (signed manifest + SHA-256)

### Requirements
- Windows 11, or Windows 10 version 1809 and later (x64 or ARM64)
- [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0) – Windows offers the download if it is missing

### Download and use
1. Download `OACP-<version>-win-x64.zip` (or `win-arm64`) from [Releases](https://github.com/vlapri/obevalon_audio_control_panel/releases).
2. Extract it to any folder you can write to (e.g. `C:\Tools\OACP`) and run `OACP.exe`.
3. Optional: *Settings → Start at sign-in*.

On the first start Windows SmartScreen may warn about an unknown publisher (the executable is not code-signed):
choose *More info → Run anyway*.

### Privacy
The only network request is the update check, which downloads the update manifest from this repository's
GitHub Releases (at start and once a day; can be turned off). No telemetry, no user identifiers.

### Building from source
Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

```bash
dotnet build -c Release
```

```bash
dotnet test
```

```bash
dotnet publish src/Obevalon.AudioControlPanel -c Release -r win-x64 -p:PublishSingleFile=true
```

Releases are built by GitHub Actions from version tags – see [docs/RELEASING.md](docs/RELEASING.md).

### License
[GNU GPL v3 or later](LICENSE) © Obevalon

---

## Česky

Malá portable utilita pro Windows 11 a 10 pro rychlé přepínání výchozího audio výstupu
(např. reproduktory ↔ HDMI projektor) a ovládání mikrofonu (hlasitost, ztlumení) –
z kompaktního okna, ze systémové lišty nebo globálními klávesovými zkratkami.

> Stav: všechny funkce hotové, před prvním stabilním vydáním. Viz [docs/SPEC.md](docs/SPEC.md).

### Funkce
- **Audio výstupy**: přepnutí jedním kliknutím; **Další výstup** přepíná dokola mezi označenými výstupy
- Vlastní **názvy a poznámky**, vlastní **pořadí**, skrytí výstupů; odpojené výstupy (vypnutý projektor) jsou šedé
- Volitelně přepíná i **zařízení pro komunikaci** (Teams, Discord…)
- **Mikrofon**: posuvník hlasitosti a ztlumení jedním kliknutím; ovládá výchozí nebo vybraný mikrofon
- **Systray**: levý klik otevře okno, pravý klik nabídne rychlé volby; ikona ukazuje ztlumený mikrofon
- **Spouštění po přihlášení** do systray – bez administrátorských práv, **bez dotazů UAC**
- **Globální klávesové zkratky** (ve výchozím stavu vypnuté): další výstup, ztlumení mikrofonu, zobrazení okna
- **Čeština a angličtina** (automaticky nebo ručně), **světlý/tmavý** motiv (automaticky nebo ručně)
- **Portable**: nastavení v `settings.ini` vedle `OACP.exe`
- **Automatické aktualizace** z GitHub Releases: nejdřív se zeptá; stažení se ověřuje (podepsaný manifest + SHA-256)

### Požadavky
- Windows 11 nebo Windows 10 od verze 1809 (x64 nebo ARM64)
- [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0) – pokud chybí, Windows nabídne stažení

### Stažení a použití
1. Stáhněte `OACP-<verze>-win-x64.zip` (nebo `win-arm64`) ze stránky [Releases](https://github.com/vlapri/obevalon_audio_control_panel/releases).
2. Rozbalte do libovolné složky, do které můžete zapisovat (např. `C:\Tools\OACP`), a spusťte `OACP.exe`.
3. Volitelně: *Nastavení → Spouštět po přihlášení*.

Při prvním spuštění může Windows SmartScreen upozornit na neznámého vydavatele (exe není podepsaný):
zvolte *Další informace → Přesto spustit*.

### Soukromí
Jediný síťový požadavek je kontrola aktualizací – stažení manifestu z GitHub Releases tohoto repozitáře
(při startu a jednou denně; lze vypnout). Žádná telemetrie, žádné identifikátory uživatele.

### Licence
[GNU GPL v3 nebo novější](LICENSE) © Obevalon
