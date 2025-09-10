# Automation Report Images: strong DTO path (RunnerId ? SD service only)

- **Date:** 2025-09-10 13:57
- **Agents:** codex
- **Branch:** main
- **Before SHA:** e7e1d1f273097cb89d4d6b6841bf22698ea9b2d7
- **After SHA:** uncommitted

## 1) Intent

Introduce a strong, explicit Images pipeline: UI ? ViewModel ? ImageGenRequest {RunnerId, ModelPath, …} ? StableDiffusionImageGenService (no llama). Add a role-aware runner registry and optional orchestrator endpoint.

## 2) Outcome

- Added shared contracts: RunnerRole, RunnerDescriptor, ImageGenRequest, ImageGenEvent.
- Implemented RunnerRegistry and StableDiffusionImageGenService with Ping + txt2img call to SD WebUI-compatible endpoints.
- Desktop DI wires RunnerRegistry and SD image service; Orchestrator Host registers both and exposes /v1/images/txt2img.
- ImagesViewModel can now pass RunnerId through to the new service (keeps legacy path to avoid breaking UI).

## 3) Files Changed
```txt
 M src/App.Desktop/Extensions/ServiceCollectionExtensions.cs
 M src/App.Desktop/ViewModels/ChatSessionsViewModel.cs
 M src/App.Desktop/ViewModels/ImagesViewModel.cs
 M src/App.Orchestrator.Host/Program.cs
?? src/App.Backend/Services/ImageGen/
?? src/App.Backend/Services/Runners/
?? src/App.Backend/bin2/
?? src/App.Data/bin2/
?? src/App.Desktop/bin2/
?? src/App.Shared/Contracts/
?? src/App.Shared/RunnerContracts/
?? src/App.Shared/bin2/
```

## 4) Per-File Notes
- src/App.Shared/RunnerContracts/RunnerRole.cs New enum for runner roles.
- src/App.Shared/RunnerContracts/RunnerDescriptor.cs New descriptor extended with Role, Kind, ExecPath, BaseUrl.
- src/App.Shared/Contracts/ImageGenRequest.cs New DTO carrying prompt, model path, and RunnerId.
- src/App.Shared/Contracts/ImageGenEvent.cs New event record for progress/info/completed/error.
- src/App.Backend/Services/Runners/RunnerRegistry.cs New registry with lookup by Id and role.
- src/App.Backend/Services/ImageGen/IImageGenService.cs New interface.
- src/App.Backend/Services/ImageGen/StableDiffusionImageGenService.cs New SD-only implementation with health ping and txt2img call.
- src/App.Orchestrator.Host/Program.cs DI + optional /v1/images/txt2img route (inserted before app.Run).
- src/App.Desktop/Extensions/ServiceCollectionExtensions.cs Desktop DI for registry + SD image gen service.
- src/App.Desktop/ViewModels/ImagesViewModel.cs Optional path to call new service with RunnerId; added minimal properties to compile.

## 5) Commands / Scripts Touched
```
None
```

## 6) Validation
- Build succeeded locally
- With orchestrator available, POST /v1/images/txt2img accepts ImageGenRequest; SD service validates RunnerId and only calls SD endpoints.
- Desktop DI composes successfully; existing UI remains functional.

## 7) Next Steps
1. Populate RunnerRegistry from discovery/settings (currently placeholder).
2. Extend payloads for img2img/inpaint and ControlNet.

## 8) Risks / Rollback
- Risk: SD endpoint shape may differ; adjust mapping as needed. Rollback: revert added files and DI registrations.
