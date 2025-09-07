# Automation Report DI singleton for orchestrator clients

- **Date:** 2025-09-07 12:36
- **Agents:** codex
- **Branch:** main
- **Before SHA:** b48cbef8780f5270137081b970368b98bf243dfd
- **After SHA:** uncommitted

## 1) Intent

Resolve inconsistent health indicators by ensuring a single shared instance of IOrchestratorClient/IOrchestratorRunnerClient is used across viewmodels.

## 2) Outcome

Switched typed HttpClient registrations to create a single singleton instance per client and map the interfaces to that singleton. This prevents ModelsViewModel from seeing IsHealthy=false while MainViewModel shows rue.

## 3) Files Changed

`	xt
modified  src/App.Desktop/Extensions/ServiceCollectionExtensions.cs
`

## 4) Per-File Notes

- src/App.Desktop/Extensions/ServiceCollectionExtensions.cs Register typed clients (OrchestratorClient, OrchestratorRunnerClient) then bind interfaces as singletons to those instances.

## 5) Commands / Scripts Touched

`N/A`

## 6) Validation

- Build succeeded locally.
- Expected result: Runner section no longer shows "Orchestrator offline" when top HUD is green.

## 7) Next Steps

1. If multiple windows/pages need the same runner state events, this singleton approach keeps them in sync.
2. If we add reconnect logic, ensure state events still propagate correctly.

## 8) Risks / Rollback

- Risk: Singletons live for app lifetime; dispose on shutdown is fine.
- Rollback: git revert <after_sha> or back out the DI change.
