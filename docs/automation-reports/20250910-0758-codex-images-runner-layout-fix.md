# Automation Report Fix Images runner layout (no overlay)

- **Date:** 2025-09-10 07:58
- **Agents:** codex
- **Branch:** main
- **Before SHA:** 3c8849edb64e3e9a178df362a3ed6047d8ac0472
- **After SHA:** uncommitted

## 1) Intent

Prevent the Images runner UI from overlaying on the assets grid by stacking cards vertically, like in Models.

## 2) Outcome

- Wrapped right column contents in a StackPanel (Grid.Column=1).
- Moved the runner card above the assets card.
- Removed the duplicate runner card that was inside the assets Grid.

## 3) Files Changed

`	xt
modified  src/App.Desktop/Views/ImagesView.xaml
`

## 4) Per-File Notes

- ImagesView.xaml Right column now stacks runner and assets cards; removed duplicate runner block inside Grid to avoid overlay.

## 5) Validation

- Build succeeded locally; verified visually that cards no longer overlap.

## 6) Risks / Rollback

- **Risk:** None; XAML layout only.
- **Rollback:** Revert this change.

