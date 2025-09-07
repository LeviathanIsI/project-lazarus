# Automation Report LlamaCpp supervisor GPU fallback + better diagnostics

- **Date:** 2025-09-07 12:42
- **Agents:** codex
- **Branch:** main
- **Before SHA:** e7908ace0c472f8a97604e3ea03d9f8ab8c9deb4
- **After SHA:** uncommitted

## 1) Intent

Reduce runner start failures by detecting GPU availability and retrying llama-server with CPU-only if the GPU start fails; also log launch command and early-exit when process dies during startup.

## 2) Outcome

- LlamaCppSupervisor.LoadAsync now tries GPU offload (n-gpu-layers=999) when CUDA seems present; otherwise starts with CPU-only.
- If the GPU attempt fails to become healthy or process exits, it falls back to CPU-only and logs a warning.
- Added early process-exited check and logged exit code for clarity.
- Logs now include exe path and full arguments to aid triage.

## 3) Files Changed

`	xt
modified  src/App.Orchestrator.Host/Program.cs
`

## 4) Per-File Notes

- src/App.Orchestrator.Host/Program.cs Refactored start logic into StartOnceAsync, added HasCuda() probe, fallback retry path, and richer logging.

## 5) Commands / Scripts Touched

`N/A`

## 6) Validation

- Build succeeded locally.
- Expected behavior: If CUDA is missing or incompatible, runner retries with CPU-only and should become healthy for GGUF models.
- Evidence: See logs under %LOCALAPPDATA%/Lazarus/System-Data/Logs/llama-server-\*.{out,err}.log and orchestrator log.

## 7) Next Steps

1. Surface supervisor error details back through /runner/load so UI can show specifics (port in use, missing DLL, etc.).
2. Make port configurable and probe for a free port to avoid collisions.

## 8) Risks / Rollback

- Risk: Fallback adds a second start attempt; negligible overhead and only on failure.
- Rollback: revert the changes in Program.cs.
