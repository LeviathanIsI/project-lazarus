# Automation Report Normalized flags + one-shot invocation on Generate

- **Date:** 2025-09-10 08:37
- **Agents:** codex
- **Branch:** main
- **Before SHA:** 290d8c76de7cb22a313a00e8ce958cd2f793cb86
- **After SHA:** uncommitted

## 1) Intent

Build normalized CLI flags from the Images form and invoke the selected image runner as a one-shot process when the user clicks Generate.

## 2) Outcome

- Added normalized parameters to Images view (with defaults): model, sampler, batch, threads, precision, device, format, filename prefix.
- On Generate: constructs flags (model/prompt/negative/seed/steps/sampler/cfg/W/H/outdir/etc.), starts the runner, and polls the output dir for the resulting image to display.
- Kept a fallback to the dummy backend only if runner execution yields no output (for development).

## 3) Files Changed

`	xt
modified  src/App.Desktop/Views/ImagesView.xaml
modified  src/App.Desktop/Views/ImagesView.xaml.cs
`

## 4) Per-File Notes

- ImagesView.xaml Runner selector inline with prompt (one-shot model).
- ImagesView.xaml.cs BuildNormalizedArgs(), StartImageRunnerAsync now accepts normalizedArgs and passes them via env (LAZARUS_IMAGE_RUNNER_EXTRA_ARGS).

## 5) Commands / Scripts Touched

`
N/A
`

## 6) Validation

- Build succeeded.
- For stable-diffusion with sd.exe, normalized flags are appended; output captured from %LOCALAPPDATA%/Lazarus/User-Content/Generated-Output.

## 7) Next Steps

1. Add UI bindings for Steps/CFG sliders if you want to adjust away from defaults in this view.
2. Expand LoRA/ControlNet to support multiple entries with weights.

## 8) Risks / Rollback

- **Risk:** Runners may differ on flag names; we pass via EXTRA_ARGS. **Mitigation:** adjust per-engine mapping as needed.
- **Rollback:** Revert this commit.

