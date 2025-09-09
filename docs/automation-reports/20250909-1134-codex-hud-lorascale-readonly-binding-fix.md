# Automation Report HUD LoRA scale binding fix

- **Date:** 2025-09-09 11:34
- **Agents:** codex
- **Branch:** main
- **Before SHA:** 50778e2b52eb520c553eea9882ded76a128f42bb
- **After SHA:** uncommitted

## 1) Intent

Eliminate runtime exception: "A TwoWay or OneWayToSource binding cannot work on the read-only property 'LoraScale' of type 'MainViewModel'".

## 2) Outcome

- Forced the binding mode for Run.Text (HUD LoRA scale) to Mode=OneWay to prevent the binding engine from attempting to write back to the read-only property.

## 3) Files Changed

`	xt
modified  src/App.Desktop/MainWindow.xaml
`

## 4) Per-File Notes

- src/App.Desktop/MainWindow.xaml Run Text now binds with Mode=OneWay.

## 5) Commands / Scripts Touched

`
None
`

## 6) Validation

- Rebuilt solution successfully: dotnet build Lazarus.sln -c Debug.
- The specific exception should no longer surface on launch.

## 7) Next Steps

1. If you want the HUD to allow changing weight directly, bind the slider to ModelsViewModel.LoraScaleValue via a routed command/event; avoid writing to MainViewModel.

## 8) Risks / Rollback

- None; read-only view binding only.
- Rollback: revert the commit if needed.
