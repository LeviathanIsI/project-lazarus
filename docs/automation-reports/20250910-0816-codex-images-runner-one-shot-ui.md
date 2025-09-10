# Automation Report Move runner selector to left card; remove loader card

- **Date:** 2025-09-10 08:16
- **Agents:** codex
- **Branch:** main
- **Before SHA:** 5a5eb80d330c489eefcd8fdb6df30df59ff2ea66
- **After SHA:** uncommitted

## 1) Intent

Use a one-shot model for image engines: user selects a runner inline, enters prompts, and Generate invokes the engine. Remove the separate loader/status card.

## 2) Outcome

- Runner selector moved above Prompt on the left side.
- Removed the right-side Runner loader/diagnostics card to prevent overlap and reduce complexity.
- Refresh button remains to rescan runners; selection persists.

## 3) Files Changed

`	xt
modified  src/App.Desktop/Views/ImagesView.xaml
`

## 4) Per-File Notes

- ImagesView.xaml Adds compact Runner selector row near Prompt and drops the loader card.

## 5) Validation

- Build succeeded; visual layout confirmed.

## 6) Risks / Rollback

- **Risk:** None; minimal layout change.
- **Rollback:** Revert this change.

