# Automation Report Training paths + DTO + trainer plan scaffolding

- **Date:** 2025-09-12 18:33
- **Agents:** codex
- **Branch:** main
- **Before SHA:** 739d145e4ad7251bc6386280aca33584a9e01f82
- **After SHA:** uncommitted

## 1) Intent

Add System-Data/Training directory contract to LazarusPaths, define a unified TrainingProfile schema, and scaffold trainer planning/orchestration (plan files + manifests) without executing trainers yet.

## 2) Outcome

- LazarusPaths: adds System-Data/Training with Datasets/Conversations|Preferences|Eval, Jobs, and Outputs/Adapters; DirectoryBootstrap updated.
- Shared DTOs: TrainingProfile and nested specs (datasets, optimization, schedule, batching, eval, hardware) with JSON annotations.
- Backend: ITrainer and TrainerPlan types; planners for LLaMA-Factory, Axolotl, Unsloth that emit config files (JSON/YAML) and a TrainingJobOrchestrator to create Jobs/<id> workspaces and write manifest.json.

## 3) Files Changed

`	xt
M src/App.Backend/Services/ConversationTrainingService.cs
M src/App.Desktop/Resources/Themes/DarkTheme.xaml
D src/App.Desktop/Services/ISystemMetricsService.cs
D src/App.Desktop/Services/SystemMetricsService.cs
M src/App.Desktop/ViewModels/Training/ConversationsDesignerViewModel.cs
M src/App.Desktop/ViewModels/Training/DesignProgressViewModel.cs
M src/App.Desktop/Views/Training/ConversationsDesignerView.xaml
M src/App.Desktop/Views/Training/DesignProgressView.xaml
M src/App.Shared/DirectoryBootstrap.cs
M src/App.Shared/LazarusPaths.cs
M src/App.Shared/Models/Training/TrainingConfiguration.cs
?? ".sln -c Debug"
?? "ignProgressView.xaml\357\200\272 removed System Resources card."
?? "k\357\200\272\357\200\252\357\200\252 None"
?? "locally after removal."
?? "service and interface."
?? src/App.Backend/Services/Training/ITrainer.cs
?? src/App.Backend/Services/Training/TrainerPlans.cs
?? src/App.Backend/Services/Training/TrainingJobOrchestrator.cs
?? src/App.Shared/Models/Training/TrainingProfile.cs
?? "tem Resources UI and all system polling from DesignProgress."
?? "\357\200\272\357\200\252\357\200\252"
`

## 4) Per-File Notes

- src/App.Shared/LazarusPaths.cs: adds Training.* paths and enumerates them in directory creation list.
- src/App.Shared/DirectoryBootstrap.cs: ensures the new training folders exist on bootstrap.
- src/App.Shared/Models/Training/TrainingProfile.cs: unified schema with System.Text.Json attributes.
- src/App.Backend/Services/Training/*: planning/orchestrator scaffolding to generate trainer configs and manifest.

## 5) Commands / Scripts Touched

`
- dotnet build Lazarus.sln -c Debug
`

## 6) Validation

- Build succeeded locally; no public APIs broken.
- Verified path constants resolve under %LOCALAPPDATA%/Lazarus/System-Data/Training.

## 7) Next Steps

1. Implement process launcher to run trainers and stream logs/metrics.
2. Add EF entities/migrations for Datasets, TrainingJobs, JobEvents, Artifacts.
3. Add API endpoints and wire UI panel to TrainingProfile schema.
4. Add dataset import/normalize pipeline and row statistics.

## 8) Risks / Rollback

- Risk: Path contract drift from UI expectations. Mitigation: single source via LazarusPaths.
- Rollback: revert commit below.

