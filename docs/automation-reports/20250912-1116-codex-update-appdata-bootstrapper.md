# Automation Report Update AppData bootstrapper (expanded layout)

- **Date:** 2025-09-12 11:16
- **Agents:** codex
- **Branch:** main
- **Before SHA:** 4c08fba593847ff0a256dc2f6724b4db83286eeb
- **After SHA:** uncommitted

## 1) Intent

Align the first-run AppData directory bootstrap with the new Lazarus layout, adding projects workspace, trainers, runner engine folders, config subtrees, and global assets.

## 2) Outcome

- LazarusPaths additions:
  - SystemData.Temp, Config_App, Config_Paths, Config_Theme, TrainingRecipes, Pipelines.
  - Models.Diffusers and UserContent.Scratch.
- DirectoryBootstrap.LeafDirectories expanded to create the new structure:
  - System-Data subfolders, Projects root, engine-scoped Runners trees (LlamaCpp, vLLM, ExLlamaV2), Trainers (LLaMA-Factory, Axolotl, Unsloth), global Audio engines, and global Avatars assets.
- Build validated successfully.

## 3) Files Changed

`	xt
modified  src/App.Shared/LazarusPaths.cs
modified  src/App.Shared/DirectoryBootstrap.cs
`

## 4) Per-File Notes

- LazarusPaths.cs Adds new canonical constants; backward compatible.
- DirectoryBootstrap.cs Creates additional directories idempotently; no deletions.

## 5) Commands / Scripts Touched

`
DirectoryBootstrap.EnsureAll() now materializes the expanded tree.
`

## 6) Validation

- Build succeeded locally (dotnet build -c Debug).
- Create paths by calling DirectoryBootstrap.EnsureAll() (app startup or manual).

## 7) Next Steps

1. Optionally expose a command in Global Actions to re-bootstrap missing folders.
2. Add unit coverage for DirectoryBootstrap if a test project is added later.

## 8) Risks / Rollback

- Low risk: only creates missing directories; no deletions. Rollback: git revert <after_sha>.
