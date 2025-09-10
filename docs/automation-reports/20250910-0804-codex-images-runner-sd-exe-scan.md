# Automation Report Expand Images scan to detect sd.exe

- **Date:** 2025-09-10 08:04
- **Agents:** codex
- **Branch:** main
- **Before SHA:** b6f4c428c5c99ceccfb9eb507c1f7025867510fa
- **After SHA:** uncommitted

## 1) Intent

Ensure stable-diffusion engines that ship sd.exe are detected and show up in the Images runner dropdown.

## 2) Outcome

- Added sd.exe and sd*.exe to the patterns scanned under Runners/Images/stable-diffusion/**.
- The dropdown will now include folders containing sd.exe (e.g., sd-master-...).

## 3) Files Changed

`	xt
modified  src/App.Desktop/Views/ImagesView.xaml.cs
`

## 4) Per-File Notes

- ScanImageRunners() pattern list extended for stable-diffusion engine.

## 5) Validation

- Build succeeded; confirmed patterns pick up sd.exe.

## 6) Risks / Rollback

- **Risk:** Minimal; limited to engine-specific patterns.
- **Rollback:** Revert this change.

