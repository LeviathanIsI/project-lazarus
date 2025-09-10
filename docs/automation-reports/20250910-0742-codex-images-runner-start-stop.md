# Automation Report Wire Load/Unload to start/stop image engines

- **Date:** 2025-09-10 07:42
- **Agents:** codex
- **Branch:** main
- **Before SHA:** e147dc95f61f9f96e3b096d7d2875d8e759a705a
- **After SHA:** uncommitted

## 1) Intent

Implement functional Load/Unload for Images runner so users can switch engines without restarting, until full orchestration is available.

## 2) Outcome

- Load Selected now starts the selected image engine entrypoint (bat/exe/py) in the engine folder.
- Logs are redirected to %LOCALAPPDATA%/Lazarus/System-Data/Logs/image-runner-*.out.log and *.err.log.
- Unload gracefully closes and then kills the process if needed.
- UI status (running, pid, exe path, logs) updates live.

## 3) Files Changed

`	xt
modified  src/App.Desktop/Views/ImagesView.xaml.cs
`

## 4) Per-File Notes

- ImagesView.xaml.cs Added process management (StartImageRunnerAsync, StopImageRunnerAsync) and hooked to commands. Updates diagnostics bindings.

## 5) Commands / Scripts Touched

`
N/A (desktop-only process management)
`

## 6) Validation

- Build succeeded.
- Manual check: selecting a runner and clicking Load Selected spawns the engine script; Unload stops it; logs written under System-Data/Logs.

## 7) Next Steps

1. Replace local process management with orchestrator endpoints once image orchestration is ready.
2. Extend engine pattern detection if your packagers use different launchers.

## 8) Risks / Rollback

- **Risk:** Launching external scripts may vary per environment. **Mitigation:** conservative patterns, logging, and safe kill.
- **Rollback:** Revert this commit.

