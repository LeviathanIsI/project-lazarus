# Repository Guidelines

## Project Structure & Module Organization

- `src/App.Desktop` (WPF startup): Views, ViewModels, Services, Resources/Themes, `appsettings*.json`.
- `src/App.Data` (EF Core Sqlite): Entities, Repositories, Configurations, `Migrations/`, `LazarusDbContext`.
- `src/App.Backend`: Orchestration/business logic; references `App.Shared`.
- `src/App.Shared`: `LazarusPaths`, model artifacts, parameter schema shared types.
- Root: `Lazarus.sln`, `Directory.Build.props` (startup=`App.Desktop`), `.vscode/`, `.vs/`, `.gitignore`, `binaries/`, `logs/`.

## Build, Test, and Development Commands

- Build all: `dotnet build Lazarus.sln -c Debug` (restore + compile).
- Run desktop app: `dotnet run --project src/App.Desktop -c Debug`.
- Format: `dotnet format` (fixes style issues before PRs).
- Tests (when present): `dotnet test` (discovers `*.Tests` projects).
- EF Core (Data):
  - Add migration: `dotnet ef migrations add <Name> -p src/App.Data -s src/App.Desktop`.
  - Apply DB: `dotnet ef database update -p src/App.Data -s src/App.Desktop`.

## Coding Style & Naming Conventions

- C#/.NET 8; nullable enabled; warnings treated as errors.
- Indentation: 4 spaces; one file per public type.
- Naming: PascalCase (types/methods), camelCase (locals/params), `_camelCase` (private fields).
- Prefer DI; add XML docs for public APIs; use `LazarusPaths` instead of absolute paths.
- WPF resources: keep App.xaml merge order stable; reuse existing styles/tokens.

## Testing Guidelines

- No test projects yet; prefer xUnit under `tests/` (e.g., `App.Data.Tests`).
- Name tests `ClassNameTests.cs`; arrange–act–assert; cover repositories/services and ViewModels.
- Run via `dotnet test`; target meaningful coverage for changed code.

## Commit & Pull Request Guidelines

- Conventional Commits (`feat`, `fix`, `docs`, `chore`, `refactor`, `style`, `ux`, …) in imperative mood.
- PRs: clear description, linked issues, screenshots/GIFs for UI changes, test plan, and EF migration notes if schema changed.
- Keep diffs focused; no secrets in commits; include example `appsettings.*.json` only.

## Security & Configuration Tips

- Do not commit secrets; prefer `appsettings.Development.json` and `*.local.json` (ignored).
- Runtime data lives under `%LOCALAPPDATA%/Lazarus` via `LazarusPaths`.

## Post-Op Automation Reports (Required)

Purpose: every automated change must leave a human-readable paper trail.

- Where to save: `docs/automation-reports/` (commit it).
- Filename: `YYYYMMDD-HHmm-<agent(s)>-<task-slug>.md` (24h local), e.g., `20250907-1432-wpf-stylist-compact-buttons.md`.
- If missing, create `docs/automation-reports/`.
- If git data missing, use `"unknown"/"uncommitted"` (do not omit fields).
- Report is part of the same commit as the code changes.
- Commit suffix example: append ` — docs: add automation report`.
- Template file: `docs/automation-reports/TEMPLATE.md`.

### What the report must contain

- Header metadata: Date/time (local), Agent(s), Branch, Before/After SHAs.
- 1. Intent, 2) Outcome, 3) Files Changed, 4) Per-File Notes,
  2. Commands/Scripts Touched, 6) Validation, 7) Next Steps, 8) Risks/Rollback.

### Prompt Tail (append this to any agent prompt)

```
POST-OP REPORT (MANDATORY)
After applying edits and validating the build:
1) Ensure docs/automation-reports/ exists. If not, create it.
2) Collect environment info:
   - branch = output of `git rev-parse --abbrev-ref HEAD` or "unknown"
   - before_sha = commit before edits (if known) or "uncommitted"
   - after_sha  = current HEAD (if committed) or "uncommitted"
   - files_changed = from `git status --porcelain` or a generated list of touched files
3) Write a Markdown report using the template in AGENTS.md (see "Automation Report Template").
   File path: docs/automation-reports/YYYYMMDD-HHmm-<agent(s)>-<task-slug>.md
4) Stage and commit the report together with the code changes.
   Append "docs: add automation report" to the commit message.
```

## Automation Report Template (excerpt)

See full template in `docs/automation-reports/TEMPLATE.md`.

````markdown
# Automation Report <Task Title>

- **Date:** <YYYY-MM-DD HH:mm>
- **Agents:** <codex>
- **Branch:** <branch-or-unknown>
- **Before SHA:** <before-or-uncommitted>
- **After SHA:** <after-or-uncommitted>

## 1) Intent

<One short paragraph describing what the run set out to do.>

## 2) Outcome

<What changed and why. Note any deviations from the plan.>

## 3) Files Changed

```txt
<added|modified|deleted|renamed>  <relative/path>
```
````

## 4) Per-File Notes

- `<relative/path>` <1 line summary>
- `<relative/path>` <1 line summary>

## 5) Commands / Scripts Touched

```
<list any new or changed commands, tasks, scripts, or config flags>
```

## 6) Validation

- Build succeeded locally
- App launched
- Feature verified: <bullet list>
- Evidence: <paths to screenshots/logs if any>

## 7) Next Steps

1. <Actionable follow-up w/ owner if known>
2. <Actionable follow-up>

## 8) Risks / Rollback

- **Risk:** <short description> **Mitigation:** <how to mitigate>
- **Rollback:** `git revert <after_sha>` or revert the commit(s) that introduced these changes.

```

## Minimal Implementation Notes (Agents Must Follow)
- Keep scope tight: one focused task per change; avoid drive-by refactors.
- Build and run locally: `dotnet build Lazarus.sln -c Debug`; `dotnet run --project src/App.Desktop`.
- Fix all warnings (treated as errors); maintain correct nullable annotations.
- WPF: preserve App.xaml merge order; no view-level `MergedDictionaries`; reuse existing styles/tokens.
- Filesystem: use `LazarusPaths`; never hard-code absolute paths.
- Data: create EF migration and apply via Desktop startup project.
- Formatting & naming: run `dotnet format`; PascalCase types/methods, camelCase locals/params, `_camelCase` private fields.
- Secrets: never commit credentials; prefer `appsettings.Development.json` and `*.local.json`.
- Git hygiene: don’t commit `binaries/` or `logs/`; use Conventional Commits.
- Post‑Op report: save to `docs/automation-reports/` with required filename and commit alongside code.

## Codex Prompt — First-Run Directory Bootstrap (Exact Lazarus Layout)

### Read First
- Windows-only: use `%LOCALAPPDATA%` and create folders under `Lazarus/`.
- Idempotent: create missing directories; do not modify or delete existing files.
- No network, installs, or downloads; local filesystem operations only.
- Echo a summary of created vs existing paths.
- After completion, file an automation report per this guide and commit it with the code.

### Exact Layout (from `LazarusPaths`)
```

%LOCALAPPDATA%\Lazarus
├─ Models
│ ├─ Base-Models
│ ├─ Embeddings
│ ├─ LoRA-Adapters
│ └─ Tokenizers
├─ Generation-Assets
│ ├─ ControlNet
│ ├─ Style-Presets
│ ├─ Upscale-Models
│ └─ VAE-Models
├─ Shared-Resources
│ ├─ External-Links
│ └─ Import-Export
└─ System-Data
├─ Cache
├─ Configuration
│ └─ Model-Presets
├─ Database
└─ Logs

```

### Prompt Snippet (copy into your agent prompt)
```

Task: First-run directory bootstrap for Lazarus (Windows).
Requirements: Idempotent; create only missing folders under %LOCALAPPDATA%\Lazarus; no deletions; no network. Afterward, generate a Post-Op automation report per AGENTS.md.

Steps:

1. Resolve $root = Join-Path $env:LOCALAPPDATA 'Lazarus'.
2. Create the exact subfolders listed below.
3. Print a sorted summary of Created/Exists for each path.
4. Write an automation report to docs/automation-reports with the required filename format, listing created paths and environment info.
5. Stage and commit alongside any related code changes with the suffix " — docs: add automation report".

Exact subfolders relative to $root:

- Models\Base-Models
- Models\Embeddings
- Models\LoRA-Adapters
- Models\Tokenizers
- Generation-Assets\ControlNet
- Generation-Assets\Style-Presets
- Generation-Assets\Upscale-Models
- Generation-Assets\VAE-Models
- Shared-Resources\External-Links
- Shared-Resources\Import-Export
- System-Data\Cache
- System-Data\Configuration
- System-Data\Configuration\Model-Presets
- System-Data\Database
- System-Data\Logs

Reference PowerShell:
$root = Join-Path $env:LOCALAPPDATA 'Lazarus'
$dirs = @(
'Models/Base-Models','Models/Embeddings','Models/LoRA-Adapters','Models/Tokenizers',
'Generation-Assets/ControlNet','Generation-Assets/Style-Presets','Generation-Assets/Upscale-Models','Generation-Assets/VAE-Models',
'Shared-Resources/External-Links','Shared-Resources/Import-Export',
'System-Data/Cache','System-Data/Configuration','System-Data/Configuration/Model-Presets','System-Data/Database','System-Data/Logs'
) | ForEach-Object { Join-Path $root $_ }
$results = foreach ($d in $dirs) {
  if (-not (Test-Path -LiteralPath $d)) { [void](New-Item -ItemType Directory -Force -Path $d); [pscustomobject]@{ Path=$d; Created=$true } }
  else { [pscustomobject]@{ Path=$d; Created=$false } }
}
$results | Sort-Object Path | Format-Table -AutoSize

```

## Minimal Smoke Test (Optional)
- Build: `dotnet build Lazarus.sln -c Debug`
- Verify folder layout (PowerShell):
```

$root = Join-Path $env:LOCALAPPDATA 'Lazarus'
$paths = @(
'Models\Base-Models','Models\Embeddings','Models\LoRA-Adapters','Models\Tokenizers',
'Generation-Assets\ControlNet','Generation-Assets\Style-Presets','Generation-Assets\Upscale-Models','Generation-Assets\VAE-Models',
'Shared-Resources\External-Links','Shared-Resources\Import-Export',
'System-Data\Cache','System-Data\Configuration','System-Data\Configuration\Model-Presets','System-Data\Database','System-Data\Logs',
'User-Content\Generated-Output','User-Content\Input-Files','User-Content\Projects',
'logs'
)
$paths | ForEach-Object {
$p = Join-Path $root $\_
'{0,-80} {1}' -f $p, (Test-Path -LiteralPath $p)
}

```

## Validation
- Move or delete `%LOCALAPPDATA%\Lazarus`, then launch the desktop app; the exact folders above should be recreated automatically.
- Preset saves must land in `System-Data\Configuration\Model-Presets\`.
- Example debug output (if logging/console visible):
  - `LAZARUS_HOME => <resolved root>`
  - `Models => <resolved Models root>`

Commit example: `chore(bootstrap): create Lazarus folder structure on first run (exact existing layout)`
```
