# Automation Report Images: fix scoped VM resolution

- **Date:** 2025-09-10 14:10
- **Agents:** codex
- **Branch:** main
- **Before SHA:** 3e5e40ad3509b1e42155c2b955c2a5d0350ae4cc
- **After SHA:** uncommitted

## 1) Intent

Resolve InvalidOperationException: creating ImagesViewModel (needs scoped repo) from root provider. Create per-VM scopes in ViewModelLocator.

## 2) Outcome

- ViewModelLocator now creates and retains an IServiceScope per singleton VM; Release/Clear also dispose scopes. Scoped dependencies now resolve cleanly.

## 3) Files Changed
```txt
modified  src/App.Desktop/ViewModels/ViewModelLocator.cs
```

## 6) Validation
- Build succeeded locally
- Resolving ImagesViewModel via ViewModelLocator no longer throws scoped resolution error.
