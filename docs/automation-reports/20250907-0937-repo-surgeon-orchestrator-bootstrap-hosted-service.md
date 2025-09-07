# Automation Report  Add OrchestratorBootstrapHostedService

- **Date:** 2025-09-07 09:37
- **Agents:** repo-surgeon
- **Branch:** main
- **Before SHA:** da0ba13358bebcb86ca8a95b81d8f3335247a0c7
- **After SHA:** uncommitted

## 1) Intent
Introduce OrchestratorBootstrapHostedService to orchestrate starting/stopping the orchestrator process on Desktop app startup/shutdown by calling the process service.

## 2) Outcome
- Added IOrchestratorProcessService abstraction and refactored OrchestratorProcessService to implement it (no longer an IHostedService).
- Registered OrchestratorBootstrapHostedService (IHostedService) which delegates to the process service.
- Service registration updated accordingly.

## 3) Files Changed
`	xt
 M src/App.Desktop/Extensions/ServiceCollectionExtensions.cs
 M src/App.Desktop/Services/OrchestratorProcessService.cs
?? src/App.Desktop/Services/OrchestratorBootstrapHostedService.cs
`

## 4) Per-File Notes
* src/App.Desktop/Services/OrchestratorProcessService.cs  Refactor to interface; exposes StartIfNeededAsync/StopIfOwnedAsync.
* src/App.Desktop/Services/OrchestratorBootstrapHostedService.cs  New hosted service that calls the process service.
* src/App.Desktop/Extensions/ServiceCollectionExtensions.cs  Register singleton process service and hosted bootstrapper.

## 5) Commands / Scripts Touched
`
- dotnet build Lazarus.sln -c Debug
`

## 6) Validation
* Build succeeded locally

## 7) Next Steps
1. Add config flags to disable auto-start in Release if desired.

## 8) Risks / Rollback
* **Risk:** Overlapping starters  **Mitigation:** Single hosted bootstrapper manages the process service now.
* **Rollback:** git revert da0ba13358bebcb86ca8a95b81d8f3335247a0c7 or revert the commit.
