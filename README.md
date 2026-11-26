# Obevalon Audio Control Panel

> 🚧 **Work in progress** – the application is being designed. See [docs/SPEC.md](docs/SPEC.md) (in Czech).

A small portable utility for Windows 10/11 for quickly switching the default audio output
(e.g. speakers ↔ HDMI projector) and controlling the microphone (volume, mute) –
from a compact window, from the system tray or with global hotkeys.

### Planned features
- Switch the default audio output with one click, or cycle through selected outputs
- Custom names, notes and ordering of audio outputs
- Microphone volume slider and one-click mute/unmute
- Lives in the system tray, optional start at sign-in (no admin rights, no UAC prompts)
- Portable – settings are stored in `settings.ini` next to the executable
- Czech and English UI, light/dark theme
- Optional global hotkeys
- Automatic update check; updates are installed only after you confirm them

### Requirements
- Windows 11 or Windows 10 version 1809 and later (x64 or ARM64)
- [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0)

### Download
Ready-to-use builds are on the [Releases](https://github.com/vlapri/obevalon_audio_control_panel/releases) page.

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

### Privacy
The only network request the application makes is the update check, which downloads
the update manifest from this repository's GitHub Releases. No telemetry, no user identifiers.

### License
[GNU GPL v3](LICENSE) © Obevalon

---

## Česky

> 🚧 **Ve vývoji** – aplikace se teprve navrhuje. Viz [docs/SPEC.md](docs/SPEC.md).

Malá portable utilita pro Windows 10/11 pro rychlé přepínání výchozího audio výstupu
(např. reproduktory ↔ HDMI projektor) a ovládání mikrofonu (hlasitost, ztlumení).
Ovládá se z kompaktního okna, ze systémové lišty nebo globálními klávesovými zkratkami.

### Plánované funkce
- Přepnutí výchozího audio výstupu jedním kliknutím nebo cyklení mezi vybranými výstupy
- Vlastní názvy, poznámky a pořadí audio výstupů
- Posuvník hlasitosti mikrofonu a ztlumení jedním kliknutím
- Běh v systémové liště, volitelné spuštění po přihlášení (bez admin práv, bez UAC)
- Portable – nastavení se ukládá do `settings.ini` vedle exe
- České a anglické rozhraní, světlý a tmavý motiv
- Volitelné globální klávesové zkratky
- Automatická kontrola aktualizací; instalace proběhne až po vašem potvrzení

### Požadavky
- Windows 11 nebo Windows 10 od verze 1809 (x64 nebo ARM64)
- [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0)

### Stažení
Hotové sestavení najdete na stránce [Releases](https://github.com/vlapri/obevalon_audio_control_panel/releases).
Postup sestavení ze zdrojových kódů je v anglické části výše.

### Soukromí
Jediný síťový požadavek aplikace je kontrola aktualizací: stažení manifestu z GitHub Releases
tohoto repozitáře. Žádná telemetrie, žádné identifikátory uživatele.

### Licence
[GNU GPL v3](LICENSE) © Obevalon
