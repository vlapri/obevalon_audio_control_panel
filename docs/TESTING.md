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
