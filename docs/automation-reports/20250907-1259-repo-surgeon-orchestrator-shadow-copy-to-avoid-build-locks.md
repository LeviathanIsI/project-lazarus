# Automation Report  Run orchestrator from shadow copy to avoid build locks

- **Date:** 2025-09-07 12:59
- **Agents:** repo-surgeon
- **Branch:** main
- **Before SHA:** 5e922c8284de266078e36d483fa91012078b714b
- **After SHA:** uncommitted

## 1) Intent
Prevent App.Orchestrator.Host from locking its own bin output during development builds (MSB3021/MSB3027) when Desktop auto-starts it.

## 2) Outcome
- In DEBUG, Desktop now shadow-copies the orchestrator build output to %LOCALAPPDATA%/Lazarus/System-Data/Cache/OrchestratorHost and runs dotnet <shadow>/Lazarus.Orchestrator.Host.dll from there.
- If shadow copy fails, falls back to dotnet run.

## 3) Files Changed
`	xt
modified  src/App.Desktop/Services/OrchestratorProcessService.cs
`

## 4) Per-File Notes
* OrchestratorProcessService.cs  MirrorDirectory helper; changed DEBUG startup to prefer shadow-run, avoiding locks on src/App.Orchestrator.Host/bin.

## 5) Commands / Scripts Touched
`
N/A
`

## 6) Validation
* Build succeeded locally with Desktop running; no file lock errors.

## 7) Next Steps
1. Optionally add a small UI button to "Restart Orchestrator" that stops and restarts from the shadow copy.

## 8) Risks / Rollback
* Risk: Shadow copy might get stale if not rebuilt; mitigation: Desktop uses existing compiled output; typical dev workflow rebuilds before running Desktop.
* Rollback: revert changes in OrchestratorProcessService.cs.