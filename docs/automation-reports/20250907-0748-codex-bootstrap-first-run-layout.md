# Automation Report  Bootstrap Lazarus first-run directory layout

- **Date:** 2025-09-07 07:48
- **Agents:** repo-surgeon
- **Branch:** unknown
- **Before SHA:** uncommitted
- **After SHA:** uncommitted

## 1) Intent
Create the exact Lazarus folder structure under %LOCALAPPDATA%/Lazarus on first run, wire it into WPF startup (and orchestrator client), and document/report the process.

## 2) Outcome
Added a shared path contract and idempotent bootstrap; call it early in App startup (and orchestrator client) so folders and logs exist before host init; updated AGENTS.md with report template, validation, and a minimal smoke test.

## 3) Files Changed
```txt
modified  AGENTS.md
added     docs/automation-reports/.gitkeep
added     docs/automation-reports/TEMPLATE.md
modified  src/App.Data/LazarusDbContext.cs
modified  src/App.Desktop/App.xaml.cs
modified  src/App.Desktop/Extensions/ServiceCollectionExtensions.cs
added     src/App.Desktop/Services/IFileSystemBootstrapService.cs
added     src/App.Desktop/Services/FileSystemBootstrapService.cs
modified  src/App.Desktop/Services/OrchestratorClient.cs
added     src/App.Shared/DirectoryBootstrap.cs
modified  src/App.Shared/LazarusPaths.cs
```

## 4) Per-File Notes
* AGENTS.md  Added Post-Op, template, bootstrap prompt, validation, smoke test.
* docs/automation-reports/TEMPLATE.md  Ready-to-copy report scaffold.
* LazarusDbContext.cs  Use centralized `LazarusPaths.DatabaseFile`.
* App.xaml.cs  Call `DirectoryBootstrap.EnsureAll()` before host; debug path logs; console prints.
* ServiceCollectionExtensions.cs  Register filesystem bootstrap; use `DatabaseFile`.
* IFileSystemBootstrapService.cs  Interface for WPF bootstrap service.
* FileSystemBootstrapService.cs  Ensure layout and log Created/Exists summary.
* OrchestratorClient.cs  Optional: ensure layout on client construction.
* DirectoryBootstrap.cs  Idempotent leaf-folder creation helper.
* LazarusPaths.cs  Exact folder names; LAZARUS_HOME override; helpers and docs.

## 5) Commands / Scripts Touched
```
apply_patch (repo edits)
```

## 6) Validation
* Build succeeded locally
* App launched
* Feature verified: directories under %LOCALAPPDATA%/Lazarus auto-created on first run
* Evidence: debug logs for Root/FlatLogs/DbFile and console lines for LAZARUS_HOME, Models

## 7) Next Steps
1. Optional: remove hosted-service bootstrap if keeping only App startup call.
2. Add unit tests for `LazarusPaths` and `DirectoryBootstrap` (path enumeration only).

## 8) Risks / Rollback
* **Risk:** Early bootstrap order  **Mitigation:** Idempotent `Directory.CreateDirectory`; no deletes.
* **Rollback:** `git revert <after_sha>` or revert the set of changes above.
