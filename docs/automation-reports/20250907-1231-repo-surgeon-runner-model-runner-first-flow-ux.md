# Automation Report  Runner-first runner/model flow (filtering + UX)

- **Date:** 2025-09-07 12:31
- **Agents:** repo-surgeon
- **Branch:** main
- **Before SHA:** a8a6fe0f948501807c039c2f678d4fd9fe3b3786
- **After SHA:** uncommitted

## 1) Intent
Implement the recommended runner-first flow in Models: filter Base Models by the selected runner, pre-filter runners by selected model, gate the Load button on runner+model+orchestrator health, and add inline UX hints and compatibility badges.

## 2) Outcome
- Added compatibility filtering for models and runners.
- Load enablement now requires runner, model, orchestrator healthy, and runner not already running.
- Added inline reason text when Load is disabled, plus hint when a model is selected first.
- Badged model items with format and recommended engine (e.g., GGUF • llama.cpp).
- Kept runner picker hidden while a runner is active.

## 3) Files Changed
`	xt
modified  src/App.Desktop/ViewModels/ModelsViewModel.cs
modified  src/App.Desktop/Views/ModelsView.xaml
added     src/App.Desktop/Converters/RunnerKindToStringConverter.cs
`

## 4) Per-File Notes
* src/App.Desktop/ViewModels/ModelsViewModel.cs  Added compatibility filtering, orchestrator health gating, and disabled-reason logic.
* src/App.Desktop/Views/ModelsView.xaml  Bound to filtered sources, added badges, hints, and simplified triggers.
* src/App.Desktop/Converters/RunnerKindToStringConverter.cs  Maps RunnerKind to friendly labels (llama.cpp, vLLM, exllamav2).

## 5) Commands / Scripts Touched
`
N/A
`

## 6) Validation
* Build succeeded locally
* Feature verified: models filter by runner; runners filter by model; Load disables when orchestrator offline/runner missing/model missing; runner picker hides when loaded.
* Evidence: build output under src/App.Desktop/bin/Debug/net8.0-windows/

## 7) Next Steps
1. When vLLM/exllamav2 backends are functional, extend LoadSelectedModelAsync beyond llama.cpp.
2. Consider keyboard focus jump to runner dropdown when model picked first (optional polish).

## 8) Risks / Rollback
* Risk: Enum mapping may not cover all future engines or formats. Mitigation: centralize compatibility matrix in shared layer if it grows.
* Rollback: git revert uncommitted (once committed, git revert <after_sha>).