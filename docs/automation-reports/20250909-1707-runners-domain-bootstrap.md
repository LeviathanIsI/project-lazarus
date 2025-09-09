# Automation Report Runners Domain Bootstrap

- **Date:** 2025-09-09 17:07
- **Agents:** codex
- **Branch:** main
- **Before SHA:** a50ca678d3b4a1e2c22ea57cd1b38f3e50c2febc
- **After SHA:** uncommitted

## New Runner Directories
```txt
%LOCALAPPDATA%/Lazarus/Runners/Chats/{llama.cpp,vllm,exllamav2}
%LOCALAPPDATA%/Lazarus/Runners/Images/{comfyui,sdwebui,invokeai}
%LOCALAPPDATA%/Lazarus/Runners/Videos/{animatediff,svd,rife}
%LOCALAPPDATA%/Lazarus/Runners/Audio/{faster-whisper,piper,rvc,noise-reduction}
%LOCALAPPDATA%/Lazarus/Runners/Avatars/{rhubarb,tripo-sr,nerfstudio}
%LOCALAPPDATA%/Lazarus/Runners/Shared/{ffmpeg,utils}
```

## Modified Files
```txt
modified  src/App.Shared/LazarusPaths.cs
modified  src/App.Shared/DirectoryBootstrap.cs
```

## Validation Checklist
- Build succeeded.
- First run creates all domain folders if missing; subsequent runs report Exists.
- ResolveRunnerPath(engine) prefers domain path and falls back to legacy flat path; if neither exists, creates preferred domain.
- Legacy flat properties (LlamaCpp, Vllm, ExLlamaV2) now resolve through ResolveRunnerPath without warnings.

## Notes
- Idempotent: only creates missing directories; never deletes or moves files.
