# Automation Report Safe cache clear in Global Settings

- **Date:** 2025-09-12 09:35
- **Agents:** codex
- **Branch:** main
- **Before SHA:** f8a0b8b3590d3e7f230b0013de74183129ad4bf1
- **After SHA:** uncommitted

## 1) Intent

Prevent UnauthorizedAccessException on "Clear Cache" by avoiding deletion of the running Orchestrator shadow copy and handling locked files safely.

## 2) Outcome

- Updated GlobalActionsViewModel to clear cache contents safely, excluding OrchestratorHost subfolder under %LOCALAPPDATA%/Lazarus/System-Data/Cache and ignoring locked binaries.
- Also updated "Clean Temp Files" to use the same safe behavior.

## 3) Files Changed

`	xt
modified  src/App.Desktop/ViewModels/GlobalActionsViewModel.cs
added     docs/automation-reports/20250912-0935-codex-safe-cache-clear.md
`

## 4) Per-File Notes

- src/App.Desktop/ViewModels/GlobalActionsViewModel.cs Add DeleteDirectoryContentsSafely helper; skip OrchestratorHost; best-effort deletes for files and directories; converted class to partial for helper separation.

## 5) Commands / Scripts Touched

`
None
`

## 6) Validation

- Build succeeded locally
- Clear Cache now avoids deleting files in use (e.g., Lazarus.Backend.dll) and logs a friendly message instead of throwing.

## 7) Next Steps

1. Optionally stop the orchestrator before clearing its shadow folder when the process is owned by Desktop.
2. Consider surfacing a toast confirming cache cleanup with counts of deleted items.

## 8) Risks / Rollback

- Risk: Some transient files might remain if locked by other processes. Mitigation: best-effort delete with retries or on next startup.
- Rollback: git revert <after_sha>.
