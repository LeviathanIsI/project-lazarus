# Automation Report Ensure stable-diffusion runner folder

- **Date:** 2025-09-10 06:40
- **Agents:** codex
- **Branch:** main
- **Before SHA:** 7ad2041839dfb4d33abcaf47a7898935ac1ee3f6
- **After SHA:** uncommitted

## 1) Intent

Ensure the Stable Diffusion runner subfolder %LOCALAPPDATA%\Lazarus\Runners\Images\stable-diffusion is bootstrapped idempotently and included in the app's first-run directory creation.

## 2) Outcome

- Added Runners.Images_StableDiffusion constant in LazarusPaths.
- Included this path in DirectoryBootstrap.LeafDirectories and EnumerateAllDirectories() so startup creates it automatically.
- Updated the PowerShell bootstrap script to create and report status for the stable-diffusion subfolder under Runners\\Images.

## 3) Files Changed

`	xt
modified  src/App.Shared/LazarusPaths.cs
modified  src/App.Shared/DirectoryBootstrap.cs
modified  scripts/bootstrap/Ensure-Runners-Images.ps1
added     docs/automation-reports/20250910-0640-codex-runners-stable-diffusion-folder.md
`

## 4) Per-File Notes

- src/App.Shared/LazarusPaths.cs Add Images_StableDiffusion path and include in enumeration.
- src/App.Shared/DirectoryBootstrap.cs Ensure the folder is part of first-run leaf directories.
- scripts/bootstrap/Ensure-Runners-Images.ps1 Create %LOCALAPPDATA%\Lazarus\Runners\Images\stable-diffusion and print Created/Exists.
- $file This report documents the change and validation.

## 5) Commands / Scripts Touched

`
scripts/bootstrap/Ensure-Runners-Images.ps1
`

## 6) Validation

- Build succeeded locally
- Folder bootstrap executed
- Feature verified:
  - C:\Users\Josh\AppData\Local\Lazarus\Runners\Images\stable-diffusion  Exists
- Evidence: N/A

## 7) Next Steps

1. Optionally surface this folder in any runner selection UI defaults.
2. Keep runner folder names consistent across docs and code.

## 8) Risks / Rollback

- **Risk:** Minimal; creates a single subfolder under user-local app data. **Mitigation:** Idempotent creation.
- **Rollback:** git revert <after_sha> or remove the added constant and list entries.

