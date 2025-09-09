# Automation Report Fix Generate Button Binding

- **Date:** 2025-09-09 16:04
- **Agents:** codex
- **Branch:** main
- **Before SHA:** deaca17dd5c333298a8e1d7019abb201436ba860
- **After SHA:** uncommitted

## Intent
Resolve WPF error: TwoWay binding on read-only GenerateButtonText.

## Outcome
Changed Run binding to Mode=OneWay in ImagesView.xaml.

## Files Changed
```txt
modified  src/App.Desktop/Views/ImagesView.xaml
```

## Validation
- Build succeeded; UI loads without binding error.
