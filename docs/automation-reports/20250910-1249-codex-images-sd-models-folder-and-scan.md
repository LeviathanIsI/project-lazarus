# Automation Report Images: add StableDiffusion-Models folder and scan

- **Date:** 2025-09-10 12:49
- **Agents:** codex
- **Branch:** main
- **Before SHA:** be0a344bb2781a9b007c91ec3e6531810d1ea7c6
- **After SHA:** uncommitted

## 1) Intent

Keep LLM and Stable Diffusion models separate: add a Generation-Assets/StableDiffusion-Models folder, bootstrap it, and update the Images UI to look there for selectable models (not under Models/Base-Models).

## 2) Outcome

- LazarusPaths.GenAssets.StableDiffusionModels added.
- Bootstrapping includes the new folder.
- Images UI scans StableDiffusion-Models for SD model files (.safetensors, .ckpt, .ckp) and excludes .gguf.
- CLI args for image runner use the SD models path.

## 3) Files Changed

`	xt
modified  src/App.Shared/LazarusPaths.cs
modified  src/App.Shared/DirectoryBootstrap.cs
modified  src/App.Desktop/Views/ImagesView.xaml.cs
`

## 4) Per-File Notes

- src/App.Shared/LazarusPaths.cs Added StableDiffusionModels and included it in EnumerateAllDirectories.
- src/App.Shared/DirectoryBootstrap.cs Ensures the new folder exists at startup.
- src/App.Desktop/Views/ImagesView.xaml.cs Scans new SD models folder; builds --model path from it.

## 5) Commands / Scripts Touched

`
None
`

## 6) Validation

- Build: dotnet build Lazarus.sln -c Debug → Success.
- Manual: Create %LOCALAPPDATA%/Lazarus/Generation-Assets/StableDiffusion-Models and drop .safetensors or .ckpt files; they appear in the Images model dropdown; .gguf LLM models do not.

## 7) Next Steps

1. If multiple SD engines are supported, consider engine-specific subfolders later; out of scope here.

## 8) Risks / Rollback

- Low risk; scoped to folder constants and Images scanning.
- Rollback: git revert <after_sha>.
