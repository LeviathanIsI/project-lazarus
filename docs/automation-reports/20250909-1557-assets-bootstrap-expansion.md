# Automation Report Assets Bootstrap Expansion

- **Date:** 2025-09-09 15:57
- **Agents:** codex
- **Branch:** main
- **Before SHA:** 171d70c697b75f989232d4113874e3edb074c783
- **After SHA:** uncommitted

## Added Directories

```txt
Generation-Assets/Style-Presets/LoRAs
Generation-Assets/Style-Presets/Embeddings
Generation-Assets/Style-Presets/Hypernetworks
Video-Assets
Video-Assets/AnimateDiff
Video-Assets/Temporal-LoRAs
Video-Assets/Video-ControlNet
Video-Assets/Frame-Interpolators
Audio-Assets
Audio-Assets/ASR-Models
Audio-Assets/TTS-Voices
Audio-Assets/Voice-Cloning
Audio-Assets/VAD
Audio-Assets/Noise-Reduction
Avatar-Assets
Avatar-Assets/3D-Models
Avatar-Assets/Rigs
Avatar-Assets/Textures
Avatar-Assets/Visemes
RAG-Assets
RAG-Assets/Indexes
RAG-Assets/Documents
RAG-Assets/Presets
Datasets
Datasets/Conversations
Datasets/Images
Datasets/Video
Datasets/Audio
Presets
Presets/Image
Presets/Video
Presets/Audio
```

## Modified Files
```txt
modified  src/App.Shared/LazarusPaths.cs
modified  src/App.Shared/DirectoryBootstrap.cs
modified  src/App.Desktop/Views/ImagesView.xaml
modified  src/App.Desktop/Views/ImagesView.xaml.cs
```

## Validation
- Build succeeded; app launches.
- Bootstrap logs Created/Exists for new folders (via FileSystemBootstrapService).
- ImagesView shows LoRA / Embedding / Hypernetwork selectors; legacy flat Style-Presets still probed as fallback.
- ControlNet/Upscaler/VAE selectors unchanged.

## Back-Compat Notes
- Legacy Generation-Assets/Style-Presets is retained and used as a fallback when new subfolders are empty.
- New helper LazarusPaths.ResolveFirstExisting() picks the first existing path and creates the preferred if none exist.

## Files Changed (git status)
```txt
 D src/App.Desktop/ViewModels/ImageLabViewModel.cs
 D src/App.Desktop/Views/ImageLabView.xaml
 M src/App.Desktop/Views/ImagesView.xaml
 M src/App.Desktop/Views/ImagesView.xaml.cs
 M src/App.Shared/DirectoryBootstrap.cs
 M src/App.Shared/LazarusPaths.cs
?? src/App.Desktop/ViewModels/ImagesViewModel.cs
```
