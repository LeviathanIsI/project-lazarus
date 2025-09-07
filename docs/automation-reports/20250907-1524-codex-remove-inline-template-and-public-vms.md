# Automation Report Settings shell: remove inline ContentTemplate + public VMs

- **Date:** 2025-09-07 15:24
- **Agents:** codex
- **Branch:** main
- **Before SHA:** a55c349d4cfae13f41a5660edc6f91afaaa2e64e
- **After SHA:** uncommitted

## 1) Intent

Ensure DataTemplates apply by removing the inline ContentTemplate and making section VMs public so XAML can resolve types.

## 2) Outcome

- SettingsShell.xaml: removed ContentTemplate from ContentControl; now Content binds only to SelectedSectionVm.
- SettingsSections.cs: changed section VMs from internal to public so DataTemplates match.

## 3) Files Changed

```txt
modified  src/App.Desktop/Views/SettingsShell.xaml
modified  src/App.Desktop/ViewModels/SettingsSections.cs
```

## 4) Per-File Notes

- WPF DataTemplate lookup requires public types for x:Type references in XAML; making VMs public ensures the mapping resolves.

## 5) Validation

- Build succeeded locally; content now appears via DataTemplates.

## 6) Risks / Rollback

- **Risk:** None; removal of inline template restores standard WPF behavior.
