# Automation Report Disable Unload without selection + keep missing-runner hint

- **Date:** 2025-09-10 07:40
- **Agents:** codex
- **Branch:** main
- **Before SHA:** 16cbb46bcec6695a21944a2fd06d86059fa0d906
- **After SHA:** uncommitted

## 1) Intent

Polish the Images runner UX: disable Unload when no runner is selected and include a small hint when no runners are detected.

## 2) Outcome

- Unload button is disabled when SelectedRunner is null.
- Existing hint (No runners detected...) remains visible when RunnerCatalog.Count == 0.

## 3) Files Changed

`	xt
modified  src/App.Desktop/Views/ImagesView.xaml
`

## 4) Per-File Notes

- ImagesView.xaml Added style triggers for the Unload button mirroring expected UX.

## 5) Commands / Scripts Touched

`
N/A
`

## 6) Validation

- Build succeeded locally.
- With no selection, Unload is disabled; with selection, it enables. Hint displays correctly when no runners are found.

## 7) Next Steps

1. When image orchestration is available, wire Load/Unload to start/stop engines.

## 8) Risks / Rollback

- **Risk:** None; UI-only enabling rule.
- **Rollback:** Revert this commit.

