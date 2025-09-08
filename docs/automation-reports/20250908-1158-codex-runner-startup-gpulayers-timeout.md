# Automation Report Improve runner startup (GPU layers + timeout)

- **Date:** 2025-09-08 11:58
- **Agents:** codex
- **Branch:** main
- **Before SHA:** 74cfd23c6d2b8947d6087a804ee2548e99f65b39
- **After SHA:** uncommitted

## 1) Intent
Match manual launch that succeeds with a moderate n-gpu-layers and longer startup time.

## 2) Outcome
- Added ResolveGpuLayers(): reads Orchestrator:Runner:GpuLayers or env LAZARUS_RUNNER_GPU_LAYERS, defaults to 60 when GPU present; falls back to 0 on failure.\n- Increased default startup timeout to 4 minutes (and set 00:04:00 in appsettings).\n- Kept health polling and fallback to CPU-only.

## 3) Files Changed
```txt
modified  src/App.Orchestrator.Host/Program.cs
modified  src/App.Orchestrator.Host/appsettings.json
```

## 4) Per-File Notes
- src/App.Orchestrator.Host/Program.cs Use config/env GPU layers; default 60; extend default startup timeout.
- src/App.Orchestrator.Host/appsettings.json Add GpuLayers=60 and extend StartupTimeout to 00:04:00.

## 5) Validation
- Build succeeded
- Manual args and auto-launch now align (n-gpu-layers 60)

## 6) Next Steps
1. If still failing, tail %LOCALAPPDATA%\\Lazarus\\System-Data\\Logs\\llama-server-*.err.log to diagnose driver/VRAM issues.

## 7) Risks / Rollback
- **Risk:** Different GPUs need different offload; config/env allows tuning.
- **Rollback:** Revert Program.cs and appsettings.json edits.
