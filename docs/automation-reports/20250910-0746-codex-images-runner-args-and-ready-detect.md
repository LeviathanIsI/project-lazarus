# Automation Report Add per-engine launch args + ready detection

- **Date:** 2025-09-10 07:46
- **Agents:** codex
- **Branch:** main
- **Before SHA:** 53db31b46eddd4a2e934500f92b92e3f35fe5bca
- **After SHA:** uncommitted

## 1) Intent

Mirror Models runner fidelity by adding per-engine launch arguments and detecting readiness/port from process output for Images engines.

## 2) Outcome

- Launch mapping by engine:
  - comfyui: adds --port <default 8188> and --listen 127.0.0.1 when possible.
  - sdwebui/stable-diffusion: appends --api.
  - invokeai: adds --host 127.0.0.1 [--port <default 9090>].
  - Supports .bat/.cmd, .exe, and .py entrypoints.
- Ready detection via stdout/err lines (Running on local URL, Uvicorn running on, pp started).
- Extracts port from URLs in output and updates RunnerPort.
- Optional override LAZARUS_IMAGE_RUNNER_PORT respected.

## 3) Files Changed

`	xt
modified  src/App.Desktop/Views/ImagesView.xaml.cs
`

## 4) Per-File Notes

- ImagesView.xaml.cs new helpers: GetDefaultPort, BuildLaunchCommand, OnRunnerOutputLine; integrated into process IO handlers.

## 5) Commands / Scripts Touched

`
N/A
`

## 6) Validation

- Build succeeded locally; smoke-checked output handlers and arg formation.

## 7) Next Steps

1. Allow per-engine args from settings UI if needed (advanced power users).
2. Surface discovered URL in UI (click-to-open) when present.

## 8) Risks / Rollback

- **Risk:** Some engine wrappers ignore CLI args; fall back still starts. **Rollback:** Revert this commit.

