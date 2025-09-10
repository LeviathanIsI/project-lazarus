# Automation Report Images: bind VM + route Generate

- **Date:** 2025-09-10 14:05
- **Agents:** codex
- **Branch:** main
- **Before SHA:** 1f5ab39866562fac8dcc8bca73ad4a958790e042
- **After SHA:** uncommitted

## 1) Intent

Bind the runner and model selection to the ImagesViewModel and route the Generate button through the ViewModel strong DTO path.

## 2) Outcome

- Runner ComboBox now binds to ViewModelLocator.ImageLabViewModel.ImageRunners/SelectedRunner.
- Prompt/NegativePrompt and Model SelectedItem bind to VM.
- Generate click delegates to VM.GenerateCommand; view mirrors IsGenerating, ProcessingStatus, and PreviewImagePath.

## 3) Files Changed
```txt
modified  src/App.Desktop/Views/ImagesView.xaml
modified  src/App.Desktop/Views/ImagesView.xaml.cs
modified  src/App.Desktop/ViewModels/ImagesViewModel.cs
```

## 4) Per-File Notes
- ImagesView.xaml Bind to VM via ViewModelLocator for runner/prompt/model.
- ImagesView.xaml.cs Add VM hookup and property mirroring; route Generate to VM; keep Cancel as no-op until VM cancel is added.
- ImagesViewModel.cs Populate ImageRunners from registry at construction.

## 5) Commands / Scripts Touched
```
None
```

## 6) Validation
- Build succeeded locally
- Selecting a runner updates VM.SelectedRunner; Generate executes VM path and updates status/preview via property mirroring.

## 7) Next Steps
1. Implement VM cancellation and wire Cancel button to it.
2. Move more view-only state into VM over time.

## 8) Risks / Rollback
- Risk: Dual binding sources (view + VM) might drift; continue consolidating state to VM. Rollback: revert these three files.
