# Automation Report  Global status bar (orchestrator + runner)

- **Date:** 2025-09-07 09:52
- **Agents:** repo-surgeon
- **Branch:** main
- **Before SHA:** fc147a24dfcc043845b5d78b419b4ce8189c8209
- **After SHA:** uncommitted

## 1) Intent
Add a global status bar showing orchestrator and runner health with red/yellow/green dots and a tooltip, and display the currently loaded base model.

## 2) Outcome
- MainWindow gained a bottom status bar with:
  - Orchestrator dot: green when healthy, red when unreachable; tooltip text.
  - Runner dot: green when running, yellow when idle (and orchestrator healthy), red when orchestrator offline; tooltip text.
  - Loaded base model name if available.
- MainViewModel now polls runner status periodically and exposes IsRunnerRunning, LoadedModelName, and tooltip strings.

## 3) Files Changed
`	xt
 M src/App.Desktop/MainWindow.xaml
 M src/App.Desktop/ViewModels/MainViewModel.cs
`

## 4) Per-File Notes
* src/App.Desktop/MainWindow.xaml  Added Grid row + status bar UI with Ellipse indicators and tooltips.
* src/App.Desktop/ViewModels/MainViewModel.cs  Inject runner client; timer-based status refresh; new properties/tooltips.

## 5) Commands / Scripts Touched
`
- dotnet build Lazarus.sln -c Debug
`

## 6) Validation
* Build succeeded locally
* Status bar binds to existing health + new runner props

## 7) Next Steps
1. Consider throttling/pause polling when window is minimized.
2. Show latency/port details in tooltips.

## 8) Risks / Rollback
* **Risk:** Background polling overhead  **Mitigation:** 5s interval and lightweight call; can be made configurable.
* **Rollback:** git revert fc147a24dfcc043845b5d78b419b4ce8189c8209 or revert the commit.
