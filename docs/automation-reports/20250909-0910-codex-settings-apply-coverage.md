# Automation Report Ensure all settings save on Apply

- **Date:** 2025-09-09 09:10
- **Agents:** codex
- **Branch:** main
- **Before SHA:** 845058a25897bd0c5b6199424325b9ea234db5d3
- **After SHA:** uncommitted

## 1) Intent

Audit settings coverage and wire any missing properties so that every setting shown in the Settings UI is persisted to settings.json when the user clicks Apply.

## 2) Outcome

- Added missing general UI properties to shared AppSettings: Language, UiTheme, StartWithWindows, StartMinimized, RestoreLastSession, AutoLoadModel, AutoSaveIntervalMinutes, HistoryLimit, AutoDownloadUpdates, SendAnonymousUsage, SendCrashReports.
- Extended GeneralSettingsViewModel to surface/bind these properties; wired RefreshFromSettings, ApplySettingsAsync, and defaults.
- Confirmed existing sections already map all their AppSettings-backed fields; no-op for non-persisted display-only fields.

## 3) Files Changed

`	xt
src/App.Desktop/Extensions/ServiceCollectionExtensions.cs
src/App.Desktop/Services/ChatService.cs
src/App.Desktop/ViewModels/ModelsViewModel.cs
src/App.Desktop/ViewModels/NavigationViewModel.cs
src/App.Desktop/ViewModels/SettingsSections.cs
src/App.Desktop/ViewModels/ViewModelLocator.cs
src/App.Shared/Settings/SettingsSchema.cs
src/App.Desktop/Services/ChatSessionService.cs
src/App.Desktop/Services/RunnerStatusProvider.cs
`

## 4) Per-File Notes

- src/App.Shared/Settings/SettingsSchema.cs Added backing fields and JSON-annotated properties.
- src/App.Desktop/ViewModels/SettingsSections.cs (General) Added bindings + applied mapping and defaults for all new fields.

## 5) Commands / Scripts Touched

`
None
`

## 6) Validation

- Build clean: dotnet build Lazarus.sln -c Debug.
- Manual: change each General setting, click Apply, inspect %LOCALAPPDATA%/Lazarus/System-Data/Configuration/settings.json to confirm fields update.

## 7) Next Steps

1. Optionally add persistence for additional runner tuning fields (CpuBatchSize, UseBlas, etc.) by extending AppSettings if you want those serialized too.
2. Wire any future UI controls by adding corresponding AppSettings properties and mapping in their section.

## 8) Risks / Rollback

- **Risk:** Newly added fields are not yet consumed by services; they are safe to persist and can be adopted later.
- **Rollback:** git revert <after_sha> or back out this commit.
