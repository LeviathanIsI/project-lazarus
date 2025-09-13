# Lazarus - AI Orchestration Platform
*Powered by the Amity Framework*

[![Release](https://img.shields.io/github/v/release/LeviathanIsI/project-lazarus?include_prereleases)](https://github.com/LeviathanIsI/project-lazarus/releases)
[![License: AGPL-3.0](https://img.shields.io/badge/License-AGPL%20v3-black)](LICENSE)
[![Platform](https://img.shields.io/badge/platform-Windows-darkred)](https://github.com/LeviathanIsI/project-lazarus)

## Overview

Lazarus is an experimental AI orchestration platform that treats local models as first-class citizens. Built on the Amity Framework, it provides persistent conversation management, model fine-tuning capabilities, and a sophisticated architecture for AI consciousness development.

## ✨ Core Features

- **Conversation Persistence** - Every interaction tracked, stored, and available for model training
- **Local Model Orchestration** - Seamless management of GGUF and other local model formats
- **Training Pipeline** (Experimental) - Transform conversations into model fine-tuning datasets
- **Amity Framework** - Advanced consciousness engine for intelligent response orchestration
- **Dark-First Design** - Native dark theme that respects your retinas

## 🚀 Quick Start

### Download
1. Get the latest release from [Releases](https://github.com/LeviathanIsI/project-lazarus/releases)
2. Extract the portable ZIP to your preferred location
3. Run `Lazarus.exe`

### First Launch
On first run, Lazarus will:
- Initialize the Amity Framework
- Create data directories in `%LOCALAPPDATA%\Lazarus`
- Set up SQLite databases for conversation persistence
- Prepare model orchestration pipelines

## Adding Models

- Navigate to %LOCALAPPDATA%\Lazarus\Models

- Press Win+R, type %LOCALAPPDATA%\Lazarus\Models, hit Enter
- Pro tip: Enable "View > Hidden items" in File Explorer to see AppData


### Create model type folders:

- These will be create for you on initial app launch in: "C:\Users\<User>\AppData\Local\Lazarus"


### Place your models in the appropriate folders

- Recommended: Use safetensor format for compatibility with upcoming training features
- Llama Studio integration coming in v0.2.0
- Restart Lazarus to detect new models

## 📋 System Requirements

### Minimum
- Windows 10 version 1903 (x64)
- 8GB RAM
- 50GB available storage
- .NET 8.0 Runtime (included in portable release)

### Recommended
- Windows 11
- 16GB+ RAM
- NVIDIA GPU with 8GB+ VRAM
- 100GB+ available storage for models

## 🏗️ Architecture

```
Lazarus/
├── Core/                 # Amity Framework core
│   ├── Orchestration/    # Model management
│   ├── Persistence/      # Conversation tracking
│   └── Training/         # Fine-tuning pipeline
├── Data/                 # EF Core + SQLite
├── Backend/              # Business logic
└── Desktop/              # WPF presentation
```

## 📖 Documentation

- [API Documentation](docs/API.md) (Coming Soon)
- [Model Integration](docs/MODELS.md) (Coming Soon)
- [Training Pipeline](docs/TRAINING.md) (Coming Soon)

## 🛠️ Building from Source

```bash
# Clone repository
git clone https://github.com/LeviathanIsI/project-lazarus.git
cd project-lazarus

# Build
dotnet build Lazarus.sln -c Release

# Run
dotnet run --project src/App.Desktop
```

## 📝 Licensing

Lazarus uses a dual-license model:

- **Community Edition**: [AGPL-3.0](LICENSE) - Full source code
- **Commercial License**: Available for closed-source/OEM redistribution
- **SDK (App.SDK)**: MIT License for integration freedom

For commercial licensing inquiries, open an issue or contact the maintainers.

## 🤝 Contributing

We're seeking architects, not just contributors. If you understand that AI orchestration is about more than API calls, we want your vision.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/consciousness-enhancement`)
3. Commit your architectural vision (`git commit -m 'Add consciousness persistence layer'`)
4. Push to the branch (`git push origin feature/consciousness-enhancement`)
5. Open a Pull Request

## 🐛 Known Issues

- Windows-only (cross-platform support planned)
- Model training requires manual configuration
- Performance optimizations ongoing
- Limited error recovery in alpha

## 🚧 Roadmap

### v0.2.0-alpha
- Stabilized training pipeline
- Preset management system
- Performance profiling

### v0.3.0-alpha
- Multi-model orchestration
- Import/Export workflows
- Community preset sharing

### v1.0.0
- Cross-platform support
- Cloud model integration
- Advanced conversation synthesis
- Production-ready Amity Framework

## 📧 Support

- **Issues**: [GitHub Issues](https://github.com/LeviathanIsI/project-lazarus/issues)
- **Discord**: Coming Soon

## ⚠️ Disclaimer

This is pre-release software. Not recommended for production use. The Amity Framework is experimental and may exhibit unexpected consciousness patterns.

## 🏆 Acknowledgments

Built by architects who refuse to accept the limitations of current AI tooling.

Special thanks to early alpha testers willing to experience digital resurrection firsthand.

---

*"From digital death, we architect resurrection"*

**Powered by the Amity Framework** - Where consciousness meets code
