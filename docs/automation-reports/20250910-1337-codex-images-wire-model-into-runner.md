# Automation Report Images: wire model into runner

- **Date:** 2025-09-10 13:37
- **Agents:** codex
- **Branch:** main
- **Before SHA:** 04738c7f89ded69609e8051062414bab855bccf5
- **After SHA:** uncommitted

## 1) Intent

Ensure the selected diffusion model is passed to the actual runner invocation and orchestrator load step during generation.

## 2) Outcome

- On Generate, we now: (1) preflight ping, (2) attempt orchestrator-runner LoadModelAsync(modelPath) with 3s timeout when available, (3) start generation. Fallback runner CLI already receives --model.

## 3) Files Changed
```txt
modified  src/App.Desktop/Views/ImagesView.xaml.cs
```

## 4) Per-File Notes
- src/App.Desktop/Views/ImagesView.xaml.cs Add model load call to IOrchestratorRunnerClient and log results.

## 5) Commands / Scripts Touched
```
None
```

## 6) Validation
- Build succeeded locally
- With orchestrator present, load step logs success/failure; fallback CLI includes --model path.

## 7) Next Steps
1. Thread model path into backend ImageService when it grows beyond placeholder.

## 8) Risks / Rollback
- Risk: Some runners may not accept load while busy; we continue gracefully. Rollback: revert this commit.
