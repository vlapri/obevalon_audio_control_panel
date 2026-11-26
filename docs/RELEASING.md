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

## Podpisový klíč pro aktualizace (milník M8)
Postup vytvoření klíče ECDSA P-256 a jeho uložení do GitHub secretu `UPDATE_SIGNING_KEY`
bude doplněn v milníku M8. Do té doby vydání neobsahují `update.json`.
