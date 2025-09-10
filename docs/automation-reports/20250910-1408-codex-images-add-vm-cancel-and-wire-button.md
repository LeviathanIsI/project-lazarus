# Automation Report Images: add VM cancel + wire button

- **Date:** 2025-09-10 14:08
- **Agents:** codex
- **Branch:** main
- **Before SHA:** d7ff60e65e01a6ad8250795789593c49f7016d4e
- **After SHA:** uncommitted

## 1) Intent

Add cancellation to ImagesViewModel and wire the UI Cancel button to invoke it.

## 2) Outcome

- ImagesViewModel: private CTS, public CancelCommand, GenerateAsync observes ct and handles OperationCanceledException.
- ImagesView.xaml.cs: Cancel button handler invokes VM.CancelCommand; build clean.

## 3) Files Changed
```txt
modified  src/App.Desktop/ViewModels/ImagesViewModel.cs
modified  src/App.Desktop/Views/ImagesView.xaml.cs
```

## 6) Validation
- Build succeeded locally
- While generating, Cancel becomes enabled and cancels the run (status: Canceled.)
