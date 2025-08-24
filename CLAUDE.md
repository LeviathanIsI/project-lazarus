# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Project Lazarus is an All-in-One Windows LLM Application built as a single Windows executable (.exe) that provides a unified interface for chat, training, LoRA management, embeddings, speech/voice integration, image/video generation, and 3D avatars using C# (.NET 8), WPF, and an in-process ASP.NET Core API.

## Build Commands

### Basic Operations

```bash
# Build entire solution
dotnet build ProjectLazarus.sln

# Build specific project
dotnet build App.Desktop/App.Desktop.csproj

# Run Desktop app
dotnet run --project App.Desktop

# Run Orchestrator Host
dotnet run --project App.Orchestrator.Host

# Restore packages
dotnet restore

# Clean build artifacts
dotnet clean

# Run tests
dotnet test
```

### Packaging & Database

```bash
# Package as single executable
dotnet publish App.Desktop/App.Desktop.csproj -c Release -r win-x64 \
  /p:PublishSingleFile=true /p:SelfContained=true /p:PublishTrimmed=true /p:ReadyToRun=true

# EF Core migrations
dotnet ef migrations add <MigrationName> --project App.Data
dotnet ef database update --project App.Data
```

---

## Architecture Overview

Project Lazarus is a .NET 8 multimedia AI platform with a multi-project architecture designed for single-exe packaging. The platform provides comprehensive AI workflows through a unified dark-themed interface.

### Core Projects

**App.Desktop** — WPF desktop client with tabbed multimedia interface

- Uses MVVM pattern with ViewModels and `RelayCommand`
- Communicates with orchestrator via HTTP API at `127.0.0.1:11711`
- Main tabs: Conversations, Model Configuration, Images, Video, 3D Models, Voice, Entities
- Critical Files: `MainWindow.xaml/.cs`, ViewModels for each tab

**App.Orchestrator** — Core orchestration service providing HTTP API

- Hosts ASP.NET Core web API with OpenAI-compatible endpoints
- Manages LLM runners (LLaMA Server, LLaMA.cpp, vLLM, ExLlamaV2)
- Provides model scanning, system info, and chat completion endpoints
- Key endpoints: `/v1/chat/completions`, `/v1/models`, `/v1/system`, `/status`

**App.Orchestrator.Host** — Console host for the orchestrator service  
**App.Backend** — Process orchestration, runners, trainers, audio, avatar  
**App.Data** — EF Core models, migrations, vector storage (SQLite + optional SQLite VSS)  
**App.SDK** — C# client library for desktop-to-API communication  
**App.Shared** — Shared contracts and models

---

## Key UI Components

### Images Tab (Complete Implementation)

- **Text2Image**: Pure prompt-to-pixel generation with parameter controls
- **Image2Image**: Transform existing images with strength controls
- **Inpainting**: Mask-based image editing with blur controls
- **Upscaling**: Resolution enhancement with model selection
- **Layout**: Three-column — Controls | Display | Generation History

### Video Tab (Complete Implementation)

- **Text2Video**: Video generation from text prompts with duration/quality controls
- **Video2Video**: Video transformation with before/after comparison
- **Motion Control**: Camera movement, object tracking, motion path visualization
- **Temporal Effects**: Time stretching, frame interpolation, motion blur, stabilization

### Voice Tab (Complete Implementation)

- **TTS Configuration**: Comprehensive text-to-speech with voice library and waveform visualization
- **Voice Cloning**: Voice training interface with progress tracking and quality settings
- **Real-Time Synthesis**: Live voice synthesis testing
- **Audio Processing**: Audio enhancement and processing tools

### Entities Tab (Complete Implementation — Crown Jewel)

- **Entity Creation**: 3D avatar creation with `Viewport3D`, personality parameters, voice binding
- **Behavioral Patterns**: AI behavior configuration and scripting
- **Interaction Testing**: Real-time entity interaction and testing environment
- **Entity Management**: Entity library and template management
- **Templates**: Virtual Assistant, Academic Expert, Conversation Partner, etc.

### 3D Models Tab (Complete Implementation)

- **Model Browser**: Tree-view file navigation with Maya-style interface
- **3D Viewport**: Native WPF `Viewport3D` with orbit/pan/zoom camera controls
- **Properties Panel**: Transform controls and model metadata
- **Layout**: Three-panel — File Browser | 3D Viewport | Properties

---

## Runner System

Pluggable architecture for different LLM backends:

- `RunnerRegistry`: Manages active runners and switching
- `IChatRunner`: Interface for chat implementations
- `LlamaServerRunner`, `LlamaCppRunner`: Concrete implementations
- Planned: vLLM, ExLlamaV2 runners

## Model Management

Automatic model discovery and inventory:

- `ModelScannerService`: Scans directories for GGUF, SafeTensors files
- Supports base models, LoRAs, VAEs, embeddings, hypernetworks
- Default scan locations: `C:\Models`, `D:\AI\Models`, `%USERPROFILE%\Models`

## API Communication

HTTP client for desktop–orchestrator communication:

- `ApiClient`: Centralized HTTP client for all API calls
- Handles health checks, chat requests, model operations

## Environment Configuration

The orchestrator can be configured via environment variables:

- `LAZARUS_RUNNER_URL`: Base URL for LLM runner
- `LAZARUS_RUNNER_KIND`: Type of runner (`"llama-server"`, `"llama-cpp"`)
- `LAZARUS_RUNNER_NAME`: Display name for the runner
- `LAZARUS_RUNNER_MODEL`: Optional model name/path

---

## Code Style Guidelines

### Framework & Architecture

- **Language**: C# (.NET 8) with nullable reference types enabled
- **UI**: WPF with MVVM pattern using dark gothic theme
- **API**: ASP.NET Core (Kestrel, in-process host)
- **Database**: EF Core + SQLite
- **Architecture**: Clean separation between Desktop (UI), Orchestrator (API), Backend (Logic), Data (EF Core)

### Dark Theme Resources

All UI components use consistent `StaticResource` references:

```xml
<!-- Primary Colors -->
<!-- Main background -->
<SolidColorBrush x:Key="PrimaryDarkBrush" Color="#0f0f0f" />
<!-- Card/panel backgrounds -->
<SolidColorBrush x:Key="SecondaryDarkBrush" Color="#1a1a1a" />
<!-- Input/control backgrounds -->
<SolidColorBrush x:Key="TertiaryDarkBrush" Color="#1e1e1e" />

<!-- Purple Accent Colors -->
<!-- Primary purple -->
<SolidColorBrush x:Key="AccentPurpleBrush" Color="#8b5cf6" />
<!-- Hover state -->
<SolidColorBrush x:Key="AccentPurpleHoverBrush" Color="#7c3aed" />

<!-- Text Colors -->
<!-- Primary white text -->
<SolidColorBrush x:Key="TextPrimaryBrush" Color="#f7fafc" />
<!-- Secondary light text -->
<SolidColorBrush x:Key="TextSecondaryBrush" Color="#e2e8f0" />
<!-- Muted/caption text -->
<SolidColorBrush x:Key="TextMutedBrush" Color="#94a3b8" />
```

### Coding Standards

- **Naming**: PascalCase for public members, camelCase for private fields
- **Async/Await**: Prefer async patterns for all I/O operations
- **Dependency Injection**: Use built-in .NET DI container throughout
- **Error Handling**: Comprehensive try–catch with user-friendly error messages
- **API Compatibility**: Maintain OpenAI-compatible endpoints

---

## Testing Instructions

### Running Tests

```bash
# Unit tests
dotnet test App.Tests/

# Integration tests for API endpoints
dotnet test App.Integration.Tests/
```

### Manual Testing Workflow

1. Start orchestrator host first: `dotnet run --project App.Orchestrator.Host`
2. Test API directly: `curl http://localhost:11711/v1/models`
3. Start desktop client: `dotnet run --project App.Desktop`
4. Desktop client will attempt to connect to orchestrator at startup
5. Verify all tabs load without silent failures
6. Test multimedia workflows in each tab

---

## Repository Etiquette

### Branch Naming

- `feature/voice-synthesis`
- `fix/viewport-binding`
- `refactor/entity-creation`

### Git Workflow

- **Merge Strategy**: Prefer merge commits to preserve feature context
- **Commit Messages**: Conventional commits format (`feat:`, `fix:`, `refactor:`)
- **Code Reviews**: All features require review before merging to `main`
- **Documentation**: Update project README for any architectural changes

---

## Developer Environment Setup

### Prerequisites

- .NET 8 SDK (required for all compilation)
- Visual Studio 2022 or VS Code with C# extension
- Git for version control
- SQLite (bundled with EF Core, no separate install needed)

### External Dependencies (bundled in final .exe)

- `llama-server.exe` (from llama.cpp releases)
- `piper.exe` (for TTS functionality)
- `rhubarb.exe` (for lip sync, future phase)
- Python environment (optional, for vLLM/LLaMA-Factory runners)

### Initial Setup

```bash
git clone <repository>
cd <project-directory>
dotnet restore
dotnet build
```

---

## Critical Implementation Notes

### Silent Startup Failures — Common Causes

- Missing `StaticResource` references (undefined brush/converter keys)
- DI container issues (circular dependencies or missing constructor parameters)
- XAML parsing failures (malformed bindings, complex controls)
- `Viewport3D` initialization failures in 3D tabs

### Resource Reference Patterns

Always use proper `StaticResource` names:

- ✅ `{StaticResource AccentPurpleBrush}`
- ❌ `{StaticResource AccentBrush}`
- ✅ `{StaticResource InvertBool}`
- ❌ `{StaticResource InvertBoolConverter}`

### XAML Layout Patterns

Follow the established three-column layout for multimedia tabs:

```xml
<Grid.ColumnDefinitions>
  <ColumnDefinition Width="300" MinWidth="200"/>  <!-- Controls -->
  <ColumnDefinition Width="*"/>                   <!-- Display -->
  <ColumnDefinition Width="300" MinWidth="200"/>  <!-- History -->
</Grid.ColumnDefinitions>
```

---

## Development Workflow

### Current Implementation Status

- ✅ Complete Multimedia Interface: All 7 tabs implemented with sub-navigation
- ✅ Images Pipeline: Text2Image, Image2Image, Inpainting, Upscaling ready for AI integration
- ✅ Video Pipeline: Comprehensive video generation and transformation interfaces
- ✅ Voice Pipeline: Complete TTS and voice cloning interface scaffolding
- ✅ 3D Pipeline: Native WPF 3D rendering with model loading capabilities
- ✅ Entity System: Advanced entity creation with 3D avatar and behavioral parameters
- ✅ Dark Theme: Consistent gothic aesthetic across all components

### Integration Readiness

The platform is fully scaffolded and ready for AI integration:

- **Stable Diffusion**: Drop into Images tab parameter controls
- **AnimateDiff/SVD**: Integrate with Video tab generation interfaces
- **Piper/XTTS**: Connect to Voice tab TTS configuration
- **LLM Integration**: Entity behavioral patterns ready for language model binding
- **3D Model Loading**: Viewport ready for OBJ/FBX parsing integration

### Future Development Phases

- Phase 2: Neural network integration (Stable Diffusion, TTS engines)
- Phase 3: Advanced entity behaviors and real-time interaction
- Phase 4: Plugin system for extensible AI model support
- Phase 5: Advanced 3D avatar animation and lip sync
- Phase 6: Multi-modal AI interaction combining all components

---

## Project Structure

```text
ProjectLazarus/
  src/
    App.Desktop/                # WPF multimedia client
      Views/
        Images/                 # Image generation interfaces
        Video/                  # Video generation interfaces
        Voice/                  # Voice synthesis interfaces
        Entities/               # Entity creation interfaces
        ThreeDModels/           # 3D model management
      ViewModels/               # MVVM ViewModels for each tab
    App.Orchestrator/           # ASP.NET Core API orchestrator
    App.Backend/                # AI runners and process management
    App.Data/                   # EF Core models and migrations
    App.SDK/                    # Client-server communication
    App.Shared/                 # Shared contracts and utilities
```

This project represents a sophisticated multimedia AI platform that unifies image generation, video synthesis, voice cloning, 3D model management, and entity creation into a single Windows executable. The architecture provides complete scaffolding for advanced AI integrations while maintaining a consistent user experience through dark-themed interfaces.

---

## Debugging Prompts

### 🔧 Silent Startup Failure

The application builds successfully but dies silently on launch.  
Check for:

1. Missing `StaticResource` references in XAML files
2. DI container circular dependencies
3. `Viewport3D` initialization issues in 3D tabs
4. Complex control template failures

Test by removing tabs one at a time to isolate the failure.

### 🎨 UI Implementation

When implementing multimedia interfaces, follow the three-column pattern:

- Left: Parameter controls and settings
- Center: Main display/preview area
- Right: History and management panels

Use consistent dark theme `StaticResource` references and proper MVVM binding.

### 🔗 Integration

For AI service integration:

1. Implement in `App.Backend` as a service
2. Register in DI container
3. Expose via `App.Orchestrator` API endpoints
4. Connect to UI via `App.SDK` `ApiClient`
5. Bind to ViewModels for reactive UI updates

The platform's evolved into a proper multimedia consciousness laboratory. Time to start feeding it neural networks and watch the digital necromancy come alive.
