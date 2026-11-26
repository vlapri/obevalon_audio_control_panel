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
| D12 | OS | Windows 11 (primárně) a Windows 10 od verze 1809 (build 17763), x64 a ARM64. Testuje se na Windows 11 a Windows 10 22H2. |
| D13 | CI/CD | **GitHub Actions** – build a testy na každý push/PR, vydání do **GitHub Releases** po pushnutí tagu `vX.Y.Z`. |
| D14 | Nastavení | `settings.ini` ve složce, odkud je spuštěn `.exe` (portable). |
| D15 | Aktualizace | Automatická kontrola nové verze, **upozornění uživatele** a po jeho souhlasu automatické stažení, ověření a instalace. Zdrojem jsou **GitHub Releases** (manifest `update.json` jako asset vydání), bez vlastního serveru. Detail v kap. 3.9 a 5.5. |
| D16 | Výstupní soubor | `OACP.exe` |
| D17 | Licence v hlavičkách | `// SPDX-License-Identifier: GPL-3.0-or-later` |
| D18 | Ikona | Vlastní návrh: reproduktor se zvukovými vlnami a šipkami přepnutí, varianta pro ztlumený mikrofon do systray. Zdroj je SVG v `src/Obevalon.AudioControlPanel/Assets/`. |

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
- Automaticky kontrolovat aktualizace + tlačítko „Zkontrolovat nyní“

### 3.7 O aplikaci

- Název, verze (z assembly), autor **Obevalon**, licence GPL v3,
  odkaz na GitHub repozitář (otevře výchozí prohlížeč).
- Tlačítko „Zkontrolovat aktualizace“ a stav poslední kontroly.

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

[Updates]
AutoCheck=true                   ; automatická kontrola nové verze
LastCheckUtc=                    ; čas poslední kontroly (ISO 8601)
SkippedVersion=                  ; verze, kterou uživatel zvolil „přeskočit“

; Jedna sekce na každé známé výstupní zařízení
[Output:{0.0.0.00000000}.{a1b2c3d4-...}]
SystemName=Speakers (Realtek(R) Audio)   ; poslední známý systémový název (pro chybějící zařízení)
Name=Reproduktory
Note=Jack vzadu
Order=1
InCycle=true
Hidden=false
```

INI obsahuje klíč `[General] SettingsVersion=<n>`. Když nová verze aplikace změní formát,
při načtení se nastavení převede (migrace) a uloží s novým číslem verze.

### 3.9 Automatické aktualizace

- **F9.1 Kontrola:** automaticky ~30 s po startu a pak každých 24 h, dokud aplikace běží.
  Lze vypnout v Nastavení (`AutoCheck`, výchozí = zapnuto). Ruční „Zkontrolovat nyní“
  v Nastavení a v okně O aplikaci kontroluje vždy, bez ohledu na `AutoCheck` a `SkippedVersion`.
- **F9.2 Zdroj:** manifest `update.json` z posledního vydání na GitHubu
  (`https://github.com/vlapri/obevalon_audio_control_panel/releases/latest/download/update.json`).
  Pre-release a draft vydání se ignorují (GitHub je pod „latest“ nevrací).
- **F9.3 Upozornění:** pokud je k dispozici vyšší verze, která není přeskočená:
  - na ikoně v systray se objeví odznak a v kontextové nabídce položka
    „Je k dispozici verze X.Y.Z…“,
  - zobrazí se nenápadné vlastní oznamovací okno (vpravo dole, ve stylu OSD, ne Windows toast)
    s textem „Je k dispozici nová verze X.Y.Z“ a tlačítky **Aktualizovat**, **Později**,
    **Přeskočit tuto verzi** a odkazem **Co je nového** (otevře stránku vydání).
  - **Později** = připomenout při příští automatické kontrole. **Přeskočit** = uložit
    `SkippedVersion`, o této verzi už neupozorňovat (vyšší verze upozorní znovu).
- **F9.4 Nic se nestahuje ani neinstaluje bez souhlasu uživatele.**
- **F9.5 Stažení:** po kliknutí na *Aktualizovat* se stáhne exe pro architekturu běžící
  aplikace (x64/ARM64) do `%TEMP%\Obevalon\AudioControlPanel\update\` a zobrazí se průběh
  s možností zrušení.
- **F9.6 Ověření:** stažený soubor musí odpovídat SHA-256 z manifestu a manifest musí mít
  platný **digitální podpis** (viz 5.5). Pokud ověření selže, aktualizace se neprovede,
  soubor se smaže a zobrazí se chyba.
- **F9.7 Instalace bez admin práv a bez samostatného updateru:**
  1. Běžící exe se přejmenuje na `OACP.exe.old`. Windows dovoluje
     přejmenovat běžící exe, přepsat ho ne.
  2. Nový exe se přesune na původní místo.
  3. Spustí se nový exe s parametry `--updated --wait-pid <PID>` a stará instance se ukončí.
  4. Nová instance počká na ukončení staré, pak teprve převezme jedinou instanci (mutex).
     Potom smaže `.old`, zobrazí krátké „Aktualizováno na verzi X.Y.Z“ a obnoví stav
     okna nebo systray podle toho, jak běžela stará instance.
  - Pokud selže krok 2 nebo 3, `.old` se přejmenuje zpět (rollback) a uživatel dostane chybové hlášení.
  - Zbylý `.old` z dřívějška se při startu smaže.
- **F9.8 Nezapisovatelná složka** (např. `Program Files`): automatická instalace není možná
  bez admin práv, a ty aplikace nepoužívá (D2). Uživatel dostane upozornění s tlačítkem
  „Otevřít stránku ke stažení“.
- **F9.9 Runtime:** manifest uvádí minimální potřebnou verzi .NET Desktop Runtime. Pokud ji
  systém nemá (např. budoucí přechod na .NET 12), aplikace se neaktualizuje. Uživateli
  nabídne odkaz na instalaci runtime a aktualizaci zkusí znovu později.
- **F9.10 Nastavení se při aktualizaci nemění.** `settings.ini` zůstává, případnou migraci
  formátu provede nová verze (viz 3.8).
- **F9.11 Soukromí:** kontrola je obyčejný HTTPS požadavek na GitHub, bez telemetrie
  a bez identifikátorů uživatele. User-Agent je `OACP/<verze>`.
  Uvést v README.
- **F9.12 Chyby sítě** při automatické kontrole se nezobrazují, jen se zalogují. Další pokus
  proběhne při příští plánované kontrole. Při ruční kontrole se chyba zobrazí.
- **F9.13** Automatická aktualizace funguje jen tehdy, když je repozitář **veřejný**.

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
- `IUpdateService` – `CheckAsync(manual)`, `DownloadAsync(progress, ct)`, `InstallAndRestart()`,
  události `UpdateAvailable`, stav (`Idle / Checking / Available / Downloading / ReadyToInstall / Failed`).
- `IUpdateInstaller` – výměna exe a restart (platformně závislá část aktualizace).

Logika, kterou lze testovat bez Windows (cyklus, řazení, slučování seznamu zařízení
s nastavením, INI), je v `Core`. Na Linuxu i v CI tak běží jednotkové testy.

### 5.3 Vláknový model

- Callbacky Core Audio chodí na jiných vláknech → služby je předávají jako události
  a ViewModely je přeposílají na UI vlákno přes `Dispatcher`.
- V callbacku `IMMNotificationClient` se **nesmí** volat zpět do Core Audio
  (doporučení Microsoftu) → pouze naplánovat obnovení seznamu.

### 5.4 Spouštěcí sekvence

1. Zpracovat parametry (`--tray`, `--log`, `--updated`, `--wait-pid <PID>`).
   S `--wait-pid` počkat na ukončení staré instance (timeout ~10 s).
2. Zkontrolovat jedinou instanci (pokud už běží → signál „zobraz okno“ a konec).
   Smazat případný `OACP.exe.old`.
3. Načíst `settings.ini` (nebo výchozí hodnoty).
4. Aplikovat jazyk a motiv.
5. Inicializovat audio služby, mikrofon a zkratky.
6. Vytvořit ikonu v systray.
7. Bez `--tray` zobrazit hlavní okno.
8. Zkontrolovat cestu autostartu (F4.4).
9. Naplánovat kontrolu aktualizací (F9.1). S `--updated` zobrazit „Aktualizováno na verzi X.Y.Z“.

### 5.5 Systém aktualizací

**Zvolený zdroj: GitHub Releases** s manifestem `update.json` jako assetem vydání.
URL `releases/latest/download/update.json` vždy přesměruje na poslední stabilní vydání.

| Varianta | Výhody | Nevýhody |
|---|---|---|
| **Releases + `update.json` (zvoleno)** | žádná další infrastruktura; manifest vzniká ve stejném workflow jako binárky; nepodléhá limitu GitHub API (60 požadavků/h na IP); manifest lze podepsat | — |
| GitHub REST API `/releases/latest` | nic dalšího se negeneruje | limit 60 požadavků/h na IP (sdílená IP ve firmě), nelze podepsat, parsování velké odpovědi |
| GitHub Pages | vlastní URL, možnost kanálů (beta) | další větev nebo nasazení a další bod selhání; synchronizace s Releases |

Přechod na GitHub Pages je možný později beze změny formátu manifestu, změní se jen URL.

**Formát `update.json`** (generuje `release.yml`):

```json
{
  "schema": 1,
  "version": "1.2.0",
  "releaseDate": "2027-01-15T12:00:00Z",
  "releaseNotesUrl": "https://github.com/vlapri/obevalon_audio_control_panel/releases/tag/v1.2.0",
  "minimumRuntime": "10.0.0",
  "assets": {
    "win-x64":   { "url": "https://github.com/.../releases/download/v1.2.0/OACP-1.2.0-win-x64.exe",   "sha256": "…", "size": 1234567 },
    "win-arm64": { "url": "https://github.com/.../releases/download/v1.2.0/OACP-1.2.0-win-arm64.exe", "sha256": "…", "size": 1234567 }
  }
}
```

**Podpis manifestu:**
- Release workflow podepíše přesné bajty `update.json` klíčem **ECDSA P-256 (SHA-256)**
  a výsledek nahraje jako `update.json.sig` (Base64).
- Privátní klíč je **pouze** v GitHub secret `UPDATE_SIGNING_KEY` (PEM). Nikdy se necommituje.
- Veřejný klíč je zabudovaný v aplikaci (`UpdateSigningKey.cs`).
- Aplikace ověří podpis pomocí vestavěné třídy `System.Security.Cryptography.ECDsa`, bez závislostí.
  SHA-256 binárky je součástí podepsaného manifestu, takže podpis chrání i stažený exe.
- Smysl: ani podvržený soubor v Releases, ani útok na přenos nevede ke spuštění cizího
  kódu, pokud útočník nemá privátní klíč.
- Rotace klíče: aplikace může obsahovat seznam důvěryhodných veřejných klíčů. Nová verze přidá
  nový klíč dřív, než se jím začne podepisovat.

**Rozdělení kódu:**
- `Core/Updates`: `UpdateManifest` (model + JSON přes `System.Text.Json` source generator),
  `UpdateChecker` (stažení manifestu + podpisu přes `HttpClient`, ověření podpisu, porovnání
  SemVer, vyhodnocení `SkippedVersion` a `minimumRuntime`), `UpdateDownloader` (stažení s průběhem,
  ověření SHA-256), `UpdateScheduler` (intervaly kontrol). Vše je testovatelné na Linuxu
  (podpis, porovnání verzí, parsování, plánování).
- `Platform/Updates`: `ExeSwapUpdateInstaller` (přejmenování / přesun / rollback / restart),
  `DotNetRuntimeProbe` (zjištění nainstalovaných verzí Microsoft.WindowsDesktop.App z registru
  `HKLM\SOFTWARE\dotnet\Setup\InstalledVersions\<arch>\sharedfx\Microsoft.WindowsDesktop.App`).
- `App`: `UpdateViewModel`, `UpdateNotificationWindow`, položka v tray menu, odznak na ikoně.

**Sekvence:**

```
Kontrola ──► stáhni update.json + .sig ──► ověř podpis ──► verze > aktuální a ≠ Skipped?
                                                             │ ne → konec
                                                             ▼ ano
                     upozornění (okno + odznak v tray) ──► Později / Přeskočit / Aktualizovat
                                                                                  ▼
            stáhni exe pro RID ──► ověř SHA-256 ──► rename běžící → .old ──► move nový exe
                                                                                  ▼
                          spusť nový exe --updated --wait-pid ──► ukonči starou instanci
                                                                                  ▼
                                   nová instance: počkej na PID, smaž .old, „Aktualizováno“
```

### 5.6 Známá omezení a rizika

- `IPolicyConfig` je neveřejné rozhraní (používá ho např. SoundSwitch nebo EarTrumpet, je stabilní od Windows 7).
  Riziko je mitigováno izolací v jedné třídě.
- Aplikace, kterým uživatel ve Windows 11 nastavil vlastní výstup (*Nastavení → Zvuk →
  Směšovač hlasitosti*), přepnutí výchozího zařízení nenásledují. To je chování Windows.
- ID zařízení se může změnit po přeinstalaci ovladače nebo výměně GPU → řeší F1.8.
- Nepodepsaný exe → varování SmartScreen při prvním spuštění. Později lze řešit
  bezplatným podepisováním pro open source (SignPath Foundation). Exe stažený samotnou
  aplikací (aktualizace) nedostane značku „staženo z internetu“ (Mark of the Web), takže
  po aktualizaci se varování neopakuje.
- Ztráta privátního podpisového klíče = konec automatických aktualizací pro stávající
  instalace (uživatelé by museli stáhnout novou verzi ručně). Klíč je potřeba zálohovat
  mimo GitHub (např. ve správci hesel).
- Automatická aktualizace funguje jen pro veřejný repozitář.

## 6. Struktura repozitáře

```
obevalon_audio_control_panel/
├─ .github/
│  └─ workflows/
│     ├─ build.yml                 # build + testy na push/PR
│     └─ release.yml               # publish, update.json + podpis, GitHub Release na tag v*
├─ build/
│  ├─ render-icons.mjs             # vyrenderování SVG ikon do .ico (Node + Playwright)
│  └─ make-update-manifest.ps1     # generování a podepsání update.json v release workflow
├─ docs/
│  ├─ SPEC.md                      # tento dokument
│  ├─ RELEASING.md                 # postup vydání, správa podpisového klíče
│  └─ TESTING.md                   # manuální testovací checklist (audio na reálném HW, aktualizace)
├─ src/
│  ├─ Obevalon.AudioControlPanel/              # WPF aplikace (exe)
│  │  ├─ App.xaml(.cs)
│  │  ├─ app.manifest                          # asInvoker, PerMonitorV2, supportedOS Win10/11
│  │  ├─ Assets/                               # app.svg, tray.svg, tray-muted.svg (zdroje) + vygenerované .ico
│  │  ├─ Localization/                         # Strings.resx, Strings.cs.resx, LocExtension
│  │  ├─ Themes/                               # Light.xaml, Dark.xaml, Controls.xaml, ThemeManager
│  │  ├─ Tray/                                 # TrayIconController, kontextové menu
│  │  ├─ ViewModels/                           # MainViewModel, OutputDeviceViewModel, MicrophoneViewModel, SettingsViewModel, AboutViewModel, UpdateViewModel
│  │  └─ Views/                                # MainWindow, SettingsWindow, AboutWindow, OsdWindow, UpdateNotificationWindow, HotkeyBox
│  ├─ Obevalon.AudioControlPanel.Core/         # net10.0, bez závislosti na Windows
│  │  ├─ Abstractions/                         # rozhraní služeb
│  │  ├─ Models/                               # OutputDevice, InputDevice, DeviceState, HotkeyGesture, ...
│  │  ├─ Settings/                             # AppSettings, IniDocument, IniSettingsStore, SettingsPathResolver, SettingsMigrator
│  │  ├─ Logic/                                # DeviceCatalog, CycleSelector, OrderingService
│  │  └─ Updates/                              # UpdateManifest, UpdateChecker, UpdateDownloader, UpdateScheduler, SignatureVerifier, UpdateSigningKey
│  └─ Obevalon.AudioControlPanel.Platform/     # net10.0-windows
│     ├─ CoreAudio/
│     │  ├─ Interop/                           # COM rozhraní, PROPERTYKEY, enumy (vč. IPolicyConfig)
│     │  ├─ AudioDeviceService.cs
│     │  └─ MicrophoneService.cs
│     ├─ Autostart/RegistryAutostartService.cs
│     ├─ Hotkeys/Win32HotkeyService.cs
│     ├─ Updates/                              # ExeSwapUpdateInstaller, DotNetRuntimeProbe
│     ├─ System/                               # SystemThemeProvider, SystemLanguageProvider, SingleInstance
│     └─ Native/                               # P/Invoke (user32, dwmapi)
├─ tests/
│  └─ Obevalon.AudioControlPanel.Core.Tests/   # xUnit, net10.0
├─ .editorconfig
├─ .gitattributes
├─ .gitignore
├─ Directory.Build.props                       # společné vlastnosti (verze, nullable, LangVersion, analyzéry)
├─ Directory.Packages.props                    # centrální správa verzí NuGet balíčků
├─ global.json                                 # pinnutí .NET SDK 10
├─ CLAUDE.md                                   # pokyny pro AI asistenta (Claude Code)
├─ ObevalonAudioControlPanel.slnx
├─ LICENSE                                     # GPL v3
└─ README.md                                   # EN + CZ, screenshoty, stažení, použití
```

Výstupní soubor: **`OACP.exe`** (zkratka Obevalon Audio Control Panel). Název aplikace v UI, v registru a v oknech zůstává „Obevalon Audio Control Panel“.

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
- Assety vydání:
  - `OACP-<verze>-<rid>.zip` (exe + LICENSE + README) – pro ruční stažení,
  - `OACP-<verze>-<rid>.exe` – samotný exe pro x64 i ARM64 (stahuje ho aktualizace),
  - `SHA256SUMS.txt`,
  - `update.json` + `update.json.sig` – manifest aktualizace a jeho podpis (kap. 5.5).
- Podpis vytvoří skript `build/make-update-manifest.ps1` s klíčem ze secretu
  `UPDATE_SIGNING_KEY`. **Pokud secret chybí, workflow selže** – nesmí vzniknout vydání
  bez podepsaného manifestu.
- Tagy s pre-release příponou (`v1.1.0-beta.1`) se vydají jako *pre-release*. Automatická
  aktualizace je nevidí (`releases/latest` je ignoruje).
- Vytvořit GitHub Release s automaticky generovanými poznámkami ke změnám.

### 7.2.1 Jednorázová příprava podpisového klíče (dělá autor)
1. Vygenerovat pár klíčů ECDSA P-256 (příkazy budou v `docs/RELEASING.md`).
2. Privátní klíč (PEM) uložit do GitHub → *Settings → Secrets and variables → Actions*
   jako `UPDATE_SIGNING_KEY` a zálohovat mimo GitHub.
3. Veřejný klíč vložit do `UpdateSigningKey.cs` a commitnout.

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
- Hlavička souborů s licencí: `// SPDX-License-Identifier: GPL-3.0-or-later` (D17).

### 8.4 Testování
- Jednotkové testy pro veškerou logiku v `Core` (INI, cyklus, řazení, slučování zařízení,
  porovnání verzí, parsování manifestu, ověření podpisu – platný i podvržený, SHA-256, plánování kontrol).
- Pro testování aktualizací lze v INI přepsat URL manifestu (`[Updates] ManifestUrl=`,
  nezdokumentováno v UI). Podpis se ověřuje vždy, testovací klíč se do vydání nedostane.
- Audio, systray, zkratky, autostart a výměnu exe nelze testovat v CI → **manuální checklist**
  v `docs/TESTING.md`, projít před každým vydáním na reálném Windows 11 (a namátkově Windows 10).

## 9. Plán práce (milníky)

Stav: ✅ = hotovo.

| Milník | Obsah | Výstup |
|---|---|---|
| **M0 – Kostra a CI** ✅ | Solution, 3 projekty + testy, `Directory.Build.props`, `global.json`, `.editorconfig`, `app.manifest`, prázdné okno, `build.yml` + `release.yml`, README | Tag `v0.1.0` vytvoří Release s (prázdným) exe – ověření celé pipeline |
| **M1 – Core Audio** | COM interop, `AudioDeviceService` (výpis, výchozí, přepnutí včetně komunikační role, notifikace), `MicrophoneService` (hlasitost, mute, callback) | Funkční služby ověřené na reálném HW |
| **M2 – Nastavení a logika** | `IniDocument`, `IniSettingsStore`, `SettingsPathResolver` (portable/APPDATA), `DeviceCatalog`, `CycleSelector`, řazení + jednotkové testy | Zelené testy |
| **M3 – Hlavní okno** | Seznam výstupů (název, poznámka, stav, aktuální), editace názvu/poznámky, pořadí, „v cyklu“, „skrýt“, tlačítko „Další výstup“, sekce mikrofonu (posuvník, mute) | Použitelná aplikace z okna |
| **M4 – Systray a životní cyklus** | Ikona, tooltip, levý/pravý klik, kontextové menu, ikona mute, minimalizace/zavření do tray, `--tray`, jediná instance | Aplikace žije v systray |
| **M5 – Autostart** | Registr HKCU Run, StartupApproved, kontrola a oprava cesty | Autostart bez UAC |
| **M6 – Lokalizace a motivy** | resx CZ/EN, autodetekce + přepnutí za běhu, Light/Dark + auto, tmavý titulek, okno Nastavení | Dvojjazyčná aplikace s motivy |
| **M7 – Klávesové zkratky** | `Win32HotkeyService`, ovládací prvek pro zachycení zkratky, hlášení konfliktů | Globální zkratky |
| **M8 – Aktualizace** | Podpisový klíč, `make-update-manifest.ps1`, rozšíření `release.yml`, `UpdateChecker` / `Downloader` / `Scheduler` + testy, `ExeSwapUpdateInstaller`, `DotNetRuntimeProbe`, oznamovací okno, položka v tray, `docs/RELEASING.md` | Ověřeno ostrou aktualizací `v0.8.0` → `v0.8.1` |
| **M9 – Dokončení** | O aplikaci, finální ikony, OSD oznámení, logování, ošetření chyb, migrace INI, `docs/TESTING.md` | Release candidate |
| **M10 – Vydání 1.0** | Zveřejnit repozitář, projít manuální checklist (Win11 + Win10), README se screenshoty, `v1.0.0` | Veřejné vydání |

Aktualizace (M8) je zařazená před vydáním 1.0 záměrně: všechny verze od 1.0 se pak
umí aktualizovat samy. Verze bez updateru by uživatel musel aktualizovat ručně.

Možná rozšíření po verzi 1.0 (zatím **mimo rozsah**): zkratka pro konkrétní zařízení,
profily (výstup + mikrofon najednou), drag & drop řazení, beta kanál aktualizací
(přes GitHub Pages), podepisování exe (Authenticode), přepínání výchozího mikrofonu.

## 10. Otevřené otázky

Momentálně žádné. Rozhodnuté otázky:

- **Q1** Licence v hlavičkách souborů → `GPL-3.0-or-later` (D17).
- **Q2** Ikona → vlastní návrh (D18).
- **Q3** Minimální verze Windows 10 → 1809 (build 17763), testuje se na 22H2 (D12).
- **Q4** Kontrola aktualizací → při startu a pak každých 24 h (F9.1).
