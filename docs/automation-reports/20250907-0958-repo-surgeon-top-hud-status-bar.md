# Automation Report  Move status HUD to top title bar

- **Date:** 2025-09-07 09:58
- **Agents:** repo-surgeon
- **Branch:** main
- **Before SHA:** 4659611f85c526f904d919cda5db6670b23f6ca4
- **After SHA:** uncommitted

## 1) Intent
Place the orchestrator/runner status HUD at the very top (inside the custom title bar), as requested.

## 2) Outcome
- Removed the bottom global status bar row.
- Added a centered HUD inside the title bar with red/yellow/green dots and tooltips, plus the loaded model name.

## 3) Files Changed
`	xt
 M src/App.Desktop/MainWindow.xaml
?? src/App.Orchestrator.Host/Properties/
`

## 4) Per-File Notes
* src/App.Desktop/MainWindow.xaml  Inserted HUD in row 0; removed the previous bottom status row.

## 5) Commands / Scripts Touched
`
- dotnet build Lazarus.sln -c Debug
`

## 6) Validation
* Build succeeded locally
* HUD sits at the top, centered; tooltips bind to MainViewModel

## 7) Next Steps
1. Adjust spacing/margins to match your final visual design.
2. Optionally make the title bar HUD left-aligned or right-aligned.

## 8) Risks / Rollback
* **Risk:** Title bar drag area reduced  **Mitigation:** HUD is compact; dragging still possible elsewhere in column.
* **Rollback:** git revert 4659611f85c526f904d919cda5db6670b23f6ca4 or revert the commit.
