# Automation Report  Release host launch policy in Desktop

- **Date:** 2025-09-07 09:35
- **Agents:** repo-surgeon
- **Branch:** main
- **Before SHA:** a8064b60faebffa5c4dbee4525b412413352131a
- **After SHA:** uncommitted

## 1) Intent
In Release builds, start the orchestrator host by launching App.Orchestrator.Host.exe located next to the app or under \App.Orchestrator.Host\ if the host is not already running.

## 2) Outcome
- Updated OrchestratorProcessService to support both DEBUG (dotnet run) and RELEASE (launch exe) paths.
- In Release, it tries <AppContext.BaseDirectory>\App.Orchestrator.Host.exe, then <Base>\App.Orchestrator.Host\App.Orchestrator.Host.exe.
- Service is now registered for all configurations.

## 3) Files Changed
`	xt
 M src/App.Desktop/Extensions/ServiceCollectionExtensions.cs
 M src/App.Desktop/Services/OrchestratorProcessService.cs
`

## 4) Per-File Notes
* src/App.Desktop/Services/OrchestratorProcessService.cs  Add Release exe discovery + launch.
* src/App.Desktop/Extensions/ServiceCollectionExtensions.cs  Register hosted service without DEBUG guard.

## 5) Commands / Scripts Touched
`
- dotnet build Lazarus.sln -c Debug
`

## 6) Validation
* Build succeeded locally

## 7) Next Steps
1. Wire a Desktop setting to disable auto-start if desired in Release.
2. Add logging when exe not found to guide packaging.

## 8) Risks / Rollback
* **Risk:** Missing exe in deployed layout  **Mitigation:** Service logs a message and continues without starting.
* **Rollback:** git revert a8064b60faebffa5c4dbee4525b412413352131a or revert this commit.
