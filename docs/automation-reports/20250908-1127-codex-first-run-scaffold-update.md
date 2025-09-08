# Automation Report First-run scaffolding adds Paths UI folders

- **Date:** 2025-09-08 11:27
- **Agents:** codex
- **Branch:** main
- **Before SHA:** 3a9167475d96b52fd1852907ff5d460c092a75e5
- **After SHA:** uncommitted

## 1) Intent
Make sure the first-run directory bootstrap creates all folders referenced by the Paths view defaults.

## 2) Outcome
Extended DirectoryBootstrap.LeafDirectories to include: Downloads (under Cache), Quantized (under Models), Conversations, Backups, Import/Export subfolders, Templates, and Plugins. App already calls DirectoryBootstrap.EnsureAll() on startup.

## 3) Files Changed
```txt
modified  src/App.Shared/DirectoryBootstrap.cs
```

## 4) Per-File Notes
- src/App.Shared/DirectoryBootstrap.cs Add missing leaf directories to mirror Paths defaults.
- src/App.Desktop/App.xaml.cs Already invokes DirectoryBootstrap.EnsureAll() before host/logging init.

## 5) Commands / Scripts Touched
`
None
`"
# Automation Report First-run scaffolding adds Paths UI folders  - **Date:** 2025-09-08 11:27 - **Agents:** codex - **Branch:** main - **Before SHA:** 3a9167475d96b52fd1852907ff5d460c092a75e5 - **After SHA:** uncommitted  ## 1) Intent Make sure the first-run directory bootstrap creates all folders referenced by the Paths view defaults.  ## 2) Outcome Extended DirectoryBootstrap.LeafDirectories to include: Downloads (under Cache), Quantized (under Models), Conversations, Backups, Import/Export subfolders, Templates, and Plugins. App already calls DirectoryBootstrap.EnsureAll() on startup.  ## 3) Files Changed ```txt modified  src/App.Shared/DirectoryBootstrap.cs ```  ## 4) Per-File Notes - src/App.Shared/DirectoryBootstrap.cs Add missing leaf directories to mirror Paths defaults. - src/App.Desktop/App.xaml.cs Already invokes DirectoryBootstrap.EnsureAll() before host/logging init.  ## 5) Commands / Scripts Touched += "
# Automation Report First-run scaffolding adds Paths UI folders  - **Date:** 2025-09-08 11:27 - **Agents:** codex - **Branch:** main - **Before SHA:** 3a9167475d96b52fd1852907ff5d460c092a75e5 - **After SHA:** uncommitted  ## 1) Intent Make sure the first-run directory bootstrap creates all folders referenced by the Paths view defaults.  ## 2) Outcome Extended DirectoryBootstrap.LeafDirectories to include: Downloads (under Cache), Quantized (under Models), Conversations, Backups, Import/Export subfolders, Templates, and Plugins. App already calls DirectoryBootstrap.EnsureAll() on startup.  ## 3) Files Changed ```txt modified  src/App.Shared/DirectoryBootstrap.cs ```  ## 4) Per-File Notes - src/App.Shared/DirectoryBootstrap.cs Add missing leaf directories to mirror Paths defaults. - src/App.Desktop/App.xaml.cs Already invokes DirectoryBootstrap.EnsureAll() before host/logging init.  ## 5) Commands / Scripts Touched += 
- Build succeeded
- Launching app on a clean machine will create the full tree under %LOCALAPPDATA%\\Lazarus

## 7) Next Steps
1. If additional folders get exposed in Paths later, add them to DirectoryBootstrap as well.

## 8) Risks / Rollback
- **Risk:** Creating extra empty folders is benign.
