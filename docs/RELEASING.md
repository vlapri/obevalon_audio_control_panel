# Vydání nové verze

Verze se řídí [SemVer](https://semver.org/lang/cs/) a bere se **pouze z git tagu**.

## Postup
1. Všechny změny jsou zmergované v `main` a workflow *Build* je zelený.
2. Projít manuální checklist v [TESTING.md](TESTING.md) s testovacím buildem z `main`.
3. Vytvořit a pushnout tag:
   ```bash
   git checkout main && git pull
   ```
   ```bash
   git tag v1.2.3
   ```
   ```bash
   git push origin v1.2.3
   ```
4. Workflow *Release* (`.github/workflows/release.yml`):
   - spustí testy,
   - sestaví `OACP.exe` pro `win-x64` a `win-arm64` s verzí z tagu,
   - vytvoří GitHub Release s automaticky generovanými poznámkami a assety:
     `OACP-<verze>-<rid>.exe`, `OACP-<verze>-<rid>.zip` (exe + LICENSE + README), `SHA256SUMS.txt`.
5. Zkontrolovat a případně doplnit poznámky k vydání na GitHubu.

Tagy s příponou (např. `v1.3.0-beta.1`) se vydají jako **pre-release**.

## Podpisový klíč pro aktualizace

Aplikace instaluje jen aktualizace, jejichž `update.json` je podepsaný klíčem ECDSA P-256.
Veřejné klíče, kterým aplikace věří, jsou v souboru
`src/Obevalon.AudioControlPanel.Core/Updates/update-signing-keys.pem` (zabudovaný do exe).
Soukromý klíč je **jen** v GitHub secretu `UPDATE_SIGNING_KEY` a v záloze autora; nikdy se necommituje.

Workflow *Release* (`build/make-update-manifest.ps1`) selže, pokud secret chybí nebo neodpovídá
žádnému důvěryhodnému veřejnému klíči. Vydání tak nikdy neobsahuje manifest, který by aplikace odmítla.

### Jednorázové nastavení
1. Otevřete soubor se soukromým klíčem (`UPDATE_SIGNING_KEY.pem`, viz níže) a zkopírujte celý obsah
   včetně řádků `-----BEGIN PRIVATE KEY-----` a `-----END PRIVATE KEY-----`.
2. GitHub → repozitář → *Settings → Secrets and variables → Actions → New repository secret*:
   název `UPDATE_SIGNING_KEY`, hodnota = obsah souboru.
3. Soubor si zálohujte mimo GitHub (např. ve správci hesel). **Bez něj už stávající instalace
   nepůjde automaticky aktualizovat.**
4. Soubor smažte z disku, kde nemá zůstat.

### Vytvoření nového klíče (pokud původní chybí nebo byl prozrazen)
```bash
openssl ecparam -name prime256v1 -genkey -noout | openssl pkcs8 -topk8 -nocrypt -out UPDATE_SIGNING_KEY.pem
```
```bash
openssl pkey -in UPDATE_SIGNING_KEY.pem -pubout
```
Druhý příkaz vypíše veřejný klíč. Postup výměny:
1. Přidejte nový veřejný klíč do `update-signing-keys.pem` (starý zatím ponechte) a vydejte verzi
   ještě podepsanou **starým** klíčem, aby se k uživatelům dostala aplikace, která zná nový klíč.
2. Potom změňte secret `UPDATE_SIGNING_KEY` na nový klíč; starý veřejný klíč lze později odebrat.
3. Při prozrazení klíče starý veřejný klíč odeberte hned a vydejte novou verzi; uživatelé ji
   musí jednou nainstalovat ručně.

## Testování aktualizací
- V `settings.ini` lze nastavit `[Updates] ManifestUrl=` na vlastní adresu manifestu
  (např. testovací pre-release). Podpis se ověřuje vždy.
- Lokální a CI sestavení (verze `0.0.0-…`) automaticky nekontrolují; ruční kontrola v Nastavení funguje.
- Ostrý test: vydat `vX.Y.Z`, nainstalovat ho, vydat `vX.Y.(Z+1)` a ověřit aktualizaci podle `TESTING.md` (M8).
