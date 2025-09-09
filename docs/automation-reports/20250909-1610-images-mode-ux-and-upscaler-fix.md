# Automation Report Images Mode UX + Upscaler Filter

- **Date:** 2025-09-09 16:10
- **Agents:** codex
- **Branch:** main
- **Before SHA:** 9edfd864e602b72d5f8ffc9dd9ccc32df593adfa
- **After SHA:** uncommitted

## Intent
Improve mode segmented control UX and ensure Upscaler ComboBox recognizes .safetensors files.

## Outcome
- Mode buttons now highlight only the selected item using Tag-coupled MultiDataTriggers; reduced height and padding.
- Upscaler filter includes .safetensors so files like remacri_original.safetensors appear.

## Files Changed
```txt
modified  src/App.Desktop/Views/ImagesView.xaml
modified  src/App.Desktop/Views/ImagesView.xaml.cs
```

## Validation
- Build succeeded; UI renders; only the active mode shows filled styling.
- Upscaler ComboBox lists .safetensors upscalers and can be selected.
