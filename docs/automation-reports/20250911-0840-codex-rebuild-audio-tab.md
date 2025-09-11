# Automation Report Rebuild Audio Tab (Design + Code)

- **Date:** 2025-09-11 08:40
- **Agents:** codex
- **Branch:** main
- **Before SHA:** 115012f5df9967334e72b9afd848583afe6fd8fe
- **After SHA:** uncommitted

## 1) Intent

Replace the Audio tab with a production-grade workspace scaffold: new layout, controls, contracts, and backend service stubs; ensure it renders with placeholders and builds cleanly.

## 2) Outcome

- Implemented new Audio layout (library + details + jobs + transport) using a local AudioTheme.\n- Added reusable controls: WaveformPreview (zoom/selection scaffolding) and AudioMeters.\n- Introduced Audio V2 contracts (AudioItem record, job/option types) under AudioV2 namespace to avoid conflicts.\n- Added Audio service interfaces and stub implementations (scan/import/transport/jobs).\n- Registered services in DI and ensured LazarusPaths provides Audio workspace dirs.\n- Preserved existing v1 AudioViewModel usage by aligning bindings; navigation now wires a VM via DI.

## 3) Files Changed

```txt
modified  src/App.Desktop/Extensions/ServiceCollectionExtensions.cs
modified  src/App.Desktop/ViewModels/NavigationViewModel.cs
added     src/App.Desktop/Styles/AudioTheme.xaml
added     src/App.Desktop/Controls/WaveformPreview.xaml
added     src/App.Desktop/Controls/WaveformPreview.xaml.cs
added     src/App.Desktop/Controls/AudioMeters.xaml
added     src/App.Desktop/Controls/AudioMeters.xaml.cs
replaced  src/App.Desktop/Views/Audio/AudioView.xaml
replaced  src/App.Desktop/Views/Audio/AudioView.xaml.cs
added     src/App.Desktop/Views/Audio/AudioView.xaml.backup
added     src/App.Backend/Services/Audio/Interfaces.cs
added     src/App.Backend/Services/Audio/Stubs.cs
modified  src/App.Shared/LazarusPaths.cs
added     src/App.Shared/Contracts/Audio/AudioContracts.cs
```

## 4) Per-File Notes

- AudioView.xaml New layout per spec; bound to existing VM property names to maintain functionality.
- AudioTheme.xaml Lightweight palette + button styles used only by AudioView.
- Controls WaveformPreview + AudioMeters scaffolded with DPs; minimal behavior to start.
- Interfaces/Stubs New Audio V2 service abstractions + no-op stubs; cross-platform safe (no System.Drawing).
- LazarusPaths Added Audio workspace paths + EnsureDirectories().
- NavigationViewModel Wires AudioViewModel via DI; safer creation.
- ServiceCollectionExtensions Registers V2 stubs alongside existing audio service.

## 5) Commands / Scripts Touched

```
dotnet build src/App.Desktop/App.Desktop.csproj -c Debug -p:OutDir=bin2/
```

## 6) Validation

- Desktop project builds cleanly.\n- Audio tab renders with the new layout; with no files, list shows empty (placeholder phase to follow).\n- Navigation switches content to Audio reliably (previous XAML errors resolved).

## 7) Next Steps

1. Implement placeholder mode (6 sample rows with dimmed UI) inside the existing VM, or migrate to V2 VM and map services.\n2. Wire WaveformPreview to actual cached PNG path via V2 IAudioLibrary.EnsureWaveformPreviewAsync.\n3. Add real job enqueueing via V2 services and surface in Jobs list.\n4. Output device + meters: bridge existing playback to Transport stub; add subscriptions in VM.

## 8) Risks / Rollback

- Risk: Some bindings are inert until VM migration to V2. Mitigation: iterate VM and adapters.\n- Rollback: `git revert <after_sha>` or revert files above.
