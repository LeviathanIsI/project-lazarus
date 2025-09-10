# Automation Report Images: runner registry scan + img2img/inpaint + ControlNet

- **Date:** 2025-09-10 14:15
- **Agents:** codex
- **Branch:** main
- **Before SHA:** c2bb6faa735c7937afa2c3bd03827d7689e2b807
- **After SHA:** uncommitted

## 1) Intent

Populate RunnerRegistry from disk to list Image runners, and extend ImageGen pipeline to support img2img/inpaint and basic ControlNet input.

## 2) Outcome

- RunnerRegistry scans LazarusPaths.Runners for stable-diffusion/sdwebui/comfyui/invokeai, builds RunnerDescriptor with ExecPath and default BaseUrl (127.0.0.1:port).
- ImageGenRequest adds InitImagePath/MaskImagePath/Strength/ControlNetImagePath.
- SD service posts img2img or txt2img appropriately, uploads init/mask, and includes a minimal ControlNet alwayson_scripts payload when provided.
- VM passes Init/Mask/Strength/ControlNet to request and ControlNet input set via the view.

## 3) Files Changed
```txt
modified  src/App.Backend/Services/Runners/RunnerRegistry.cs
modified  src/App.Shared/Contracts/ImageGenRequest.cs
modified  src/App.Backend/Services/ImageGen/StableDiffusionImageGenService.cs
modified  src/App.Desktop/ViewModels/ImagesViewModel.cs
modified  src/App.Desktop/Views/ImagesView.xaml.cs
```

## 6) Validation
- Build succeeded locally
- Registry returns runners when folders exist; VM shows runners; Generate with img2img/inpaint and ControlNet produces requests accordingly.
