# Automation Report Settings UI: sidebar nav, browse pickers, advanced expander

- **Date:** 2025-09-07 14:20
- **Agents:** codex
- **Branch:** main
- **Before SHA:** 02cf0822b0c63ad3d2ec7a8e6656569a129de2a2
- **After SHA:** uncommitted

## 1) Intent

Move from tabs to sidebar categories; add executable/folder pickers; include Advanced expander; keep dark-only.

## 2) Outcome

- SettingsView now has a left sidebar (General, Paths, Orchestrator, Runner).
- Browse buttons: llama-server executable (OpenFileDialog) and folder pickers via OpenFileDialog trick.
- Advanced expander for runner args; tooltips for URL/port.
- Dark tokens applied; no theme selector.

## 3) Files Changed

```txt
modified  src/App.Desktop/Views/SettingsView.xaml
modified  src/App.Desktop/ViewModels/SettingsViewModel.cs
modified  src/App.Desktop/App.Desktop.csproj
```

## 4) Per-File Notes

- SettingsView.xaml Replaced TabControl with ListBox+panels; added Browse buttons and Advanced expander.
- SettingsViewModel.cs Added categories, browse commands; used OpenFileDialog for directory selection.
- App.Desktop.csproj: kept WPF-only; avoided WinForms to prevent ambiguous types.

## 5) Commands / Scripts Touched

```
UI interactions:
- BrowseLlamaServerCommand -> OpenFileDialog
- BrowseModelsDirectoryCommand / BrowseCacheDirectoryCommand -> OpenFileDialog (folder trick)
```

## 6) Validation

- Build succeeded locally
- Sidebar switches sections; browse buttons update fields

## 7) Next Steps

1. Add sampling sliders (temperature, top-p) once schema lands.
2. Add orchestrator health indicator and ping button.

## 8) Risks / Rollback

- **Risk:** Folder selection workaround is less polished; replace with Win32 folder dialog later.
