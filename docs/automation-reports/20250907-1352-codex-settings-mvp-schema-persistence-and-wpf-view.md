# Automation Report Settings MVP: schema, persistence, WPF view

- **Date:** 2025-09-07 13:52
- **Agents:** codex
- **Branch:** main
- **Before SHA:** 40a375152cc1e81e6156b4447c6b9e37d2ba7416
- **After SHA:** uncommitted

## 1) Intent

Add a versioned, strongly typed settings model persisted to %LOCALAPPDATA%/Lazarus/settings.json, a JSON-backed service with safe writes + autosave debounce, and a WPF Settings view with import/export.

## 2) Outcome

- Implemented AppSettings (SchemaVersion=1) per MVP with General, Paths, Orchestrator, and Runner (llama.cpp) options.
- Added SettingsService (load/save/import/export, safe writes, debounced SaveSoon).
- Created SettingsView + SettingsViewModel (tabs: General, Paths, Orchestrator, Llama.cpp); wired DI and navigation route ("Settings").

## 3) Files Changed

```txt
modified  src/App.Backend/App.Backend.csproj
modified  src/App.Desktop/Extensions/ServiceCollectionExtensions.cs
modified  src/App.Desktop/ViewModels/NavigationViewModel.cs
modified  src/App.Backend/Services/Settings/
modified  src/App.Desktop/Services/UiDebounceDispatcher.cs
modified  src/App.Desktop/ViewModels/SettingsViewModel.cs
modified  src/App.Desktop/Views/SettingsView.xaml
modified  src/App.Desktop/Views/SettingsView.xaml.cs
modified  src/App.Shared/Settings/
```

## 4) Per-File Notes

- src/App.Shared/Settings/SettingsSchema.cs Flat MVP schema, LlamaCppSettings; SchemaVersion=1.
- src/App.Shared/Settings/SettingsPaths.cs Paths for settings.json and temp file.
- src/App.Shared/Settings/ISettingsService.cs Contract for settings load/save and events.
- src/App.Backend/Services/Settings/SettingsService.cs JSON persistence, safe writes, debounce.
- src/App.Desktop/ViewModels/SettingsViewModel.cs Binds to AppSettings; Save/Reset/Import/Export.
- src/App.Desktop/Views/SettingsView.xaml(.cs) Basic tabbed UI; DI-backed DataContext.
- src/App.Desktop/Services/UiDebounceDispatcher.cs UI debounce helper (not strictly required in MVP).
- src/App.Desktop/Extensions/ServiceCollectionExtensions.cs Registers SettingsService; loads on startup.
- src/App.Desktop/ViewModels/NavigationViewModel.cs Adds "Settings" route.

## 5) Commands / Scripts Touched

```
Persistence:
- %LOCALAPPDATA%/Lazarus/settings.json (atomic write via .tmp -> replace)
Config keys (mirrored in UI):
- PreferredTheme, Language, ModelsDirectory, CacheDirectory
- OrchestratorBaseUrl, OrchestratorStartupTimeoutSec
- ActiveRunner, LlamaCpp.DefaultPort, LlamaCpp.StartupTimeoutSec, LlamaCpp.BinaryDir
Navigation:
- NavigateTo("Settings") shows the new Settings view
```

## 6) Validation

- Build succeeded locally
- SettingsService creates defaults and saves to settings.json
- SettingsView edits update service; debounced save

## 7) Next Steps

1. Wire the Settings entry in the sidebar/menu to NavigateTo("Settings").
2. Optionally sync selected settings to orchestrator host appsettings.

## 8) Risks / Rollback

- **Risk:** Schema evolution needs migrations. **Mitigation:** version field + UpgradeIfNeeded placeholder.
- **Rollback:** `git revert <after_sha>` or revert the commit(s).
