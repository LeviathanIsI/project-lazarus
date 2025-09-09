# Automation Report HUD: LoRA/Tokenizer/Embedding (like Model)

- **Date:** 2025-09-09 12:52
- **Agents:** codex
- **Branch:** main
- **Before SHA:** c6e82dd1ad18f8459ff8da40e96156ce0198e9a4
- **After SHA:** uncommitted

## 1) Intent

Show LoRA (with strength), Tokenizer, and Embedding in the titlebar HUD the same way as the Model name, with a default of "None Loaded" when missing.

## 2) Outcome

- Titlebar HUD now shows:
  - LoRA: <name @0.70> (or None Loaded)
  - Tokenizer: <name> (or None Loaded)
  - Embedding: <name> (or None Loaded)
- Keeps the existing Model: <name> display and uses the same label/value style.

## 3) Files Changed

`	xt
modified  src/App.Desktop/MainWindow.xaml
modified  src/App.Desktop/ViewModels/MainViewModel.cs
`

## 4) Per-File Notes

- MainWindow.xaml Added three label/value pairs with TargetNullValue= None Loaded.
- MainViewModel.cs Added LoadedLoraDisplay and expanded change notifications.

## 5) Commands / Scripts Touched

`
None
`

## 6) Validation

- Build succeeded locally.
- HUD updates as AppState changes.

## 7) Next Steps

- Option: Add tooltips with full file names for each attachment.

## 8) Risks / Rollback

- Low; purely UI. Revert commit if any regressions.
