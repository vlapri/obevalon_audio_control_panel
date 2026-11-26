# Obevalon Audio Control Panel – specifikace, architektura a plán práce

> Stav dokumentu: **návrh v1** (před zahájením vývoje)
> Autor aplikace: **Obevalon**
> Repozitář: <https://github.com/vlapri/obevalon_audio_control_panel>
> Licence: **GNU GPL v3** (soubor `LICENSE`)

---

## 1. Cíl projektu

Malá portable utilita pro Windows 10/11 (primárně Windows 11) pro rychlé přepínání
výchozího audio výstupu (např. jack/reproduktory ↔ HDMI/projektor) a ovládání
mikrofonu (hlasitost, mute) – z okna, ze systémové lišty (systray) i globálními
klávesovými zkratkami.

## 2. Přijatá rozhodnutí

| # | Téma | Rozhodnutí |
|---|---|---|
| D1 | Jazyk a platforma | **C# / .NET 10 (LTS)**, UI ve **WPF** |
| D2 | Admin práva | **Nepoužívají se.** Aplikace běží jako `asInvoker`, žádné UAC. Všechny funkce (Core Audio, registr HKCU) fungují bez admin práv. |
| D3 | Distribuce | **Malý exe** – framework-dependent single-file (vyžaduje nainstalovaný .NET 10 Desktop Runtime). Pokud runtime chybí, Windows zobrazí dialog s odkazem ke stažení. |
| D4 | Jazyk UI | **Čeština + angličtina**, autodetekce podle jazyka systému, ruční přepnutí v nastavení. |
| D5 | Klávesové zkratky | Globální zkratky, **ve výchozím stavu vypnuté**, uživatelsky nastavitelné. |
| D6 | Systray | **Levý klik** = otevřít okno, **pravý klik** = kontextová nabídka s rychlými volbami. |
| D7 | Komunikační zařízení | Při přepnutí výstupu se mění i **výchozí komunikační zařízení** (nastavitelné, výchozí = ano). |
| D8 | Mikrofon | Ovládá se **jeden** mikrofon (výchozí systémový, případně ručně vybraný). |
| D9 | Hlasitost | Aplikace ovládá **jen hlasitost mikrofonu**, ne hlasitost výstupu. |
| D10 | Vzhled | Kompaktní okno, motiv **auto (podle systému) / světlý / tmavý**. |
| D11 | Licence | **GNU GPL v3**. Všechny závislosti musí mít licenci kompatibilní s GPL v3 (MIT, Apache 2.0, BSD…). |
| D12 | OS | Windows 10 (1809+) a Windows 11, x64 a ARM64. |
| D13 | CI/CD | **GitHub Actions** – build a testy na každý push/PR, vydání do **GitHub Releases** po pushnutí tagu `vX.Y.Z`. |
| D14 | Nastavení | `settings.ini` ve složce, odkud je spuštěn `.exe` (portable). |

## 3. Funkční specifikace

### 3.1 Audio výstupy

- **F1.1** Načíst všechna výstupní zařízení (render endpoints) včetně neaktivních
  (odpojené HDMI při vypnutém projektoru). Neaktivní zobrazit šedě se stavem
  „nedostupné“.
- **F1.2** Zařízení identifikovat podle **ID endpointu** (stabilní), ne podle názvu.
- **F1.3** Ke každému zařízení lze nastavit:
  - **vlastní název** (zobrazovaný místo systémového názvu),
  - **poznámku** (volný text, zobrazený pod názvem / v tooltipu),
  - **pořadí** v seznamu (tlačítka nahoru/dolů, později drag & drop),
  - příznak **„v cyklu“** – účastní se cyklického přepínání,
  - příznak **„skrýt“** – nezobrazuje se v seznamu ani v menu (lze zobrazit přepínačem „zobrazit skrytá“).
- **F1.4** Přepnutí výchozího výstupu:
  - kliknutím na zařízení v seznamu,
  - tlačítkem **„Další výstup“** (cyklus),
  - z kontextové nabídky v systray,
  - globální klávesovou zkratkou.
- **F1.5** **Cyklus**: projde zařízení s příznakem „v cyklu“ ve stavu *aktivní* v pořadí
  podle seznamu; přepne na zařízení následující po aktuálním. Pokud aktuální v cyklu
  není, přepne na první. Pokud je v cyklu méně než 2 dostupná zařízení, akce nic nedělá
  (a volitelně zobrazí upozornění).
- **F1.6** Přepnutí nastaví výchozí zařízení pro role *Console* a *Multimedia*,
  a pokud je zapnuto (D7), i pro roli *Communications*.
- **F1.7** Aktuální výchozí výstup je v seznamu zvýrazněný. Aplikace reaguje na změny
  provedené mimo ni (nastavení Windows, připojení/odpojení zařízení) – živá aktualizace.
- **F1.8** Zařízení, které už v systému neexistuje (např. po výměně ovladače se změnilo ID),
  zůstane v nastavení jako „chybějící“ s možností ho odstranit.
- **F1.9** Volitelné **OSD oznámení** po přepnutí (malé nenápadné okno na ~1,5 s s názvem
  nového výstupu). Výchozí: zapnuto. Nepoužívat Windows toast (zaneřáďuje Centrum oznámení).

### 3.2 Mikrofon

- **F2.1** Ovládaný mikrofon: **„systémový výchozí“** (výchozí volba, sleduje změny) nebo
  konkrétní zařízení vybrané ze seznamu vstupů.
- **F2.2** Posuvník hlasitosti 0–100 % (+ zobrazení hodnoty).
- **F2.3** Tlačítko **mute/unmute** s ikonou mikrofonu / přeškrtnutého mikrofonu.
- **F2.4** Stav mute je viditelný i na **ikoně v systray** (překryvná ikona) a v tooltipu.
- **F2.5** Obousměrná synchronizace – změny hlasitosti/mute provedené jinde
  (Windows, Teams, hardwarové tlačítko) se okamžitě projeví v aplikaci.

### 3.3 Systray a životní cyklus aplikace

- **F3.1** Ikona v systray je zobrazena vždy, když aplikace běží.
- **F3.2** **Levý klik** → zobrazit/aktivovat hlavní okno.
- **F3.3** **Pravý klik** → kontextová nabídka:
  - seznam (neskrytých, dostupných) výstupů s označením aktuálního → klik přepne,
  - „Další výstup“,
  - „Ztlumit mikrofon“ / „Zapnout mikrofon“,
  - oddělovač,
  - „Otevřít“, „Nastavení“, „O aplikaci“,
  - oddělovač,
  - „Ukončit“.
- **F3.4** **Tooltip** ikony: aktuální výstup + stav mikrofonu.
- **F3.5** Minimalizace okna → schovat do systray. Zavření okna (×) → schovat do systray
  (nastavitelné: „zavřením okna aplikaci ukončit“). Ukončení vždy přes menu „Ukončit“.
- **F3.6** Spuštění s parametrem `--tray` (používá autostart) → start rovnou do systray
  bez zobrazení okna.
- **F3.7** **Jediná instance**: druhé spuštění pouze aktivuje okno běžící instance a skončí.

### 3.4 Autostart

- **F4.1** Přepínač „Spouštět po přihlášení“ v nastavení.
- **F4.2** Implementace přes registr
  `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`, hodnota
  `ObevalonAudioControlPanel` = `"<plná cesta k exe>" --tray`. Bez admin práv, bez UAC.
- **F4.3** Zdrojem pravdy o autostartu je **registr**, ne INI.
- **F4.4** Při startu aplikace kontrolovat, zda cesta v registru odpovídá aktuální cestě
  k exe (portable složka mohla být přesunuta). Pokud ne, nabídnout opravu.
- **F4.5** Respektovat zakázání autostartu ve Správci úloh
  (`...\Explorer\StartupApproved\Run`) – zobrazit stav „zakázáno ve Windows“.

### 3.5 Klávesové zkratky

- **F5.1** Akce: *Další výstup*, *Přepnout mute mikrofonu*, *Zobrazit okno*.
  (Rozšiřitelné, např. později zkratka pro konkrétní zařízení.)
- **F5.2** Ve výchozím stavu **žádná zkratka není přiřazena**.
- **F5.3** Nastavení zkratky zachycením stisknuté kombinace (modifikátor + klávesa).
- **F5.4** Pokud registrace selže (zkratku používá jiná aplikace), zobrazit chybu u dané zkratky.

### 3.6 Nastavení (okno/sekce Nastavení)

- Jazyk: Automaticky / Čeština / English
- Motiv: Automaticky / Světlý / Tmavý
- Spouštět po přihlášení
- Zavřením okna aplikaci ukončit (jinak schovat do systray)
- Při přepnutí měnit i komunikační zařízení
- Zobrazovat OSD oznámení při přepnutí
- Ovládaný mikrofon
- Klávesové zkratky

### 3.7 O aplikaci

- Název, verze (z assembly), autor **Obevalon**, licence GPL v3,
  odkaz na GitHub repozitář (otevře výchozí prohlížeč).

### 3.8 Nastavení v INI

- **F8.1** Soubor `settings.ini` vedle `.exe`.
- **F8.2** Pokud složka není zapisovatelná (např. `C:\Program Files\...`), použít
  `%APPDATA%\Obevalon\AudioControlPanel\settings.ini` a informovat o tom v Nastavení.
- **F8.3** Zápis atomicky (zapsat do `settings.ini.tmp`, pak přejmenovat), kódování UTF-8.
- **F8.4** Neznámé nebo poškozené hodnoty → použít výchozí, aplikace nesmí spadnout.

Návrh formátu:

```ini
; Obevalon Audio Control Panel settings
[General]
Language=auto                    ; auto | cs | en
Theme=auto                       ; auto | light | dark
CloseToTray=true
SwitchCommunicationsDevice=true
ShowSwitchOsd=true
ShowHiddenDevices=false

[Microphone]
DeviceId=                        ; prázdné = systémový výchozí mikrofon

[Hotkeys]
NextOutput=                      ; např. Ctrl+Alt+A
ToggleMicMute=                   ; např. Ctrl+Alt+M
ShowWindow=

[Window]
Left=
Top=

; Jedna sekce na každé známé výstupní zařízení
[Output:{0.0.0.00000000}.{a1b2c3d4-...}]
SystemName=Speakers (Realtek(R) Audio)   ; poslední známý systémový název (pro chybějící zařízení)
Name=Reproduktory
Note=Jack vzadu
Order=1
InCycle=true
Hidden=false
```

## 4. Nefunkční požadavky

- **N1** Žádné admin práva, žádné UAC (`requestedExecutionLevel = asInvoker`).
- **N2** Rychlý start do systray (cíl < 1 s), nízká spotřeba paměti v klidu.
- **N3** Žádný polling – změny zařízení a hlasitosti přes notifikační callbacky Core Audio.
- **N4** Per-Monitor V2 DPI awareness, ostré ikony při 100–200 % škálování.
- **N5** Aplikace nesmí spadnout kvůli audio chybě (zařízení zmizí během operace apod.);
  chyby se zalogují a zobrazí srozumitelně.
- **N6** Volitelný log do `logs\` vedle exe (vypnutý ve výchozím stavu, zapnutelný parametrem
  `--log` nebo v INI).
- **N7** Velikost výsledného exe řádově jednotky MB.

## 5. Architektura

### 5.1 Technologie

| Oblast | Řešení |
|---|---|
| Runtime | .NET 10 (LTS), `net10.0` / `net10.0-windows` |
| UI | WPF, MVVM |
| MVVM | `CommunityToolkit.Mvvm` (MIT) |
| Systray | `H.NotifyIcon.Wpf` (MIT) – WPF kontextové menu, které jde stylovat podle motivu |
| Core Audio | **vlastní tenká COM interop vrstva** přes source-generated COM (`[GeneratedComInterface]`): `IMMDeviceEnumerator`, `IMMDevice`, `IPropertyStore`, `IMMNotificationClient`, `IAudioEndpointVolume`, `IAudioEndpointVolumeCallback` a neveřejné `IPolicyConfig` pro nastavení výchozího zařízení |
| Klávesové zkratky | Win32 `RegisterHotKey` / `UnregisterHotKey` přes skryté okno (`HwndSource`) |
| Jediná instance | pojmenovaný `Mutex` + pojmenovaný `EventWaitHandle` pro signál „zobraz okno“ |
| Lokalizace | `.resx` (`Strings.resx` = EN, `Strings.cs.resx` = CZ), přepínání za běhu |
| Motiv | vlastní `ResourceDictionary` Light/Dark; detekce přes registr `AppsUseLightTheme` + `SystemEvents.UserPreferenceChanged`; tmavý titulek okna přes `DwmSetWindowAttribute` |
| INI | vlastní jednoduchý parser/zapisovač (bez závislostí) |
| Testy | xUnit |

Proč vlastní interop místo knihovny NAudio: `IPolicyConfig` stejně v NAudio není,
potřebujeme jen zlomek API a source-generated COM je kompatibilní s trimmingem.
Celé neveřejné API je izolované v jedné třídě – kdyby ho Microsoft změnil, oprava je na jednom místě.

### 5.2 Vrstvy

```
┌──────────────────────────────────────────────────────────────┐
│  Obevalon.AudioControlPanel  (WPF app, net10.0-windows)      │
│  Views, ViewModels, Tray, OSD, Themes, Localization, App.xaml│
└───────────────┬───────────────────────────────┬──────────────┘
                │ používá rozhraní              │ DI složení (App startup)
┌───────────────▼─────────────────┐  ┌──────────▼───────────────────────────┐
│ Obevalon.AudioControlPanel.Core │  │ Obevalon.AudioControlPanel.Platform  │
│ (net10.0 – bez závislosti na    │◄─┤ (net10.0-windows)                    │
│  Windows, testovatelné všude)   │  │ implementace rozhraní z Core:        │
│ - modely (OutputDevice, ...)    │  │ - CoreAudio interop + služby         │
│ - rozhraní služeb               │  │ - Autostart (registr HKCU)           │
│ - AppSettings + INI parser      │  │ - Hotkeys (RegisterHotKey)           │
│ - logika pořadí a cyklu         │  │ - detekce systémového motivu/jazyka  │
│ - DeviceCatalog (sloučení       │  │ - SingleInstance                     │
│   systémových dat a nastavení)  │  │                                      │
└─────────────────────────────────┘  └──────────────────────────────────────┘
```

Klíčová rozhraní v `Core`:

- `IAudioDeviceService` – výpis výstupů a vstupů, aktuální výchozí, `SetDefaultOutput(id, includeCommunications)`,
  události `DevicesChanged`, `DefaultOutputChanged`.
- `IMicrophoneService` – `Volume`, `IsMuted`, `SetVolume`, `ToggleMute`, událost `StateChanged`,
  výběr zařízení (výchozí / konkrétní ID).
- `IAutostartService` – `IsEnabled`, `IsDisabledBySystem`, `IsPathValid`, `Enable()`, `Disable()`, `RepairPath()`.
- `IHotkeyService` – `Register(action, gesture)`, `Unregister(action)`, událost `HotkeyPressed`.
- `ISettingsStore` – `Load()`, `Save(AppSettings)`, `SettingsPath`, `IsPortable`.
- `ISystemThemeProvider`, `ISystemLanguageProvider`.

Logika, kterou lze testovat bez Windows (cyklus, řazení, slučování seznamu zařízení
s nastavením, INI), je v `Core`. Na Linuxu i v CI tak běží jednotkové testy.

### 5.3 Vláknový model

- Callbacky Core Audio chodí na jiných vláknech → služby je předávají jako události
  a ViewModely je přeposílají na UI vlákno přes `Dispatcher`.
- V callbacku `IMMNotificationClient` se **nesmí** volat zpět do Core Audio
  (doporučení Microsoftu) → pouze naplánovat obnovení seznamu.

### 5.4 Spouštěcí sekvence

1. Zkontrolovat jedinou instanci (pokud už běží → signál „zobraz okno“ a konec).
2. Zpracovat parametry (`--tray`, `--log`).
3. Načíst `settings.ini` (nebo výchozí hodnoty).
4. Aplikovat jazyk a motiv.
5. Inicializovat audio služby, mikrofon a zkratky.
6. Vytvořit ikonu v systray.
7. Bez `--tray` zobrazit hlavní okno.
8. Zkontrolovat cestu autostartu (F4.4).

### 5.5 Známá omezení a rizika

- `IPolicyConfig` je neveřejné rozhraní (používá ho např. SoundSwitch nebo EarTrumpet, je stabilní od Windows 7).
  Riziko je mitigováno izolací v jedné třídě.
- Aplikace, kterým uživatel ve Windows 11 nastavil vlastní výstup (*Nastavení → Zvuk →
  Směšovač hlasitosti*), přepnutí výchozího zařízení nenásledují. To je chování Windows.
- ID zařízení se může změnit po přeinstalaci ovladače nebo výměně GPU → řeší F1.8.
- Nepodepsaný exe → varování SmartScreen při prvním spuštění. Později lze řešit
  bezplatným podepisováním pro open source (SignPath Foundation).

## 6. Struktura repozitáře

```
obevalon_audio_control_panel/
├─ .github/
│  └─ workflows/
│     ├─ build.yml                 # build + testy na push/PR
│     └─ release.yml               # publish + GitHub Release na tag v*
├─ docs/
│  ├─ SPEC.md                      # tento dokument
│  └─ TESTING.md                   # manuální testovací checklist (audio na reálném HW)
├─ src/
│  ├─ Obevalon.AudioControlPanel/              # WPF aplikace (exe)
│  │  ├─ App.xaml(.cs)
│  │  ├─ app.manifest                          # asInvoker, PerMonitorV2, supportedOS Win10/11
│  │  ├─ Assets/                               # app.ico, ikony tray (normal / mic muted), SVG zdroje
│  │  ├─ Localization/                         # Strings.resx, Strings.cs.resx, LocExtension
│  │  ├─ Themes/                               # Light.xaml, Dark.xaml, Controls.xaml, ThemeManager
│  │  ├─ Tray/                                 # TrayIconController, kontextové menu
│  │  ├─ ViewModels/                           # MainViewModel, OutputDeviceViewModel, MicrophoneViewModel, SettingsViewModel, AboutViewModel
│  │  └─ Views/                                # MainWindow, SettingsWindow, AboutWindow, OsdWindow, HotkeyBox
│  ├─ Obevalon.AudioControlPanel.Core/         # net10.0, bez závislosti na Windows
│  │  ├─ Abstractions/                         # rozhraní služeb
│  │  ├─ Models/                               # OutputDevice, InputDevice, DeviceState, HotkeyGesture, ...
│  │  ├─ Settings/                             # AppSettings, IniDocument, IniSettingsStore, SettingsPathResolver
│  │  └─ Logic/                                # DeviceCatalog, CycleSelector, OrderingService
│  └─ Obevalon.AudioControlPanel.Platform/     # net10.0-windows
│     ├─ CoreAudio/
│     │  ├─ Interop/                           # COM rozhraní, PROPERTYKEY, enumy (vč. IPolicyConfig)
│     │  ├─ AudioDeviceService.cs
│     │  └─ MicrophoneService.cs
│     ├─ Autostart/RegistryAutostartService.cs
│     ├─ Hotkeys/Win32HotkeyService.cs
│     ├─ System/                               # SystemThemeProvider, SystemLanguageProvider, SingleInstance
│     └─ Native/                               # P/Invoke (user32, dwmapi)
├─ tests/
│  └─ Obevalon.AudioControlPanel.Core.Tests/   # xUnit, net10.0
├─ .editorconfig
├─ .gitignore
├─ Directory.Build.props                       # společné vlastnosti (verze, nullable, LangVersion, analyzéry)
├─ Directory.Packages.props                    # centrální správa verzí NuGet balíčků
├─ global.json                                 # pinnutí .NET SDK 10
├─ ObevalonAudioControlPanel.slnx
├─ LICENSE                                     # GPL v3
└─ README.md                                   # EN + CZ, screenshoty, stažení, použití
```

Výstupní soubor: **`ObevalonAudioControlPanel.exe`**.

## 7. Build a vydání (GitHub Actions)

### 7.1 `build.yml`
- Spouští se na `push` a `pull_request`.
- `runs-on: windows-latest`, `actions/setup-dotnet` (verze z `global.json`).
- `dotnet restore` → `dotnet build -c Release` → `dotnet test`.

### 7.2 `release.yml`
- Spouští se na push tagu `v*.*.*` (např. `v1.0.0`).
- Pro `win-x64` a `win-arm64`:
  ```
  dotnet publish src/Obevalon.AudioControlPanel -c Release -r <rid>
    --self-contained false -p:PublishSingleFile=true
    -p:Version=<verze z tagu> -o out/<rid>
  ```
- Zabalit do `ObevalonAudioControlPanel-<verze>-<rid>.zip` (exe + LICENSE + README),
  přiložit i samotný `.exe` pro x64 a `SHA256SUMS.txt`.
- Vytvořit GitHub Release s automaticky generovanými poznámkami ke změnám.

### 7.3 Verzování
- **SemVer**. Verze se bere **z git tagu** (žádné ruční přepisování v souborech).
- Lokální/vývojový build má verzi `0.0.0-dev`.
- Vydání: merge do `main` → `git tag vX.Y.Z` → `git push origin vX.Y.Z`.

## 8. Pokyny pro práci

### 8.1 Obecně
- Komunikace s autorem: **česky**.
- Kód, identifikátory, komentáře v kódu, commit zprávy: **anglicky** (veřejný open-source projekt).
- `docs/SPEC.md` je česky, `README.md` dvojjazyčně (EN, pak CZ).
- Tento dokument je **zdroj pravdy**. Změna požadavků = nejdřív aktualizovat SPEC, pak kód.

### 8.2 Git
- `main` je vždy sestavitelný. Práce probíhá ve feature větvích a jde do `main` přes pull request.
- Jedna milník/funkce = jeden PR (malé, přehledné PR).
- Commit zprávy ve stylu *Conventional Commits*: `feat:`, `fix:`, `docs:`, `ci:`, `refactor:`, `test:`, `chore:`.

### 8.3 Kód
- Zapnuté `Nullable`, `TreatWarningsAsErrors` v Release, .NET analyzéry.
- MVVM: žádná logika v code-behind kromě čistě UI věcí.
- Veškeré P/Invoke a COM jen v projektu `Platform`. `Core` nesmí referencovat nic Windows-specifického.
- Žádná nová NuGet závislost bez odsouhlasení; musí mít licenci kompatibilní s GPL v3.
- Každý text v UI musí jít přes lokalizační zdroje (žádné natvrdo psané řetězce), vždy v CZ i EN.
- Hlavička souborů s licencí: `// SPDX-License-Identifier: GPL-3.0-or-later`
  *(nebo `GPL-3.0-only` – viz otevřená otázka Q1)*.

### 8.4 Testování
- Jednotkové testy pro veškerou logiku v `Core` (INI, cyklus, řazení, slučování zařízení).
- Audio, systray, zkratky a autostart nelze testovat v CI → **manuální checklist**
  v `docs/TESTING.md`, projít před každým vydáním na reálném Windows 11 (a namátkově Windows 10).

## 9. Plán práce (milníky)

| Milník | Obsah | Výstup |
|---|---|---|
| **M0 – Kostra a CI** | Solution, 3 projekty + testy, `Directory.Build.props`, `global.json`, `.editorconfig`, `app.manifest`, prázdné okno, `build.yml` + `release.yml`, README | Tag `v0.1.0` vytvoří Release s (prázdným) exe – ověření celé pipeline |
| **M1 – Core Audio** | COM interop, `AudioDeviceService` (výpis, výchozí, přepnutí včetně komunikační role, notifikace), `MicrophoneService` (hlasitost, mute, callback) | Funkční služby ověřené na reálném HW |
| **M2 – Nastavení a logika** | `IniDocument`, `IniSettingsStore`, `SettingsPathResolver` (portable/APPDATA), `DeviceCatalog`, `CycleSelector`, řazení + jednotkové testy | Zelené testy |
| **M3 – Hlavní okno** | Seznam výstupů (název, poznámka, stav, aktuální), editace názvu/poznámky, pořadí, „v cyklu“, „skrýt“, tlačítko „Další výstup“, sekce mikrofonu (posuvník, mute) | Použitelná aplikace z okna |
| **M4 – Systray a životní cyklus** | Ikona, tooltip, levý/pravý klik, kontextové menu, ikona mute, minimalizace/zavření do tray, `--tray`, jediná instance | Aplikace žije v systray |
| **M5 – Autostart** | Registr HKCU Run, StartupApproved, kontrola a oprava cesty | Autostart bez UAC |
| **M6 – Lokalizace a motivy** | resx CZ/EN, autodetekce + přepnutí za běhu, Light/Dark + auto, tmavý titulek, okno Nastavení | Dvojjazyčná aplikace s motivy |
| **M7 – Klávesové zkratky** | `Win32HotkeyService`, ovládací prvek pro zachycení zkratky, hlášení konfliktů | Globální zkratky |
| **M8 – Dokončení** | O aplikaci, finální ikony, OSD oznámení, logování, ošetření chyb, `docs/TESTING.md` | Release candidate |
| **M9 – Vydání 1.0** | Projít manuální checklist (Win11 + Win10), README se screenshoty, `v1.0.0` | Veřejné vydání |

Možná rozšíření po verzi 1.0 (zatím **mimo rozsah**): zkratka pro konkrétní zařízení,
profily (výstup + mikrofon najednou), drag & drop řazení, kontrola nové verze přes GitHub API,
podepisování exe, přepínání výchozího mikrofonu.

## 10. Otevřené otázky

- **Q1** Licence v hlavičkách souborů: `GPL-3.0-or-later` (umožní přechod na budoucí verze GPL),
  nebo striktně `GPL-3.0-only`? *Návrh: `GPL-3.0-or-later`.*
- **Q2** Ikona aplikace: vytvořit vlastní jednoduchou (reproduktor + šipky přepnutí),
  nebo dodá autor?
- **Q3** Minimální verze Windows 10: stačí 1809, nebo podporovat jen aktuálně podporované verze (22H2)?
