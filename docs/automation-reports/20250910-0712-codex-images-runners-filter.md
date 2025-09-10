# Automation Report Filter Images runner scan to known engines

- **Date:** 2025-09-10 07:12
- **Agents:** codex
- **Branch:** main
- **Before SHA:** d7883d80f4a840b0e01d488abbc602963bff415c
- **After SHA:** uncommitted

## 1) Intent

Align Images runner discovery with the models/chats approach: show only recognized image engines and never surface domain folders.

## 2) Outcome

- Removed generic fallback patterns and the "surface folder anyway" code.
- Only recognized engines are scanned (stable-diffusion, sdwebui, comfyui, invokeai).
- Dropdown no longer shows domain folders like Audio/Avatars/Images/Shared/Videos.

## 3) Files Changed

`	xt
modified  src/App.Desktop/Views/ImagesView.xaml.cs
`

## 4) Per-File Notes

- ImagesView.xaml.cs Tightened ScanImageRunners() to filter by known engines and skip unknowns entirely.

## 5) Commands / Scripts Touched

`
N/A
`

## 6) Validation

- Build succeeded locally (0 errors/warnings).
- Runner combo now lists only actual image engines found under %LOCALAPPDATA%\Lazarus\Runners\Images or legacy directly under Runners.

## 7) Next Steps

1. Optionally expand recognized engines list if we add more.
2. Persist selected image runner in settings, analogous to models.

## 8) Risks / Rollback

- **Risk:** Engines with non-standard entrypoints might be skipped. **Mitigation:** Add patterns as needed.
- **Rollback:** Reintroduce broader patterns or fallback.

