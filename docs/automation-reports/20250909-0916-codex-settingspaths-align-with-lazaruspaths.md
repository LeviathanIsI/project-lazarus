# Automation Report Align SettingsPaths with LazarusPaths

- **Date:** 2025-09-09 09:16
- **Agents:** codex
- **Branch:** main
- **Before SHA:** f7cf21195df90a2529e7f520687b223f77c351f7
- **After SHA:** uncommitted

## 1) Intent

Stop creating %LOCALAPPDATA%\Lazarus\LazarusAI and align all settings/data directories to the canonical %LOCALAPPDATA%\Lazarus layout defined by LazarusPaths.

## 2) Outcome

- SettingsPaths now maps to LazarusPaths for all directories and settings files.
- SettingsFile/SettingsBackupFile live under System-Data\Configuration.
- EnsureDirectoriesExist() iterates LazarusPaths.EnumerateAllDirectories() and ensures System-Data\Configuration exists — no extra app subfolder created.

## 3) Files Changed

`	xt
modified  src/App.Shared/Settings/SettingsPaths.cs
`

## 4) Per-File Notes

- src/App.Shared/Settings/SettingsPaths.cs removed hard-coded LazarusAI subfolder; all paths proxy to LazarusPaths.

## 5) Commands / Scripts Touched

`
None
`

## 6) Validation

- Build succeeded (dotnet build Lazarus.sln -c Debug).
- Running will no longer create %LOCALAPPDATA%\Lazarus\LazarusAI; settings persist to %LOCALAPPDATA%\Lazarus\System-Data\Configuration\settings.json.

## 7) Next Steps

1. Delete any stray %LOCALAPPDATA%\Lazarus\LazarusAI folder manually if previously created (app no longer uses it).

## 8) Risks / Rollback

- **Risk:** Consumers expecting old SettingsPaths layout may need updated paths. The app’s own services already use SettingsPaths, which now points to the canonical layout.
- **Rollback:** git revert <after_sha>.
