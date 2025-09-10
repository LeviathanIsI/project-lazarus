# Automation Report Images: route Generate through selected runner

- **Date:** 2025-09-10 13:46
- **Agents:** codex
- **Branch:** main
- **Before SHA:** 95ff095aba7c934275f0af8ec6537431ce48459c
- **After SHA:** uncommitted

## 1) Intent

Ensure Generate always uses the selected Image runner and never falls back to any default; block when runner missing.

## 2) Outcome

- On Generate: require SelectedRunner, preflight, attempt to load model, start runner, and watch for output. Removed direct call to placeholder ImageService to avoid silent fallback.

## 3) Files Changed
```txt
modified  src/App.Desktop/Views/ImagesView.xaml.cs
```

## 4) Per-File Notes
- src/App.Desktop/Views/ImagesView.xaml.cs Added guard for SelectedRunner and removed dummy backend path so generation goes only via selected image runner.

## 5) Commands / Scripts Touched
```
None
```

## 6) Validation
- Build succeeded locally
- With no runner selected: UI shows "Select an Image runner." and does not launch.
- With runner selected and model chosen: runner starts and output image detected; no other engine used.

## 7) Next Steps
1. Introduce a shared ImageGenRequest DTO with RunnerId when backend is ready, and thread it through to a real image gen service.

## 8) Risks / Rollback
- Risk: Some runner flavors may not emit output under expected folder. Mitigation: allow configuring output dir.
- Rollback: revert the commit.
