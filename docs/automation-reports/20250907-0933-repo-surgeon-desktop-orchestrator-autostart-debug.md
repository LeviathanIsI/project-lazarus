# Automation Report  Desktop auto-starts orchestrator in Debug

- **Date:** 2025-09-07 09:33
- **Agents:** repo-surgeon
- **Branch:** main
- **Before SHA:** c2c3233aecb9d8eae3a38f01e505f714f8ad98c7
- **After SHA:** uncommitted

## 1) Intent
Have the WPF app auto-start the local orchestrator host in Debug builds if it's not already running.

## 2) Outcome
- Added OrchestratorProcessService (Debug-only hosted service) to start dotnet run --project src/App.Orchestrator.Host -c Debug when needed.
- Registered it under AddLazarusBackgroundServices() within #if DEBUG.
- Validates orchestrator health at http://127.0.0.1:11711/health before and after launch.

## 3) Files Changed
`	xt
 M src/App.Desktop/Extensions/ServiceCollectionExtensions.cs
 M src/App.Orchestrator.Host/Program.cs
?? src/App.Desktop/Services/OrchestratorProcessService.cs
`

## 4) Per-File Notes
* src/App.Desktop/Services/OrchestratorProcessService.cs  Process launcher with health checks and graceful stop.
* src/App.Desktop/Extensions/ServiceCollectionExtensions.cs  Adds the hosted service in Debug.

## 5) Commands / Scripts Touched
`
- dotnet build Lazarus.sln -c Debug
- dotnet run --project src/App.Desktop -c Debug
`

## 6) Validation
* Build succeeded locally

## 7) Next Steps
1. Add an option to disable auto-start via Desktop config if needed.
2. Use LAZARUS_REPO_ROOT to locate the repo in more environments.

## 8) Risks / Rollback
* **Risk:** Multiple orchestrator instances in dev  **Mitigation:** Checks health before starting; stops process on Desktop shutdown.
* **Rollback:** git revert c2c3233aecb9d8eae3a38f01e505f714f8ad98c7 or revert the commit.
