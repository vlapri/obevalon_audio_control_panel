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
- Windows 10 or Windows 11 (x64 or ARM64)
- [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0)

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
- Windows 10 nebo Windows 11 (x64 nebo ARM64)
- [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0)

### Soukromí
Jediný síťový požadavek aplikace je kontrola aktualizací: stažení manifestu z GitHub Releases
tohoto repozitáře. Žádná telemetrie, žádné identifikátory uživatele.

### Licence
[GNU GPL v3](LICENSE) © Obevalon
