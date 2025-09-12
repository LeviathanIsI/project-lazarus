# Automation Report Chat: “Jump to latest” control

- **Date:** 2025-09-12 11:35
- **Agents:** codex
- **Branch:** main
- **Before SHA:** b3fae4be972f4d5f6421bfc3b29b730cb32c3e2d
- **After SHA:** uncommitted

## 1) Intent

Add a visible control to quickly scroll to the newest message when the user is not at the bottom of the chat.

## 2) Outcome

- Exposed AutoScrollBehavior.IsAtBottom attached property for binding.
- Added a routed command AutoScrollBehavior.ScrollToEndCommand and bound it to the messages ScrollViewer.
- Inserted a “Jump to latest” button that appears only when not at bottom and triggers the command.

## 3) Files Changed

`	xt
modified  src/App.Desktop/Behaviors/AutoScrollBehavior.cs
modified  src/App.Desktop/Views/ChatSessionsView.xaml
`

## 4) Per-File Notes

- AutoScrollBehavior.cs now tracks and exposes IsAtBottom and installs a command binding to call ScrollToEnd().
- ChatSessionsView.xaml adds a button overlay (bottom-right of messages panel), shows via inverted IsAtBottom and executes the routed command against the messages scroller.

## 5) Commands / Scripts Touched

`
None
`

## 6) Validation

- Build succeeded; button visibility toggles via binding to (behaviors:AutoScrollBehavior.IsAtBottom).
- Clicking the button scrolls the messages panel to the latest.

## 7) Next Steps

1. Fade-in/out animation for the button to feel more polished.
2. Keyboard shortcut (e.g., End) to trigger the command when focus is on chat.

## 8) Risks / Rollback

- Low risk; changes are contained to the chat view and behavior.
- Rollback: git revert <after_sha>.
