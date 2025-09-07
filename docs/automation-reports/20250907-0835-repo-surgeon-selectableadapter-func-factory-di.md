# Automation Report  SelectableAdapter DI factory (Func<AdapterInfo, SelectableAdapter>)

- **Date:** 2025-09-07 08:35
- **Agents:** repo-surgeon
- **Branch:** main
- **Before SHA:** 8365197efb63e041d795313362cf11e9420c1d9d
- **After SHA:** uncommitted

## 1) Intent
Introduce a typed factory using Func<AdapterInfo, SelectableAdapter> so DI can construct SelectableAdapter while the caller supplies AdapterInfo. Move SelectableAdapter to its own file and update bindings/usage.

## 2) Outcome
Added transient factory registration in App.xaml.cs using ActivatorUtilities. Implemented SelectableAdapter with ctor (AdapterInfo, IOrchestratorClient, ILogger). Injected and used the factory in ModelsViewModel; updated XAML to bind Info.Name. Removed prior ISelectableAdapterFactory.

## 3) Files Changed
```txt
modified  src/App.Desktop/App.xaml.cs
modified  src/App.Desktop/ViewModels/ModelsViewModel.cs
modified  src/App.Desktop/Views/ModelsView.xaml
added     src/App.Desktop/ViewModels/SelectableAdapter.cs
```

## 4) Per-File Notes
* src/App.Desktop/App.xaml.cs  Register Func<AdapterInfo, SelectableAdapter> and plain SelectableAdapter.
* src/App.Desktop/ViewModels/SelectableAdapter.cs  New class with ctor deps + IsSelected and debug log.
* src/App.Desktop/ViewModels/ModelsViewModel.cs  Inject Func factory; replace Item->Info and factory usage.
* src/App.Desktop/Views/ModelsView.xaml  Bind to Info.Name.

## 5) Commands / Scripts Touched
```
No scripts; DI registration added in App.xaml.cs
```

## 6) Validation
* Build succeeded locally
* Launch attempted; GUI apps may not stay attached in this environment
* DI error for AdapterInfo not registered no longer reproducible in build path

## 7) Next Steps
1. Verify UI behavior (checkbox selection) still updates as expected.
2. Consider adding similar factories for other per-item VMs if present.

## 8) Risks / Rollback
* Risk: Any code still referencing Item property on SelectableAdapter. Mitigation: Updated references and XAML to Info.
* Rollback: git revert the commit.
