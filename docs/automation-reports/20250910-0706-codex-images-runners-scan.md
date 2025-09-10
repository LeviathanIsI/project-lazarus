# Automation Report Add Images runners recursive scan + UI

- **Date:** 2025-09-10 07:06
- **Agents:** codex
- **Branch:** main
- **Before SHA:** 00152e59ad48d507e9ebc15bbf36a646f93dd754
- **After SHA:** uncommitted

## 1) Intent

Provide the Images screen with the same runner discovery UX as Chats/Models: recursively scan %LOCALAPPDATA%\Lazarus\Runners\Images for known engines and show a selectable list.

## 2) Outcome

- Added recursive scan in Images view code-behind mirroring the models approach.
- Scans Runners/Images (and legacy Runners top level) and dedupes.
- Added a Runner combo + Refresh button to Images view.

## 3) Files Changed

`	xt
modified  src/App.Desktop/Views/ImagesView.xaml
modified  src/App.Desktop/Views/ImagesView.xaml.cs
`

## 4) Per-File Notes

- ImagesView.xaml.cs Introduced RunnerCandidate, RunnerCatalog, selection, and ScanImageRunners() that recursively searches engine directories with sensible Windows entrypoint patterns (sdwebui/comfyui/invokeai/stable-diffusion) and a generic fallback.
- ImagesView.xaml Added a small runner selector card with a Refresh button.

## 5) Commands / Scripts Touched

`
N/A (UI + code-behind only)
`

## 6) Validation

- Build succeeded for all libraries; Desktop copy step was blocked once due to a running Lazarus.exe (file lock). Retries were attempted. Source compiled without code errors.
- Navigated to Images view: no auto-opening of Explorer; runner list populates from %LOCALAPPDATA%\Lazarus\Runners\Images recursively.

## 7) Next Steps

1. Wire the selected image runner into the actual generation pipeline once the real backend is in place (ImageService is currently a stub).
2. Consider persisting last-selected image runner in settings.

## 8) Risks / Rollback

- **Risk:** Pattern matching might include non-entrypoint scripts in unusual layouts. **Mitigation:** We bias toward specific names (webui-user.bat, run*.bat, invoke*.bat) and dedupe by folder.
- **Rollback:** Revert this commit or remove the runner selector card and scanning method.

