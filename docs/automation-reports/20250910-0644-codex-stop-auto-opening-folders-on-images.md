# Automation Report Stop auto-opening folders on Images screen

- **Date:** 2025-09-10 06:44
- **Agents:** codex
- **Branch:** main
- **Before SHA:** 440aefc01b025e9ff53b1af5453d809b7a831e17
- **After SHA:** uncommitted

## 1) Intent

Prevent the app from automatically opening folders when navigating to the Images screen. Only open folders via explicit user actions.

## 2) Outcome

Removed the implicit Explorer launch that occurred on combo selection changes for LoRAs, Embeddings, and Hypernetworks. The UI still exposes explicit buttons to open folders; no folders are opened on navigation or automatic selection.

## 3) Files Changed

`	xt
modified  src/App.Desktop/Views/ImagesView.xaml.cs
`

## 4) Per-File Notes

- src/App.Desktop/Views/ImagesView.xaml.cs Stop calling OpenFolderSafe in TryOpenIfPlaceholder; keep selection reset behavior.

## 5) Commands / Scripts Touched

`
N/A
`

## 6) Validation

- Build succeeded locally
- Navigating to Images view no longer launches Explorer automatically
- Affected paths (no longer auto-open):
  - %LOCALAPPDATA%\Lazarus\Generation-Assets\Style-Presets\Hypernetworks
  - %LOCALAPPDATA%\Lazarus\Generation-Assets\Style-Presets\LoRAs
  - %LOCALAPPDATA%\Lazarus\Generation-Assets\Style-Presets\Embeddings

## 7) Next Steps

1. Consider adding a small inline hint or tooltip guiding users to the explicit “Open Folder” buttons.
2. If further auto-open behavior is reported, trace additional handlers that might trigger OpenFolderSafe indirectly.

## 8) Risks / Rollback

- **Risk:** None functionally; the change only removes automatic Explorer launch. **Mitigation:** Explicit open buttons remain available.
- **Rollback:** Revert the commit or reintroduce the call inside TryOpenIfPlaceholder.

