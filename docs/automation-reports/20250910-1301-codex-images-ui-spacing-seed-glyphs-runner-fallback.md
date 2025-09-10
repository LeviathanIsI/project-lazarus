# Automation Report Images UI: spacing, seed glyphs, runner fallback

- **Date:** 2025-09-10 13:01
- **Agents:** codex
- **Branch:** main
- **Before SHA:** ce369d5e423ce1a891a03f5a0a0b069aa322a7b9
- **After SHA:** uncommitted

## 1) Intent

Improve readability of seed/steps controls, fix corrupted glyphs, and ensure generation hits the image runner even if the service path fails.

## 2) Outcome

- Seed row: larger textbox width, clear buttons (Rnd and Lock) with tooltips.
- Steps row: wider columns, added numeric Steps display; more spacing.
- Keyboard: Ctrl+Enter preserved; no Space-to-generate.
- Generation: _imageService call guarded; on exception it falls through to start the selected runner; writes image-runner-*.out.log/err.log to System-Data/Logs.

## 3) Files Changed

`	xt
modified  src/App.Desktop/Views/ImagesView.xaml
modified  src/App.Desktop/Views/ImagesView.xaml.cs
`

## 4) Per-File Notes

- ImagesView.xaml Seed/Steps layout tweaks, tooltips, added steps value display.
- ImagesView.xaml.cs Lock glyph fixed to Lock/Unlock; protected service call with catch to allow runner fallback.

## 5) Commands / Scripts Touched

`
None
`

## 6) Validation

- Build: dotnet build Lazarus.sln -c Debug → Success.
- Visual: controls no longer cramped; labels clear.
- Functional: If IImageService is null/throws, runner starts and logs appear at %LOCALAPPDATA%/Lazarus/System-Data/Logs/image-runner-*.log.

## 7) Next Steps

1. Add determinate progress based on runner output if available.

## 8) Risks / Rollback

- Low risk; localized XAML + guarded call.
- Rollback: git revert <after_sha>.
