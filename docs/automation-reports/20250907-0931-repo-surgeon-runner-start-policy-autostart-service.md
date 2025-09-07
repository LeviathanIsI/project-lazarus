# Automation Report  Runner start policy (auto-start service)

- **Date:** 2025-09-07 09:31
- **Agents:** repo-surgeon
- **Branch:** main
- **Before SHA:** 6639e8d2f85a3a04e3caa10ea79d6cf5cb0e3bfd
- **After SHA:** uncommitted

## 1) Intent
Define and implement a runner start policy: auto-start on host boot if Orchestrator.Runner.ModelPath is set (and AutoStart=true), otherwise remain idle; unload on host stop.

## 2) Outcome
- Added RunnerAutoStartService (hosted service) to start the runner on app start when configured, and unload on shutdown.
- Configured with Orchestrator.Runner.AutoStart (default true) and ModelPath (blank by default).
- Endpoints now reflect supervisor state.

## 3) Files Changed
`	xt
 M src/App.Orchestrator.Host/Program.cs
 M src/App.Orchestrator.Host/appsettings.json
`

## 4) Per-File Notes
* src/App.Orchestrator.Host/Program.cs  Register hosted service; implement RunnerAutoStartService.
* src/App.Orchestrator.Host/appsettings.json  Added AutoStart flag.

## 5) Commands / Scripts Touched
`
- dotnet build Lazarus.sln -c Debug
- dotnet run --project src/App.Orchestrator.Host -c Debug
`

## 6) Validation
* Build succeeded locally
* With empty ModelPath, orchestrator stays idle; with a model path set, it will attempt to start on boot.

## 7) Next Steps
1. Add restart-on-crash policy with bounded retries and backoff.
2. Expose current policy via /runner/policy and support runtime changes.

## 8) Risks / Rollback
* **Risk:** Auto-start unexpected on dev machines  **Mitigation:** Keep ModelPath empty by default; use AutoStart=false for dev.
* **Rollback:** git revert 6639e8d2f85a3a04e3caa10ea79d6cf5cb0e3bfd or revert this commit.
