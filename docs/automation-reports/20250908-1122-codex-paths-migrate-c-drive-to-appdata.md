# Automation Report Migrate legacy C:\\Lazarus paths to AppData

- **Date:** 2025-09-08 11:22
- **Agents:** codex
- **Branch:** main
- **Before SHA:** 52dc31c760609b9e7ab0e149d8b2caddc9b2a874
- **After SHA:** uncommitted

## 1) Intent
Ensure Paths view shows AppData defaults even if older settings still point to C:\\Lazarus.*

## 2) Outcome
Added normalization in PathsSettingsViewModel to replace blank or legacy C:\\Lazarus values with LazarusPaths AppData defaults for Models, Cache, Temp, and Export. Values save back to settings on apply.

## 3) Files Changed
```txt
modified  src/App.Desktop/ViewModels/SettingsSections.cs
```

## 4) Per-File Notes
- src/App.Desktop/ViewModels/SettingsSections.cs NormalizePathToAppData() migrates to AppData for display and subsequent saves.

## 5) Commands / Scripts Touched
`
None
`"
# Automation Report Migrate legacy C:\\Lazarus paths to AppData  - **Date:** 2025-09-08 11:22 - **Agents:** codex - **Branch:** main - **Before SHA:** 52dc31c760609b9e7ab0e149d8b2caddc9b2a874 - **After SHA:** uncommitted  ## 1) Intent Ensure Paths view shows AppData defaults even if older settings still point to C:\\Lazarus.*  ## 2) Outcome Added normalization in PathsSettingsViewModel to replace blank or legacy C:\\Lazarus values with LazarusPaths AppData defaults for Models, Cache, Temp, and Export. Values save back to settings on apply.  ## 3) Files Changed ```txt modified  src/App.Desktop/ViewModels/SettingsSections.cs ```  ## 4) Per-File Notes - src/App.Desktop/ViewModels/SettingsSections.cs NormalizePathToAppData() migrates to AppData for display and subsequent saves.  ## 5) Commands / Scripts Touched += "
# Automation Report Migrate legacy C:\\Lazarus paths to AppData  - **Date:** 2025-09-08 11:22 - **Agents:** codex - **Branch:** main - **Before SHA:** 52dc31c760609b9e7ab0e149d8b2caddc9b2a874 - **After SHA:** uncommitted  ## 1) Intent Ensure Paths view shows AppData defaults even if older settings still point to C:\\Lazarus.*  ## 2) Outcome Added normalization in PathsSettingsViewModel to replace blank or legacy C:\\Lazarus values with LazarusPaths AppData defaults for Models, Cache, Temp, and Export. Values save back to settings on apply.  ## 3) Files Changed ```txt modified  src/App.Desktop/ViewModels/SettingsSections.cs ```  ## 4) Per-File Notes - src/App.Desktop/ViewModels/SettingsSections.cs NormalizePathToAppData() migrates to AppData for display and subsequent saves.  ## 5) Commands / Scripts Touched += 
- Build succeeded
- Paths page should now show AppData locations instead of C:\\Lazarus

## 7) Next Steps
1. If you have an existing settings.json with custom paths, these will not be overridden unless they were the legacy C:\\Lazarus locations.

## 8) Risks / Rollback
- **Risk:** Accidental migration for unrelated non-AppData paths containing 'Lazarus' in their name. **Mitigation:** We only migrate when not under AppData and path contains \\Lazarus\\ or starts with C:\\Lazarus\\.
