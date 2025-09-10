# Automation Report Audio + Chat per-view scope and designer

- **Date:** 2025-09-10 17:43
- **Agents:** codex
- **Branch:** main
- **Before SHA:** 4556493919948c45212091243ba710e120b1691d
- **After SHA:** uncommitted

## 1) Intent

Ensure each view gets its own ViewModel instance (prevent cross-tab bleed) and keep designer rendering without DI.

## 2) Outcome

- AudioView/ChatSessionsView now create a DI scope per view and dispose it on Unloaded; VMs remain transient in DI.
- Designer continues to render with design-time DataContext for AudioView.

## 3) Files Changed

`	xt
modified  src/App.Desktop/Views/AudioView.xaml.cs
modified  src/App.Desktop/Views/ChatSessionsView.xaml.cs
`

## 4) Per-File Notes

- AudioView.xaml.cs implements IDisposable, creates scope, resolves AudioViewModel, disposes on Unloaded.
- ChatSessionsView.xaml.cs same pattern for ChatSessionsViewModel.

## 5) Commands / Scripts Touched

`
Build: dotnet build Lazarus.sln -c Debug
`

## 6) Validation

- Build succeeded locally
- Audio/Chat tabs will each have isolated ViewModel instances at runtime
- Designer unaffected (AudioView shows sample stats)

## 7) Next Steps

1. Add ItemsControl list for Audio page items when ready.
2. Audit other views for singleton ViewModel usage and convert to transient + per-view scope if needed.

## 8) Risks / Rollback

- **Risk:** None expected; scopes dispose cleanly on Unloaded. **Mitigation:** Revert commit if any behavioral regression.
- **Rollback:** git revert <after_sha>.

