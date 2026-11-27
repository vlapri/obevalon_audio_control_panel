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
   Nebo bez gitu: GitHub → *Actions → Release → Run workflow*, větev `main`, verze `1.2.3`.
   Workflow tag `v1.2.3` vytvoří sám (pokud už existuje, skončí chybou).
4. Workflow *Release* (`.github/workflows/release.yml`):
   - spustí testy,
   - sestaví `OACP.exe` pro `win-x64` a `win-arm64` s verzí z tagu,
   - vytvoří GitHub Release s automaticky generovanými poznámkami a assety:
     `OACP-<verze>-<rid>.exe`, `OACP-<verze>-<rid>.zip` (exe + LICENSE + README), `SHA256SUMS.txt`.
5. Zkontrolovat a případně doplnit poznámky k vydání na GitHubu.

Tagy s příponou (např. `v1.3.0-beta.1`) se vydají jako **pre-release**.

## Podepisování exe (SignPath)

Windows 11 s **Inteligentním řízením aplikací** (Smart App Control) nepodepsané programy úplně blokuje.
Exe se proto podepisují certifikátem **SignPath Foundation** (zdarma pro open source) přes službu SignPath.io.
Workflow *Release* podepisuje automaticky, jakmile je nastavená proměnná `SIGNPATH_ORGANIZATION_ID`;
bez ní vydání proběhne nepodepsaně (s varováním v logu).

### Jednorázové nastavení (po schválení žádosti SignPath Foundation)
1. V SignPath (app.signpath.io) zkontrolujte projekt, který Foundation pro repozitář založila, a jeho
   **slug** (např. `obevalon_audio_control_panel`).
2. **Artifact configuration**: vložte obsah `.signpath/artifact-configuration.xml` (podepisuje oba exe
   v artefaktu `unsigned-executables`) a poznamenejte si její slug (nebo ji nastavte jako výchozí).
3. **Signing policy**: použijte politiku pro vydání (typicky `release-signing`) navázanou na tento
   repozitář přes *Trusted build system* GitHub.com.
4. Vytvořte **API token** (uživatel s rolí *Submitter* v projektu).
5. GitHub → *Settings → Secrets and variables → Actions*:
   - secret `SIGNPATH_API_TOKEN` = API token,
   - variables:
     - `SIGNPATH_ORGANIZATION_ID` = ID organizace (GUID z URL v SignPath),
     - `SIGNPATH_PROJECT_SLUG` = slug projektu,
     - `SIGNPATH_SIGNING_POLICY_SLUG` = slug politiky (výchozí `release-signing`),
     - `SIGNPATH_ARTIFACT_CONFIGURATION_SLUG` = slug konfigurace artefaktu (nepovinné, jinak výchozí).

### Průběh vydání s podpisem
1. Workflow sestaví nepodepsaná exe a nahraje je jako artefakt `unsigned-executables`.
2. Odešle žádost o podpis do SignPath a **čeká až 60 minut** na dokončení.
3. **Schválení**: žádost o podpis vydání schvaluje *Approver* ručně v SignPath (e-mail / app.signpath.io).
4. Podepsaná exe workflow ověří (`Get-AuthenticodeSignature` musí být `Valid`), zabalí je do zip,
   spočítá z nich `update.json` (SHA-256 podepsaných souborů) a vytvoří vydání.

Pravidla SignPath Foundation vyžadují v README sekci o podepisování a zásady ochrany soukromí
(viz README → *Code signing policy*, *Privacy policy*). Při změně týmu je aktualizujte.

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
