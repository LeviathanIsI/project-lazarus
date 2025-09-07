# Automation Report Settings sidebar structure update

- **Date:** 2025-09-07 14:24
- **Agents:** codex
- **Branch:** main
- **Before SHA:** 5275ad8ea5ce5492afd3f6717bfcdeaddfc666ff
- **After SHA:** uncommitted

## 1) Intent

Set sidebar categories to General, Paths, Orchestrator, Runners, Models and add a Models panel.

## 2) Outcome

- Categories updated; Runner -> Runners, and Models added.
- Models panel includes Active Model field with Browse picker; settings persist via service.

## 3) Files Changed

```txt
modified  src/App.Desktop/ViewModels/SettingsViewModel.cs
modified  src/App.Desktop/Views/SettingsView.xaml
```

## 4) Per-File Notes

- SettingsViewModel.cs Added categories, ActiveModelId property, BrowseActiveModelCommand.
- SettingsView.xaml DataTriggers updated to Runners; new Models panel.

## 5) Commands / Scripts Touched

```
None
```

## 6) Validation

- Build succeeded locally

## 7) Next Steps

1. Populate Models panel with inventory list and selection if desired.

## 8) Risks / Rollback

- **Risk:** Minimal; UI-only structural change.
