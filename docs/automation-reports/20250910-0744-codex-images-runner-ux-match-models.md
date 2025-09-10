# Automation Report Images runner UX fully matches Models (visibility/enable rules)

- **Date:** 2025-09-10 07:44
- **Agents:** codex
- **Branch:** main
- **Before SHA:** b607f03faa672a4b3107d19769998bc03879ad65
- **After SHA:** uncommitted

## 1) Intent

Make Images runner buttons behave exactly like Models: hide Load while running and show/enable Unload only when running.

## 2) Outcome

- Load Selected now collapses when IsRunnerRunning == True.
- Unload is visible/enabled only when IsRunnerRunning == True.

## 3) Files Changed

`	xt
modified  src/App.Desktop/Views/ImagesView.xaml
`

## 4) Per-File Notes

- ImagesView.xaml Adjusted triggers to mirror Models runner card.

## 5) Validation

- Build succeeded locally; visual behavior verified.

## 6) Risks / Rollback

- **Risk:** None (UI only).
- **Rollback:** Revert this change.

