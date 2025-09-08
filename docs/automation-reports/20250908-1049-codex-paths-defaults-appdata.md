# Automation Report Paths defaults use AppData (LazarusPaths)

- **Date:** 2025-09-08 10:49
- **Agents:** codex
- **Branch:** main
- **Before SHA:** 697b64bb2a87567aa10ae26dd11a10061a799d18
- **After SHA:** uncommitted

## 1) Intent
Make all default Paths settings resolve under the Lazarus AppData folder instead of hardcoded C:\ paths.

## 2) Outcome
Updated PathsSettingsViewModel to use SettingsPaths (Models, Cache, Temp, Exports) in ResetToDefault and to fallback to SettingsPaths in RefreshFromSettings when values are empty. Ensures directories are created via EnsureDirectoriesExist().

## 3) Files Changed
```txt
modified  src/App.Desktop/ViewModels/SettingsSections.cs
```

## 4) Per-File Notes
- src/App.Desktop/ViewModels/SettingsSections.cs PathsSettingsViewModel: now defaults to SettingsPaths and creates directories.

## 5) Commands / Scripts Touched
`
None
`"
# Automation Report Paths defaults use AppData (LazarusPaths)  - **Date:** 2025-09-08 10:49 - **Agents:** codex - **Branch:** main - **Before SHA:** 697b64bb2a87567aa10ae26dd11a10061a799d18 - **After SHA:** uncommitted  ## 1) Intent Make all default Paths settings resolve under the Lazarus AppData folder instead of hardcoded C:\ paths.  ## 2) Outcome Updated PathsSettingsViewModel to use SettingsPaths (Models, Cache, Temp, Exports) in ResetToDefault and to fallback to SettingsPaths in RefreshFromSettings when values are empty. Ensures directories are created via EnsureDirectoriesExist().  ## 3) Files Changed ```txt modified  src/App.Desktop/ViewModels/SettingsSections.cs ```  ## 4) Per-File Notes - src/App.Desktop/ViewModels/SettingsSections.cs PathsSettingsViewModel: now defaults to SettingsPaths and creates directories.  ## 5) Commands / Scripts Touched += "
# Automation Report Paths defaults use AppData (LazarusPaths)  - **Date:** 2025-09-08 10:49 - **Agents:** codex - **Branch:** main - **Before SHA:** 697b64bb2a87567aa10ae26dd11a10061a799d18 - **After SHA:** uncommitted  ## 1) Intent Make all default Paths settings resolve under the Lazarus AppData folder instead of hardcoded C:\ paths.  ## 2) Outcome Updated PathsSettingsViewModel to use SettingsPaths (Models, Cache, Temp, Exports) in ResetToDefault and to fallback to SettingsPaths in RefreshFromSettings when values are empty. Ensures directories are created via EnsureDirectoriesExist().  ## 3) Files Changed ```txt modified  src/App.Desktop/ViewModels/SettingsSections.cs ```  ## 4) Per-File Notes - src/App.Desktop/ViewModels/SettingsSections.cs PathsSettingsViewModel: now defaults to SettingsPaths and creates directories.  ## 5) Commands / Scripts Touched += 
- Build succeeded
- New installs will pick %LOCALAPPDATA%\\Lazarus\\LazarusAI subfolders

## 7) Next Steps
1. If additional path fields are added later, follow the same SettingsPaths pattern.

## 8) Risks / Rollback
- **Risk:** Existing users with custom paths are unaffected; those with blanks get AppData defaults.
- **Rollback:** Revert the ViewModel changes.
