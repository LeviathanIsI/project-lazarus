# Automation Report Chat: send→stop toggle with cancel

- **Date:** 2025-09-12 11:46
- **Agents:** codex
- **Branch:** main
- **Before SHA:** 0d0fc1de80904b23bdce4210839674295db8511e
- **After SHA:** uncommitted

## 1) Intent

When the assistant is generating, change the Send button into a Stop button that cancels inference.

## 2) Outcome

- ViewModel: added CancelInferenceCommand that calls _cts?.Cancel(); enabled only when IsStreaming.
- View: replaced single send button with two mutually exclusive buttons (Send vs Stop), switching on IsStreaming.
- Stop button uses a square icon; Send remains the arrow.

## 3) Files Changed

`	xt
modified  src/App.Desktop/ViewModels/ChatSessionsViewModel.cs
modified  src/App.Desktop/Views/ChatSessionsView.xaml
`

## 4) Per-File Notes

- ChatSessionsViewModel Raises CanExecute for cancel when IsStreaming changes.
- ChatSessionsView.xaml Maintains IsDefault only on Send; Stop doesn't capture Enter.

## 5) Commands / Scripts Touched

`
None
`

## 6) Validation

- Build succeeded locally; cancel toggles correctly and stops streaming via CTS.

## 7) Next Steps

1. Gray out input while streaming if desired, or keep editable.
2. Add a short fade/crossfade between Send and Stop for polish.

## 8) Risks / Rollback

- Low risk; UI/command changes scoped to chat only.
- Rollback: git revert <after_sha>.
