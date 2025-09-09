# Automation Report HUD ToolTips: Window RelativeSource

- **Date:** 2025-09-09 12:28
- **Agents:** codex
- **Branch:** main
- **Before SHA:** a3c77a1788f53f6dbeab52856f7b4e56f821fb0d
- **After SHA:** uncommitted

## 1) Intent

Make HUD tooltip bindings robust by targeting the Window's DataContext, removing reliance on PlacementTarget during template parse.

## 2) Outcome

- Rewrote both HUD ToolTips to contain a TextBlock bound to the Window via RelativeSource AncestorType=Window.

## 3) Files Changed

`	xt
modified  src/App.Desktop/MainWindow.xaml
`

## 4) Per-File Notes

- src/App.Desktop/MainWindow.xaml Updated tooltip binding blocks for Orchestrator and Runner.

## 5) Commands / Scripts Touched

`
None
`

## 6) Validation

- Build successful locally.

## 7) Next Steps

- If any startup binding errors persist, capture the first XamlParseException message for precise fix.

## 8) Risks / Rollback

- Low risk; localized tooltip change.
- Rollback via git revert of this commit.
