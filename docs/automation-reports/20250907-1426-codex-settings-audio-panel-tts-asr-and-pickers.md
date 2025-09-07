# Automation Report Settings: Audio panel (TTS/ASR + pickers)

- **Date:** 2025-09-07 14:26
- **Agents:** codex
- **Branch:** main
- **Before SHA:** 33104d2d21315031f74655e70d72231571c103e5
- **After SHA:** uncommitted

## 1) Intent

Add an Audio section to Settings (sidebar) to control TTS/ASR with executable pickers and voice setting.

## 2) Outcome

- New Audio panel: Enable TTS, Piper Executable (browse), Piper Voice, Enable ASR, Faster-Whisper Executable (browse).
- ViewModel binds to AppSettings.Audio and persists via SettingsService.

## 3) Files Changed

```txt
modified  src/App.Desktop/ViewModels/SettingsViewModel.cs
modified  src/App.Desktop/Views/SettingsView.xaml
```

## 4) Per-File Notes

- SettingsViewModel.cs Added Audio properties and Browse commands.
- SettingsView.xaml Added Audio panel with dark-mode tokens and pickers.

## 5) Commands / Scripts Touched

```
None
```

## 6) Validation

- Build succeeded locally

## 7) Next Steps

1. Optional: preset voice dropdown; detect installed Piper voices.
2. Optional: test TTS/ASR buttons to verify executables.

## 8) Risks / Rollback

- **Risk:** None; UI-only wiring to existing schema.
