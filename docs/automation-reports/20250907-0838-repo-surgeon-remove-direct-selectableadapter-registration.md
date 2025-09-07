# Automation Report  Remove direct SelectableAdapter DI registration

- **Date:** 2025-09-07 08:38
- **Agents:** repo-surgeon
- **Branch:** main
- **Before SHA:** 79bd73389c2b3f41bf386daf08f41354bde90d76
- **After SHA:** uncommitted

## 1) Intent
Eliminate DI validation error by removing direct SelectableAdapter registration that required AdapterInfo, keeping only the typed factory.

## 2) Outcome
App builds cleanly; Startup container validation no longer attempts to construct SelectableAdapter without AdapterInfo.

## 3) Files Changed
```txt
modified  src/App.Desktop/App.xaml.cs
```

## 4) Per-File Notes
* src/App.Desktop/App.xaml.cs  Removed AddTransient<SelectableAdapter>(); retained Func<AdapterInfo, SelectableAdapter> factory.

## 5) Commands / Scripts Touched
```
No scripts modified
```

## 6) Validation
* Build succeeded locally
* DI error not reproduced after change

## 7) Next Steps
1. If design-time tools require direct resolution, provide a design-time AdapterInfo via a designer-only factory path.

## 8) Risks / Rollback
* Risk: Any code trying to resolve SelectableAdapter directly will fail. Mitigation: Use the factory everywhere.
* Rollback: revert this commit.
