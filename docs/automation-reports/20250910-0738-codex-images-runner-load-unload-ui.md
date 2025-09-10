# Automation Report Add Load/Unload controls for Images runner

- **Date:** 2025-09-10 07:38
- **Agents:** codex
- **Branch:** main
- **Before SHA:** 9b1b7e73ff419c5882a59095069dbe35e60c6eb4
- **After SHA:** uncommitted

## 1) Intent

Provide Load/Unload controls on the Images runner card so users can swap the image runner without restarting, matching the Models UX.

## 2) Outcome

- Added Load Selected and Unload buttons.
- Load Selected persists the chosen engine path to settings (LastImageRunnerPath) and updates runner status message.
- Unload clears the saved runner and resets selection.

## 3) Files Changed

`	xt
modified  src/App.Desktop/Views/ImagesView.xaml
modified  src/App.Desktop/Views/ImagesView.xaml.cs
`

## 4) Per-File Notes

- ImagesView.xaml Added buttons and enable rules; kept Refresh button for parity.
- ImagesView.xaml.cs Implemented LoadSelectedRunnerCommand and UnloadRunnerCommand using ISettingsService.

## 5) Commands / Scripts Touched

`
N/A
`

## 6) Validation

- Build succeeded locally.
- Selecting a runner enables Load Selected; clicking it saves and shows a confirmation toast. Unload clears it without app restart.

## 7) Next Steps

1. When image backend orchestration lands, wire Load Selected to start/attach engine and update diagnostics live.
2. Consider disabling Unload when no runner is selected.

## 8) Risks / Rollback

- **Risk:** Minimal; currently limited to UI + persisted selection.
- **Rollback:** Revert the commit or remove the buttons.

