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
