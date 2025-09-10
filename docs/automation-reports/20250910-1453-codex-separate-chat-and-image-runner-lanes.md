# Automation Report Separate Chat vs. Image Runner Lanes

- **Date:** 2025-09-10 14:53
- **Agents:** codex
- **Branch:** main
- **Before SHA:** a264002a2fd48293f209580e2ba8cfe98d29b796
- **After SHA:** uncommitted

## 1) Intent

Introduce a clean separation between chat (llama-server) and image (SD/Comfy) runner lanes with a tiny in-memory registry, lane-specific services, and DI wiring. Ensure chat streaming does not get impacted by image gen changes.

## 2) Outcome

- Added RunnerKind + lane-specific DTOs (ChatRequest, ImageGenRequest).
- Implemented InMemoryRunnerRegistry (read-only).
- Added LlamaChatService (SSE streaming) and ImageGenService (sd-webui/comfy) in Backend.
- Desktop DI now registers a separate registry and lane-specific typed HttpClients + services.
- ChatSessionsViewModel: if available, uses LlamaChatService for streaming; otherwise falls back to existing code.

## 3) Files Changed
```txt
 M src/App.Desktop/Extensions/ServiceCollectionExtensions.cs
 M src/App.Desktop/ViewModels/ChatSessionsViewModel.cs
?? src/App.Backend/Runners/
?? src/App.Backend/Services/Chat/
?? src/App.Backend/Services/Image/
?? src/App.Backend/bin2/
?? src/App.Data/bin2/
?? src/App.Desktop/bin2/
?? src/App.Shared/Contracts/Chat/
?? src/App.Shared/Contracts/Image/
?? src/App.Shared/Enums/
?? src/App.Shared/bin2/
```

## 6) Validation
- Build succeeded locally
- Chats stream via llama-server independently; images route via selected image runner only.

## 7) Next Steps
1. Optionally migrate legacy chat HTTP path fully to LlamaChatService.
2. Load runner registry from settings/DB instead of the fixed sample.

## 8) Risks / Rollback
- Risk: Two registries (legacy Services.Runners and new Backend.Runners) coexist; both are intentionally scoped to avoid collisions.
- Rollback: revert the added DI lines and new files.
