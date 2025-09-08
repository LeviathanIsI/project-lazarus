# Automation Report Remove legacy --api flag from llama-server startup

- **Date:** 2025-09-08 12:31
- **Agents:** codex
- **Branch:** main
- **Before SHA:** 8e75935f510966dc004cb65e8fdd757e28756f2d
- **After SHA:** uncommitted

## 1) Intent
Fix runner startup failure: log shows 'error: invalid argument: --api'.

## 2) Outcome
Adjusted Program.cs to launch llama-server without --api for broad binary compatibility; other flags unchanged.

## 3) Files Changed
```txt
modified  src/App.Orchestrator.Host/Program.cs
```

## 4) Per-File Notes
- src/App.Orchestrator.Host/Program.cs Startup args now omit --api to match current llama-server binaries.

## 5) Validation
- Build succeeded
- Manual command already works; host should now match behavior

## 6) Next Steps
1. If some binaries require --api, we can add a config toggle to enable it per-user.

## 7) Risks / Rollback
- **Risk:** Older builds that require --api. **Mitigation:** configurable flag if needed.
