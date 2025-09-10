# Automation Report Images: add Steps/CFG/sampler UI, LoRA list, ControlNet input; normalize flags

- **Date:** 2025-09-10 08:51
- **Agents:** codex
- **Branch:** main
- **Before SHA:** b35291df79488788762d8cbe1590c19bf756dacb
- **After SHA:** uncommitted

## 1) Intent

Expose must-have generation parameters in the Images view and pass them as normalized flags to one-shot runners.

## 2) Outcome

- UI: sampler dropdown + Steps/CFG sliders; model dropdown; LoRAs text list with weights; ControlNet input browse field.
- Flags: --model --prompt --negative --seed --steps --sampler --cfg --W --H --outdir --format --prefix --vae --loras --controlnet --controlnet-input --threads --batch --device --precision --init-img --mask --strength.
- Generate builds flags and launches the runner; image is taken from Generated-Output.

## 3) Files Changed

`	xt
modified  src/App.Desktop/Views/ImagesView.xaml
modified  src/App.Desktop/Views/ImagesView.xaml.cs
`

## 4) Per-File Notes

- ImagesView.xaml Added/updated controls; bound sliders to new properties.
- ImagesView.xaml.cs Added properties + BuildNormalizedArgs; one-shot run consumes flags via env EXTRA_ARGS and polls outdir.

## 5) Validation

- Build passed; local smoke: controls render; flag string composition ok.

## 6) Risks / Rollback

- **Risk:** Runners may differ on CLI; flags passed via EXTRA_ARGS mitigate. **Rollback:** revert changes.

