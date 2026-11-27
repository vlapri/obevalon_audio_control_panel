# CLAUDE.md

Guidance for Claude Code when working in this repository.

## Project

**Obevalon Audio Control Panel**: a portable Windows 10/11 tray utility for switching the
default audio output, cycling between selected outputs, and controlling microphone
volume and mute. It also updates itself from GitHub Releases. Author: **Obevalon**. License: **GPL v3**.

**`docs/SPEC.md` is the single source of truth** (requirements F*, decisions D*, architecture,
repository structure, milestones M0–M10, open questions Q*). Read the relevant section before
starting any work. If a requirement changes, update SPEC.md first, then the code.

## Communication and language

- Talk to the author in **Czech**.
- Code, identifiers, code comments, commit messages and PR descriptions: **English**.
- `docs/SPEC.md` is in Czech. `README.md` is bilingual (EN first, then CZ).
- Every user-facing string goes through `.resx` resources and must exist in **both**
  `Strings.resx` (EN) and `Strings.cs.resx` (CZ). No hard-coded UI text.

## Tech stack

- C# / **.NET 10 (LTS)**, **WPF**, MVVM (`CommunityToolkit.Mvvm`), tray icon via `H.NotifyIcon.Wpf`.
- Core Audio via our own thin source-generated COM interop (`[GeneratedComInterface]`),
  including the undocumented `IPolicyConfig`. No NAudio.
- Unit tests: xUnit.
- Distribution: framework-dependent **single-file** exe (`OACP.exe`),
  `win-x64` and `win-arm64`.

## Hard rules

1. **No admin rights, no UAC.** The manifest stays `asInvoker`. Autostart uses
   `HKCU\...\Run`, not Task Scheduler or HKLM. Never add a feature that needs elevation
   without the author's explicit approval.
2. **Layering:** `Obevalon.AudioControlPanel.Core` targets plain `net10.0` and must not
   reference anything Windows-specific (no WPF, no registry, no P/Invoke, no COM). All
   P/Invoke and COM code goes in `Obevalon.AudioControlPanel.Platform`. UI goes in the WPF app project.
3. **No new NuGet dependency** without asking the author. Any dependency must have a
   GPL-v3-compatible license (MIT, Apache-2.0, BSD…). Manage versions centrally in `Directory.Packages.props`.
4. **Updates:** never download or install anything without the user's consent.
   Always verify the manifest signature (ECDSA P-256) and the SHA-256 of the downloaded exe.
   Never commit private keys. The signing key lives only in the GitHub secret `UPDATE_SIGNING_KEY`.
5. **Settings** are stored in `settings.ini` next to the exe, with a fallback to `%APPDATA%\Obevalon\AudioControlPanel\`.
   Writes are atomic. Bad values fall back to defaults and never crash the app. A format
   change requires bumping `SettingsVersion` and adding a migration.
6. Core Audio callbacks arrive on background threads. Marshal them to the UI via the `Dispatcher`.
   Never call back into Core Audio from inside an `IMMNotificationClient` callback.
7. Each source file starts with `// SPDX-License-Identifier: GPL-3.0-or-later`.

## Notes

- The WPF Fluent theme (`Application.ThemeMode`) is experimental in .NET 10; `WPF0001` is suppressed in the
  app project. Custom styles must use Fluent color resources via `DynamicResource` (e.g. `TextFillColorPrimaryBrush`).
- Log with `AppLog` (off by default; `--log` or `Logging=true`). Never log personal data beyond device names.
- The trusted update signing public keys are in `Core/Updates/update-signing-keys.pem`; see `docs/RELEASING.md`
  before changing them.

## Code style

- `Nullable` enabled, `TreatWarningsAsErrors` in Release, .NET analyzers on, follow `.editorconfig`.
- MVVM: no logic in code-behind beyond pure view concerns.
- Keep it small and simple. This is a lightweight utility, so avoid frameworks and abstractions it doesn't need.

## Build and test

```bash
dotnet build -c Release
```

```bash
dotnet test
```

- This project is often developed in a **Linux cloud container**. The Windows projects
  build there (`EnableWindowsTargeting` is set automatically in `Directory.Build.props`),
  but the WPF app **cannot run** there. Core tests (`net10.0`) run on Linux.
- If the container has no .NET SDK: `apt-get install -y dotnet-sdk-10.0`
  (the `dot.net` install script host may be blocked by the network policy).
- Icons: edit the SVG sources in `src/Obevalon.AudioControlPanel/Assets/`, then regenerate
  the `.ico` files with `node build/render-icons.mjs` (needs Node with Playwright + Chromium) and commit both.
- Audio, tray, hotkeys, autostart and the exe-swap update can only be verified on real
  Windows. Add any new manual checks to `docs/TESTING.md` and tell the author what to test.
- Release-like local publish (framework-dependent is the project default):
  `dotnet publish src/Obevalon.AudioControlPanel -c Release -r win-x64 -p:PublishSingleFile=true`
- CI (`.github/workflows/build.yml`) uploads a test build `OACP.exe` as an artifact for every PR.
  Point the author to it for manual testing.

## Git workflow

- `main` must always build. Work on feature branches, merge via pull request,
  one milestone or feature per PR.
- Conventional Commits: `feat:`, `fix:`, `docs:`, `ci:`, `refactor:`, `test:`, `chore:`.
- Versions come **only from git tags** (`vX.Y.Z`, SemVer). Don't hard-code version numbers.
  Pushing a tag triggers `.github/workflows/release.yml`, which publishes the exe files,
  zips, `SHA256SUMS.txt` and the signed `update.json` to GitHub Releases. Tags with a
  suffix (`-beta.1`) become pre-releases, which auto-update ignores.
- This cloud environment cannot push tags: start releases with the workflow's manual trigger
  (`release.yml`, input `version`).
- Executables are Authenticode-signed through SignPath (SignPath Foundation certificate) when the
  `SIGNPATH_*` repository variables are set; each signing request is approved manually by the author.
  `update.json` hashes are computed from the signed files. See `docs/RELEASING.md`.

## Repository map

See SPEC.md §6 for the full tree. In short:

- `src/Obevalon.AudioControlPanel/` – WPF app: `App.xaml.cs` (composition and lifecycle), Views, ViewModels,
  Tray, Hotkeys (HotkeyManager), Themes (Fluent ThemeMode), Localization, Controls
- `src/Obevalon.AudioControlPanel.Core/` – models, interfaces, INI settings, output catalog and cycle logic,
  autostart command, hotkey gestures, update logic (versions, manifest, signature, download), AppLog
- `src/Obevalon.AudioControlPanel.Platform/` – Core Audio COM interop, registry autostart, RegisterHotKey,
  exe-swap update installer, .NET runtime probe, single instance
- `tests/Obevalon.AudioControlPanel.Core.Tests/` – xUnit tests
- `build/` – `make-update-manifest.ps1` (release: update.json + signature), `render-icons.mjs` (icons)
- `docs/` – SPEC, RELEASING, TESTING
