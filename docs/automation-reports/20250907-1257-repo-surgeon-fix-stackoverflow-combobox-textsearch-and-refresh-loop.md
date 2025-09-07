# Automation Report  Fix ComboBox selection refresh loop + TextSearch

- **Date:** 2025-09-07 12:57
- **Agents:** repo-surgeon
- **Branch:** main
- **Before SHA:** 97d9e9b26417fc3e921ae66be5909f85dc5e00b5
- **After SHA:** uncommitted

## 1) Intent
Resolve InsufficientExecutionStackException triggered by re-entrant ComboBox selection refresh and WPF TextSearch calling ToString repeatedly on BaseModelInfo.

## 2) Outcome
- Removed unnecessary VisibleBaseModels change notification from SelectedModel setter to avoid re-entrant ItemsSource refresh while selection is changing.
- Set TextSearch.TextPath to DisplayName for both Model and Runner ComboBoxes to prevent TextSearch from using ToString().

## 3) Files Changed
`	xt
modified  src/App.Desktop/ViewModels/ModelsViewModel.cs
modified  src/App.Desktop/Views/ModelsView.xaml
`

## 4) Per-File Notes
* ModelsViewModel.cs  Trimmed PropertyChanged notifications in SelectedModel.
* ModelsView.xaml  Enabled TextSearch and set TextSearch.TextPath on both ComboBoxes.

## 5) Commands / Scripts Touched
`
N/A
`

## 6) Validation
* Build succeeded locally.
* Expected: No more stack exhaustion during model selection; smoother dropdown behavior.

## 7) Next Steps
1. If you still see any re-entrancy, we can add a small _suppress guard around selection setters.
2. Optional: remove the stray TopK binding warning in XAML.

## 8) Risks / Rollback
* Low risk: cosmetic binding changes only; rollback by reverting these two files.