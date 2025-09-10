# Automation Report Align Images runner selection with Models

- **Date:** 2025-09-10 07:32
- **Agents:** codex
- **Branch:** main
- **Before SHA:** d50b7244268bc512cb4e9ab4c7374c3f192425a5
- **After SHA:** uncommitted

## 1) Intent

Make the Images view runner selection and status mirror the Models view: same dropdown item template, recursive scan, and diagnostics block.

## 2) Outcome

- Added runner selector + status card to the right column of Images view, matching Models markup.
- Scan filters to recognized engines (stable-diffusion, sdwebui, comfyui, invokeai) under %LOCALAPPDATA%\\Lazarus\\Runners\\Images (and legacy under Runners).
- Bound the same fields (Status, Model Path, PID, Binary, Port, Err/Out logs, Message) and wired Refresh.

## 3) Files Changed

`	xt
modified  src/App.Desktop/Views/ImagesView.xaml
modified  src/App.Desktop/Views/ImagesView.xaml.cs
`

## 4) Per-File Notes

- ImagesView.xaml Runner card moved to right column, sibling to asset card (not nested), mirroring Models.
- ImagesView.xaml.cs Added runner scan exposure VisibleRunnerCatalog, plus basic runner status via IOrchestratorRunnerClient.

## 5) Commands / Scripts Touched

`
N/A
`

## 6) Validation

- Build succeeded (0 errors/warnings).
- Dropdown shows only real image engines; domain folders no longer appear.
- Runner card UI aligns visually/structurally with Models.

## 7) Next Steps

1. If desired, add Load/Unload buttons for image engines once backend orchestration supports them.
2. Persist last-selected image runner in settings for Images.

## 8) Risks / Rollback

- **Risk:** No functional change beyond UI and local scan/status; future backend wiring may adjust fields.
- **Rollback:** Revert this commit.

