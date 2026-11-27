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

**Windows blocks the app ("unknown publisher")?** Releases are being moved to code signing through SignPath
(see *Code signing policy* below). Until a release is signed, Windows 11 **Smart App Control** blocks it
completely and SmartScreen shows a warning. For a release that is not signed yet you can:
- SmartScreen warning: choose *More info → Run anyway*;
- Smart App Control: *Windows Security → App & browser control → Smart App Control settings → Off*
  (on some Windows 11 versions it cannot be turned back on without reinstalling Windows).

Signed releases show *Publisher: SignPath Foundation* in the file properties.

### Code signing policy
Free code signing provided by [SignPath.io](https://about.signpath.io/), certificate by
[SignPath Foundation](https://signpath.org/).

- Committers and reviewers: [vlapri](https://github.com/vlapri) (Obevalon)
- Approvers: [vlapri](https://github.com/vlapri) (Obevalon)

Only executables built by the GitHub Actions release workflow from this repository's source code are signed.
Every signing request is approved manually.

### Privacy policy
This program does not transfer any information to other networked systems except for the update check:
when automatic update checks are enabled (default; can be turned off in Settings) or when you click
*Check now*, it downloads the update manifest from this repository's GitHub Releases
(`github.com`), and after you confirm an update it downloads the new version from there.
These requests contain no personal data or identifiers beyond what any HTTPS request contains
(IP address, the user agent `OACP/<version>`). There is no telemetry.
Settings and the optional diagnostic log stay on your computer.

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

**Windows aplikaci blokuje („neznámý vydavatel“)?** Vydání přecházejí na digitální podpis přes SignPath
(viz *Podepisování* níže). Dokud vydání podepsané není, **Inteligentní řízení aplikací** ve Windows 11
ho úplně zablokuje a SmartScreen zobrazí varování. U dosud nepodepsaného vydání můžete:
- varování SmartScreen: *Další informace → Přesto spustit*;
- Inteligentní řízení aplikací: *Zabezpečení Windows → Řízení aplikací a prohlížeče → Nastavení
  inteligentního řízení aplikací → Vypnuto* (na některých verzích Windows 11 ho pak nejde znovu zapnout
  bez přeinstalace systému).

Podepsaná vydání mají ve vlastnostech souboru *Vydavatel: SignPath Foundation*.

### Podepisování
Bezplatné podepisování kódu poskytuje [SignPath.io](https://about.signpath.io/), certifikát vydává
[SignPath Foundation](https://signpath.org/). Podepisují se jen exe sestavená workflow GitHub Actions
ze zdrojového kódu tohoto repozitáře a každou žádost o podpis schvaluje autor ručně.
Role (správce kódu, revize, schvalování): [vlapri](https://github.com/vlapri) (Obevalon).

### Soukromí
Program nepřenáší žádné informace jinam s výjimkou kontroly aktualizací: při zapnuté automatické kontrole
(výchozí, lze vypnout v Nastavení) nebo po kliknutí na *Zkontrolovat nyní* stáhne manifest aktualizace
z GitHub Releases tohoto repozitáře (`github.com`) a po vašem potvrzení odtud stáhne novou verzi.
Požadavky neobsahují osobní údaje ani identifikátory nad rámec běžného HTTPS požadavku (IP adresa,
User-Agent `OACP/<verze>`). Žádná telemetrie. Nastavení i volitelný diagnostický log zůstávají v počítači.

### Licence
[GNU GPL v3 nebo novější](LICENSE) © Obevalon
