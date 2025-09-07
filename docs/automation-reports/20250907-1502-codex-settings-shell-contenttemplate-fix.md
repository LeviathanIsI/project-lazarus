# Automation Report Settings shell ContentTemplate binding fix

- **Date:** 2025-09-07 15:02
- **Agents:** codex
- **Branch:** main
- **Before SHA:** f92146de030f77223d8895900f3438ab1061d3ec
- **After SHA:** uncommitted

## 1) Intent

Ensure sub-views render in the shell by templating SelectedSection into its View.

## 2) Outcome

- Replaced Content="{Binding SelectedSection.View}" with Content="{Binding SelectedSection}" + ContentTemplate (ContentPresenter -> View).
- Added vm namespace to shell and kept dark-mode tokens.

## 3) Files Changed

```txt
modified  src/App.Desktop/Views/SettingsShell.xaml
```

## 4) Per-File Notes

- More robust binding path; WPF reliably re-templates on SelectedSection change.

## 5) Commands / Scripts Touched

```
None
```

## 6) Validation

- Build succeeded locally
- Selecting a section swaps in its view

## 7) Next Steps

1. Add visual selection styling for sidebar (optional).

## 8) Risks / Rollback

- **Risk:** None.
