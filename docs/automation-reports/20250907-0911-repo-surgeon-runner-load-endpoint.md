# Automation Report  Add /runner/load (start or hot-swap)

- **Date:** 2025-09-07 09:11
- **Agents:** repo-surgeon
- **Branch:** main
- **Before SHA:** d61fe27630f5eb5ac54269effdbe346074bfd77f
- **After SHA:** uncommitted

## 1) Intent
Expose POST /runner/load { modelPath } to start a new runner when idle or hot-swap the model when a runner is active.

## 2) Outcome
- New endpoint validates modelPath, infers runner type (GGUF->LlamaCpp else VLLM), and either updates the existing in-memory runner or creates a new one.
- Returns { status:"ok", hotSwapped, runnerId, port, runnerType, modelId, modelPath }.
- Helper utilities centralized under HostHelpers.

## 3) Files Changed
`	xt
 M src/App.Orchestrator.Host/Program.cs
`

## 4) Per-File Notes
* src/App.Orchestrator.Host/Program.cs  Implemented /runner/load; refactored helpers into HostHelpers; updated references.

## 5) Commands / Scripts Touched
`
- dotnet build Lazarus.sln -c Debug
- dotnet run --project src/App.Orchestrator.Host -c Debug --no-build
`

## 6) Validation
* Build succeeded locally
* Example call with an existing file path returned 200 and payload with runner info

## 7) Next Steps
1. Replace stub with real process management: launch llama-server/llm and capture pid.
2. Attempt true hot-swap via runner-native reload endpoints when supported.

## 8) Risks / Rollback
* **Risk:** Port collision with actual runners  **Mitigation:** Track and reserve ports; probe availability.
* **Rollback:** git revert d61fe27630f5eb5ac54269effdbe346074bfd77f or revert the commit(s).
