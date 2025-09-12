# Automation Report Chat autoscroll behavior

- **Date:** 2025-09-12 11:30
- **Agents:** codex
- **Branch:** main
- **Before SHA:** 74b24d511fd29709bf39fa6ae7f21ca0cf87a315
- **After SHA:** uncommitted

## 1) Intent

Ensure the chat message view autoscrolls as messages are added/streamed, but only when the user is at the bottom; suspend autoscroll when the user scrolls up.

## 2) Outcome

- Added AutoScrollBehavior (attached property) for ScrollViewer.
- Enabled it on the chat messages ScrollViewer in ChatSessionsView.xaml.
- Behavior keeps the view at bottom on content growth and respects user scroll position.

## 3) Files Changed

`	xt
added     src/App.Desktop/Behaviors/AutoScrollBehavior.cs
modified  src/App.Desktop/Views/ChatSessionsView.xaml
`

## 4) Per-File Notes

- AutoScrollBehavior.cs Uses ScrollChanged with extent-delta heuristics; ConditionalWeakTable stores per-viewer state.
- ChatSessionsView.xaml Attached ehaviors:AutoScrollBehavior.Enable="True" to MessagesScrollViewer.

## 5) Commands / Scripts Touched

`
None
`

## 6) Validation

- Build succeeded locally; autoscroll logic should engage when at bottom and content grows.

## 7) Next Steps

1. Optional: add smooth scroll animation when new content arrives.
2. Optional: add a small "Jump to latest" button when user is not at bottom.

## 8) Risks / Rollback

- Low risk; attaches to single ScrollViewer. Rollback: git revert <after_sha>.
