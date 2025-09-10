# Automation Report Add Audio ViewModel + Wire AudioView

- **Date:** 2025-09-10 16:45
- **Agents:** codex
- **Branch:** main
- **Before SHA:** b1b8ca70f9aee98f014b8386d1111953db78b4e3
- **After SHA:** 6a299d3920a4c360f71dc4d2e2fa3f4ddf77b5d8

## 1) Intent

Add an Audio service + ViewModel and wire the existing AudioView buttons and stats. Enable import and stub generation with duration reading via NAudio.

## 2) Outcome

- Added IAudioService and a minimal AudioService that copies imports and generates a 2s silent WAV, reading durations with NAudio.
- Added AudioViewModel with commands and stats; bound existing AudioView buttons and stat fields.
- Registered the service (singleton) and ViewModel (transient) in DI.

## 3) Files Changed

`	xt
modified  src/App.Desktop/App.Desktop.csproj
modified  src/App.Desktop/Extensions/ServiceCollectionExtensions.cs
modified  src/App.Desktop/ViewModels/ChatSessionsViewModel.cs
modified  src/App.Desktop/Views/AudioView.xaml
modified  src/App.Desktop/Views/AudioView.xaml.cs
modified  src/App.Backend/bin2/
modified  src/App.Data/bin2/
modified  src/App.Desktop/Services/AudioService.cs
modified  src/App.Desktop/Services/IAudioService.cs
modified  src/App.Desktop/ViewModels/AudioViewModel.cs
modified  src/App.Desktop/bin2/
modified  src/App.Shared/bin2/
`

## 4) Per-File Notes

- src/App.Desktop/Services/IAudioService.cs Audio contracts for import/generate/stats.
- src/App.Desktop/Services/AudioService.cs Default implementation using %LOCALAPPDATA%/Lazarus/Audio via LazarusPaths.
- src/App.Desktop/ViewModels/AudioViewModel.cs Commands, stats, and simple row projection.
- src/App.Desktop/Extensions/ServiceCollectionExtensions.cs DI registrations for service + VM.
- src/App.Desktop/Views/AudioView.xaml Bind buttons and stat displays to VM.
- src/App.Desktop/Views/AudioView.xaml.cs Resolve AudioViewModel from DI as DataContext.
- src/App.Desktop/App.Desktop.csproj Add NAudio 2.2.1 for duration reading.

## 5) Commands / Scripts Touched

`
DI: AddLazarusCore() -> services.AddSingleton<IAudioService, AudioService>();
DI: AddLazarusViewModels() -> services.AddTransient<AudioViewModel>();
`

## 6) Validation

- Build succeeded locally
- Feature verified: Import/Generate commands bound; stats properties bound; synthesis status stub returns Ready
- Evidence: build output under src/App.Desktop/bin/Debug/net8.0-windows/

## 7) Next Steps

1. Replace stub generation with Piper-based synthesis and real readiness checks (owner: audio feature).
2. Add an ItemsControl list to show imported/generated items with context menu actions.

## 8) Risks / Rollback

- **Risk:** Unsupported audio formats may fail duration reading. **Mitigation:** Catch and report errors; prefer WAV for generation.
- **Rollback:** git revert <after_sha> or revert the commit introducing these changes.


