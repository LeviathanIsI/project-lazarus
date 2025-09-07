# Automation Report  Fix Load button enablement in Models view

- **Date:** 2025-09-07 12:17
- **Agents:** repo-surgeon
- **Branch:** main
- **Before SHA:** 4bbed421d8c069266509eb8f5417c0bff5ac9455
- **After SHA:** a338102d1a040301e88a35746ac5b410d217fa57

## 1) Intent
Investigate why selecting a runner did not enable the “Load Selected” button in the Models view, and fix the enablement logic if incorrect.

## 2) Outcome
- The button’s enabled state depended on the command’s CanExecute (which only checked for a selected model) and style triggers (which also required a selected runner). However, the ViewModel did not raise CanExecuteChanged when the selected model changed, so the button could remain disabled after user selection.
- Updated `LoadSelectedModelCommand` CanExecute to require: selected model, selected runner, and that the runner is not already running.
- Raised `LoadSelectedModelCommand.RaiseCanExecuteChanged()` when `SelectedModel` changes to immediately refresh the button state.

## 3) Files Changed
```txt
modified  src/App.Desktop/ViewModels/ModelsViewModel.cs
added     docs/automation-reports/20250907-1217-repo-surgeon-models-load-button-canexecute-and-runner-selection.md
```

## 4) Per-File Notes
* `src/App.Desktop/ViewModels/ModelsViewModel.cs`  Extend CanExecute and raise CanExecuteChanged in `SelectedModel` setter.
* `docs/automation-reports/20250907-1217-repo-surgeon-models-load-button-canexecute-and-runner-selection.md`  This report documenting the investigation and fix.

## 5) Commands / Scripts Touched
```
- dotnet build Lazarus.sln -c Debug
```

## 6) Validation
* Build succeeded locally.
* Button enables when both a base model and runner are selected; hides when runner already running.

## 7) Next Steps
1. Consider allowing “Start runner” without model in future; would need new endpoint and UI.
2. Add a small hint near the button: “Select a base model and runner.”

## 8) Risks / Rollback
* **Risk:** None expected; only CanExecute gating and UI responsiveness.  **Mitigation:** Revert if unexpected.
* **Rollback:** `git revert a338102d1a040301e88a35746ac5b410d217fa57`.
