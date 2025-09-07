# Automation Report Settings: RAG panel + persistence

- **Date:** 2025-09-07 14:31
- **Agents:** codex
- **Branch:** main
- **Before SHA:** 6eb987c1adb6f546086171658cfd73cc8f570c7f
- **After SHA:** uncommitted

## 1) Intent

Add Embeddings/RAG controls to Settings: enable vector store, DB path picker, SQLite VSS toggle.

## 2) Outcome

- New RAG panel under Settings sidebar; persists to AppSettings.Rag.
- Browse button for the database path.

## 3) Files Changed

```txt
modified  src/App.Desktop/ViewModels/SettingsViewModel.cs
modified  src/App.Desktop/Views/SettingsView.xaml
```

## 4) Per-File Notes

- SettingsViewModel.cs Added RagEnableVectorStore, RagDatabasePath, RagUseSQLiteVss and BrowseRagDatabaseCommand.
- SettingsView.xaml Added RAG panel with dark-mode tokens and bindings.

## 5) Commands / Scripts Touched

```
None
```

## 6) Validation

- Build succeeded locally

## 7) Next Steps

1. Optional: add chunk size/overlap and embed model selection when schema expands.

## 8) Risks / Rollback

- **Risk:** None; UI wiring only to existing RagSettings.
