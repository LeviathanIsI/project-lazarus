# Automation Report Images: Multiline Prompts + Backend Guard

- **Date:** 2025-09-09 16:41
- **Agents:** codex
- **Branch:** main
- **Before SHA:** 2245145604f602f3d860e8779a55acb900cf35f6
- **After SHA:** uncommitted

## Files Changed
```txt
modified  src/App.Desktop/Views/ImagesView.xaml
modified  src/App.Desktop/Views/ImagesView.xaml.cs
```

## Summary
- Prompt/Negative Prompt replaced with clean multi-line TextBoxes (no clipping, scrollable).
- Generate now calls backend when available; if not configured, shows a non-blocking warning and does NOT replace the preview.
- Removed default/placeholder preview writes; only real outputs update the preview.

## Validation
- Build succeeded (dotnet build -c Debug).
- With no backend: clicking Generate shows a warning and keeps current preview unchanged.
- With backend: output image loads into preview, counters increment.

## Screenshots / Evidence
- Prompt fields before/after, preview unchanged on missing backend (attach separately if desired).
