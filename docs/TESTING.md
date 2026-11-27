# Manuální testování

Audio, systray, klávesové zkratky, autostart a aktualizace nelze ověřit v CI.
Před každým vydáním projděte tento checklist na **Windows 11** a namátkově na **Windows 10 22H2**.

Testovací build z libovolného PR nebo z `main` najdete v záložce **Actions** →
běh workflow *Build* → artefakt `OACP-ci-<číslo>-win-x64`.

## Požadavky na testovací počítač
- Nainstalovaný [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0).

## M0 – Kostra aplikace
- [ ] `OACP.exe` se spustí **bez dotazu UAC**.
- [ ] Okno zobrazuje název, verzi, autora, licenci a odkaz na GitHub.
- [ ] Odkaz „Projekt na GitHubu“ / „Project on GitHub“ otevře repozitář ve výchozím prohlížeči.
- [ ] Na českém Windows je text česky, na anglickém (a jiném) anglicky.
- [ ] Ikona je vidět v záhlaví okna, na hlavním panelu a u souboru v Průzkumníku.
- [ ] Při škálování 150 % a 200 % je okno ostré (ne rozmazané).
- [ ] Vlastnosti souboru → Podrobnosti: produkt „Obevalon Audio Control Panel“, společnost „Obevalon“, správná verze.
- [ ] Bez nainstalovaného .NET 10 Desktop Runtime Windows zobrazí dialog s odkazem ke stažení.
- [ ] (ARM64 zařízení) `OACP-<verze>-win-arm64.exe` se spustí.

## M1 – Core Audio (přepínání výstupů a mikrofon)
Pro přehrávání zvuku během testu stačí třeba video na YouTube.

### Audio výstupy
- [ ] Seznam obsahuje stejná zařízení jako *Nastavení → Systém → Zvuk*, odpojená a zakázaná jsou šedá se stavem.
- [ ] Aktuální výchozí výstup má fajfku, tučný název a popisek „Výchozí“.
- [ ] Kliknutí na jiný výstup okamžitě přepne zvuk (jack ↔ HDMI).
- [ ] Po přepnutí je zařízení výchozí i pro komunikaci (*Ovládací panely → Zvuk*: zelená fajfka i ikona telefonu).
- [ ] Změna výchozího výstupu přímo ve Windows se hned projeví v aplikaci.
- [ ] Vypnutí a zapnutí projektoru (HDMI): zařízení se v seznamu změní na „Odpojeno“ a zpět bez restartu aplikace.
- [ ] Připojení a odpojení USB nebo Bluetooth sluchátek seznam aktualizuje.
- [ ] Přejmenování zařízení ve Windows se projeví v seznamu.

### Mikrofon
- [ ] Zobrazuje se název výchozího mikrofonu a jeho aktuální hlasitost.
- [ ] Posuvník mění hlasitost mikrofonu (ověřit v *Nastavení → Zvuk → Vstup*), tažení je plynulé a neposkakuje.
- [ ] Změna hlasitosti mikrofonu ve Windows se projeví na posuvníku.
- [ ] Tlačítko s mikrofonem ztlumí mikrofon (ikona se změní na přeškrtnutou červenou) a znovu zapne.
- [ ] Ztlumení mikrofonu jinde (Windows, Teams, tlačítko na headsetu) se projeví v aplikaci.
- [ ] Změna výchozího mikrofonu ve Windows přepne ovládání na nový mikrofon.
- [ ] Bez mikrofonu (odpojený nebo zakázaný) se zobrazí „Nebyl nalezen žádný mikrofon.“

### Stabilita
- [ ] Po mnoha přepnutích a změnách hlasitosti aplikace běží bez chyb.
- [ ] Po zavření okna nezůstane proces `OACP.exe` ve Správci úloh.

## M2 – Nastavení
- [ ] Po prvním spuštění vznikne `settings.ini` vedle `OACP.exe` a obsahuje sekci `[Output:…]` pro každý výstup.
- [ ] Když je `OACP.exe` ve složce bez práva zápisu (např. `C:\Program Files\OACP\`), nastavení se uloží do `%APPDATA%\Obevalon\AudioControlPanel\settings.ini` a aplikace nehlásí chybu.
- [ ] Ruční úprava `settings.ini` (při vypnuté aplikaci) se po spuštění projeví:
  - `Name=` a `Note=` u výstupu změní zobrazený název a poznámku,
  - změna `Order=` změní pořadí,
  - `Hidden=true` výstup skryje,
  - `Language=en` / `Language=cs` přepne jazyk.
- [ ] Poškozený nebo nesmyslný `settings.ini` nezpůsobí pád; použijí se výchozí hodnoty.
- [ ] Výstup, který už v systému není (např. po výměně ovladače), zůstane v seznamu šedý se stavem „Chybí“.

## M3 – Hlavní okno
- [ ] Po najetí myší na řádek výstupu se objeví tlačítka: nahoru, dolů, přejmenovat, skrýt (u chybějícího výstupu i odebrat).
- [ ] Šipky nahoru a dolů mění pořadí; pořadí zůstane i po restartu aplikace.
- [ ] Ikona cyklu vpravo zapne nebo vypne zařazení do cyklu (zvýrazněná = v cyklu).
- [ ] „Další výstup“ přepíná dokola mezi výstupy v cyklu v pořadí seznamu; odpojené a skryté přeskakuje.
- [ ] Když v cyklu nic není, „Další výstup“ přepíná mezi všemi zobrazenými dostupnými výstupy.
- [ ] Při méně než dvou dostupných výstupech v cyklu se zobrazí vysvětlující text.
- [ ] Tužka otevře úpravu názvu a poznámky; Enter nebo „Uložit“ uloží, Esc nebo „Zrušit“ zahodí změny.
- [ ] Prázdný název vrátí systémový název; poznámka se zobrazí pod názvem.
- [ ] Skrytý výstup zmizí; se zaškrtnutým „Zobrazit skryté“ je vidět kurzívou s popiskem „Skryté“ a jde znovu zobrazit.
- [ ] Chybějící výstup jde odebrat ze seznamu.
- [ ] Okno se po restartu otevře na stejném místě; po odpojení monitoru se otevře na středu obrazovky.
- [ ] Všechny změny se okamžitě propíšou do `settings.ini`.

## M4 – Systray a životní cyklus
- [ ] Po spuštění je v oznamovací oblasti (systray) ikona aplikace, ostrá i při škálování 125–200 %.
- [ ] Levý klik na ikonu otevře a aktivuje hlavní okno (i když bylo schované nebo minimalizované).
- [ ] Pravý klik otevře nabídku: dostupné výstupy (aktuální zaškrtnutý), „Další výstup“, ztlumení mikrofonu, „Otevřít“, „Ukončit“.
- [ ] Kliknutí na výstup v nabídce ho přepne; „Další výstup“ přepne dokola.
- [ ] Položka mikrofonu ztlumí/zapne mikrofon; při ztlumení má ikona v systray červený odznak.
- [ ] Tooltip ikony ukazuje aktuální výstup a stav mikrofonu a mění se s nimi.
- [ ] Minimalizace okna ho schová do systray (nezůstane na hlavním panelu).
- [ ] Zavření okna křížkem ho schová do systray; „Ukončit“ v nabídce aplikaci opravdu ukončí (proces zmizí ze Správce úloh).
- [ ] S `CloseToTray=false` v `settings.ini` zavření okna aplikaci ukončí.
- [ ] `OACP.exe --tray` se spustí jen do systray bez okna.
- [ ] Druhé spuštění `OACP.exe` neotevře druhou instanci, ale zobrazí okno té první.
- [ ] Výstup s podtržítkem v názvu se v nabídce zobrazí správně.

## M5 – Autostart a okno Nastavení
- [ ] Ozubené kolečko v hlavním okně i „Nastavení…“ v nabídce systray otevře okno Nastavení (jen jedno).
- [ ] Zaškrtnutí „Spouštět po přihlášení“ nevyvolá UAC a v `regedit` vznikne `HKCU\Software\Microsoft\Windows\CurrentVersion\Run\ObevalonAudioControlPanel` = `"…\OACP.exe" --tray`.
- [ ] Po odhlášení a přihlášení se aplikace spustí jen do systray, bez dotazu UAC.
- [ ] Vypnutí autostartu ve Správci úloh (Aplikace po spuštění) se v Nastavení zobrazí jako upozornění; vypnutím a zapnutím v aplikaci se autostart znovu povolí.
- [ ] Odškrtnutí autostartu hodnotu z registru odstraní.
- [ ] Po přesunutí portable složky jinam a spuštění se aplikace zeptá, zda opravit cestu autostartu; „Ano“ cestu opraví.
- [ ] Nastavení ukazuje cestu k `settings.ini` a informaci o portable režimu; „Otevřít složku“ otevře Průzkumníka s označeným souborem.

## M6 – Jazyk, motivy a nastavení
- [ ] Motiv „Automaticky“: okna, nabídka v systray a tooltipy mají světlý/tmavý vzhled podle *Nastavení → Přizpůsobení → Barvy → Režim aplikací*; přepnutí ve Windows se projeví za běhu.
- [ ] Motiv „Světlý“ / „Tmavý“ se použije okamžitě a zůstane po restartu; titulek okna odpovídá motivu.
- [ ] V tmavém motivu je veškerý text čitelný (seznam výstupů, tlačítka v řádcích, úprava názvu, posuvník, zaškrtávátka, rozbalovací seznamy).
- [ ] Jazyk „Čeština“ / „English“ přepne texty v otevřených oknech okamžitě, bez restartu; „Automaticky“ použije jazyk Windows.
- [ ] Výběr mikrofonu: „Výchozí mikrofon systému“ sleduje výchozí mikrofon; konkrétní mikrofon se ovládá i po změně výchozího ve Windows.
- [ ] „Přepnutí výstupu změní i zařízení pro komunikaci“ vypnuté → po přepnutí zůstane komunikační zařízení beze změny.
- [ ] „Zavřením okna aplikaci ukončit“ → křížek aplikaci ukončí; vypnuté → schová do systray.
- [ ] Všechny volby se uloží do `settings.ini` a platí po restartu.
- [ ] (Windows 10) Vzhled je v pořádku i bez efektu Mica.

## M7 – Klávesové zkratky
- [ ] Ve výchozím stavu není přiřazená žádná zkratka.
- [ ] V Nastavení: klik do pole a stisk např. `Ctrl+Alt+A` zapíše zkratku; Backspace nebo křížek ji smaže; samotný Shift+písmeno nebo samotné písmeno se nepřijme.
- [ ] Po zavření Nastavení fungují zkratky v celých Windows (i když je okno aplikace schované):
  - „Další výstup“ přepne výstup,
  - „Ztlumit / zapnout mikrofon“ přepne mikrofon (změní se i ikona v systray),
  - „Zobrazit okno“ zobrazí hlavní okno.
- [ ] Stejnou zkratku nejde přiřadit dvěma akcím (zobrazí se chyba).
- [ ] Zkratka používaná jinou aplikací (nebo Windows, např. `Win+L`) zobrazí upozornění „Tuto zkratku používá jiná aplikace nebo Windows“.
- [ ] Zkratky platí i po restartu (uloženo v `[Hotkeys]` v `settings.ini`).

## M8 – Aktualizace
Předpoklad: secret `UPDATE_SIGNING_KEY` je nastavený a existují dvě vydání (např. `v0.8.0` a `v0.8.1`).
- [ ] Vydání obsahuje `update.json`, `update.json.sig`, exe a zip pro x64 i ARM64 a `SHA256SUMS.txt`.
- [ ] Nainstalovaná starší verze asi 30 s po startu zobrazí vpravo dole okno „Je k dispozici nová verze“; ikona v systray dostane zelený odznak a v nabídce je položka „Aktualizovat na verzi …“.
- [ ] Bez kliknutí na „Aktualizovat“ se nic nestáhne (ověřit v `%TEMP%\Obevalon\AudioControlPanel\update`).
- [ ] „Co je nového“ otevře stránku vydání.
- [ ] „Aktualizovat“ zobrazí průběh stahování, aplikace se restartuje v nové verzi bez UAC a zobrazí „Aktualizováno na verzi …“; `settings.ini` zůstal beze změny; `OACP.exe.old` po startu zmizí.
- [ ] Byla-li aplikace při aktualizaci jen v systray, nová verze se spustí znovu jen do systray.
- [ ] „Později“ okno zavře; při další automatické kontrole (za 24 h nebo po restartu) se nabídka objeví znovu.
- [ ] „Přeskočit tuto verzi“ – o této verzi už aplikace neupozorní (ručně v Nastavení ji ale stále nabídne).
- [ ] Nastavení → Aktualizace → „Zkontrolovat nyní“ ukáže „Máte nejnovější verzi“ nebo otevře nabídku; zobrazí čas poslední kontroly.
- [ ] Vypnutá automatická kontrola → po startu se nic nekontroluje.
- [ ] Aplikace ve `C:\Program Files\…` (bez práva zápisu) → „Aktualizovat“ zobrazí vysvětlení a odkaz na stránku ke stažení.
- [ ] Bez internetu automatická kontrola nic nezobrazí; ruční kontrola ukáže chybu.
- [ ] Podvržený `update.json` (např. přes `ManifestUrl` na upravený soubor) je odmítnut („neplatný podpis“).

## M9 – Dokončení
- [ ] Ikona „i“ v hlavním okně i „O aplikaci…“ v nabídce systray otevře okno O aplikaci: název, verze, autor Obevalon, odkaz na GitHub, text licence a odkaz na ni, „Zkontrolovat nyní“.
- [ ] Přepnutí výstupu z nabídky v systray nebo klávesovou zkratkou zobrazí nad hlavním panelem na ~1,5 s oznámení s názvem výstupu; nekrade fokus.
- [ ] Ztlumení/zapnutí mikrofonu zkratkou nebo z nabídky zobrazí oznámení „Mikrofon ztlumen/zapnut“.
- [ ] Při práci přímo v hlavním okně se oznámení nezobrazuje; s vypnutým „Po přepnutí zobrazit krátké oznámení“ se nezobrazuje nikdy.
- [ ] `OACP.exe --log` nebo volba „Zapisovat diagnostický log“ vytvoří `logs\oacp-RRRR-MM-DD.log` vedle `settings.ini` se záznamy o startu, přepnutích a chybách.

## Před každým vydáním
- [ ] Projít sekce M1–M9 na Windows 11 (x64), hlavní body na Windows 10 22H2.
- [ ] Dlouhodobý běh: aplikace běží celý den v systray, přepínání a zkratky stále fungují, paměť nenarůstá.
- [ ] Přepínání při vypnutém/zapnutém projektoru, uspání a probuzení PC, odpojení USB/Bluetooth zařízení.
