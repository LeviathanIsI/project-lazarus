# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Project Lazarus is an All-in-One Windows LLM Application built as a **single Windows executable (.exe)** that provides a unified interface for chat, training, LoRA management, embeddings, speech/voice integration, and 3D avatars using C# (.NET 8), WPF, and an in-process ASP.NET Core API.

## Build Commands

### Basic Operations

- **Build entire solution**: `dotnet build ProjectLazarus.sln`
- **Build specific project**: `dotnet build App.Desktop/App.Desktop.csproj`
- **Run Desktop app**: `dotnet run --project App.Desktop`
- **Run Orchestrator Host**: `dotnet run --project App.Orchestrator.Host`
- **Restore packages**: `dotnet restore`
- **Clean build artifacts**: `dotnet clean`
- **Run tests**: `dotnet test`

### Packaging & Database

```bash
# Package as single executable
dotnet publish App.Desktop/App.Desktop.csproj -c Release -r win-x64  /p:PublishSingleFile=true /p:SelfContained=true /p:PublishTrimmed=true /p:ReadyToRun=true

# EF Core migrations
dotnet ef migrations add <MigrationName> --project App.Data
dotnet ef database update --project App.Data
```

## Architecture Overview

Project Lazarus is a .NET 8 LLM application with a multi-project architecture designed for eventual single-exe packaging:

### Core Projects

**App.Desktop**: WPF desktop client with tabbed interface (Chat, Models)

- Uses MVVM pattern with ViewModels and RelayCommand
- Communicates with orchestrator via HTTP API at 127.0.0.1:11711
- Main views: ChatView, ModelsView
- Critical Files: MainWindow.xaml/.cs, ChatViewModel.cs

**App.Orchestrator**: Core orchestration service providing HTTP API

- Hosts ASP.NET Core web API with OpenAI-compatible endpoints
- Manages LLM runners (LLaMA Server, LLaMA.cpp, vLLM, ExLlamaV2)
- Provides model scanning, system info, and chat completion endpoints
- Key endpoints: /v1/chat/completions, /v1/models, /v1/system, /status
- Critical Files: OrchestratorHost.cs

**App.Orchestrator.Host**: Console host for the orchestrator service

- Simple console entry point that starts the orchestrator web service

**App.Backend**: Process orchestration, runners, trainers, audio, avatar

- Critical Files: LlamaCppRunner.cs, runner implementations

**App.Data**: EF Core models, migrations, vector storage (SQLite + optional SQLite VSS)

**App.SDK**: C# client library for desktop-to-API communication

- Critical Files: ApiClient.cs

**App.Shared**: Shared contracts and models

- Contains OpenAI-compatible request/response models in OpenAIContracts.cs

### Key Components

**Runner System**: Pluggable architecture for different LLM backends

- RunnerRegistry: Manages active runners and switching
- IChatRunner: Interface for chat implementations
- LlamaServerRunner, LlamaCppRunner: Concrete implementations
- Planned: vLLM, ExLlamaV2 runners

**Model Management**: Automatic model discovery and inventory

- ModelScannerService: Scans directories for GGUF, SafeTensors files
- Supports base models, LoRAs, VAEs, embeddings, hypernetworks
- Default scan locations: C:\Models, D:\AI\Models, %USERPROFILE%\Models

**API Communication**: HTTP client for desktop-orchestrator communication

- ApiClient: Centralized HTTP client for all API calls
- Handles health checks, chat requests, model operations

**Training Integration (Future)**:

- LLaMA-Factory: General LoRA/DPO/ORPO training
- Axolotl: Advanced configs, distributed setups
- Unsloth: Optimized LoRA/QLoRA for smaller VRAM

**Audio & Voice Pipeline (Future)**:

- ASR: faster-whisper subprocess
- TTS: Piper integration (voice models bundled)
- Lip Sync: Rhubarb for viseme generation

**3D Avatar System (Future)**:

- Rendering: HelixToolkit.Wpf.SharpDX
- Animation: Driven by TTS + viseme timing data

## Environment Configuration

The orchestrator can be configured via environment variables:

- `LAZARUS_RUNNER_URL`: Base URL for LLM runner
- `LAZARUS_RUNNER_KIND`: Type of runner ("llama-server", "llama-cpp")
- `LAZARUS_RUNNER_NAME`: Display name for the runner
- `LAZARUS_RUNNER_MODEL`: Optional model name/path

## Code Style Guidelines

**Framework & Architecture**

- Language: C# (.NET 8) with nullable reference types enabled
- UI: WPF with MVVM pattern
- API: ASP.NET Core (Kestrel, in-process host)
- Database: EF Core + SQLite
- Architecture: Clean separation between Desktop (UI), Orchestrator (API), Backend (Logic), Data (EF Core)

**Coding Standards**

- Naming: PascalCase for public members, camelCase for private fields
- Async/Await: Prefer async patterns for all I/O operations
- Dependency Injection: Use built-in .NET DI container throughout
- Error Handling: Comprehensive try-catch with user-friendly error messages
- API Compatibility: Maintain OpenAI-compatible endpoints

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
5. Verify chat functionality end-to-end

## Repository Etiquette

**Branch Naming**

- feature/chat-streaming
- fix/runner-lifecycle
- refactor/di-setup

**Git Workflow**

- Merge Strategy: Prefer merge commits to preserve feature context
- Commit Messages: Conventional commits format (feat:, fix:, refactor:)
- Code Reviews: All features require review before merging to main
- Documentation: Update project README for any architectural changes

## Developer Environment Setup

**Prerequisites**

- .NET 8 SDK (required for all compilation)
- Visual Studio 2022 or VS Code with C# extension
- Git for version control
- SQLite (bundled with EF Core, no separate install needed)

**External Dependencies (bundled in final .exe)**

- llama-server.exe (from llama.cpp releases)
- piper.exe (for TTS functionality)
- rhubarb.exe (for lip sync, future phase)
- Python environment (optional, for vLLM/LLaMA-Factory runners)

**Initial Setup**

```bash
git clone <repository>
cd <project-directory>
dotnet restore
dotnet build
```

## Unexpected Behaviors & Warnings

**Critical Issues**

- Hardcoded API URL: ApiClient defaults to localhost:11711 - orchestrator must start before desktop UI
- Subprocess Management: Runner processes may not terminate cleanly on app exit - implement proper disposal
- Single-File Publishing: Native binaries (llama-server.exe, piper.exe) need special handling in publish process
- EF Core: SQLite migrations require manual intervention for complex schema changes
- WPF Threading: UI updates from async operations must use Dispatcher.Invoke()

**Performance Considerations**

- Memory Usage: Large language models consume significant RAM - implement memory monitoring
- Startup Time: First-run extraction of bundled binaries can cause delays
- Vector Storage: SQLite VSS performance degrades with large embedding collections

## Development Workflow

**Current MVP Focus (v0.1)**

- Core Chat Pipeline: Desktop UI → SDK → Orchestrator → llama.cpp Runner → Response streaming
- Model Management: Basic model loading and runner assignment
- Training Integration: Job queuing for LLaMA-Factory (subprocess execution)
- TTS Toggle: Piper integration for text-to-speech

**Development Process**

1. Start orchestrator host first: `dotnet run --project App.Orchestrator.Host`
2. Start desktop client: `dotnet run --project App.Desktop`
3. Desktop client will attempt to connect to orchestrator at startup
4. All chat interactions flow through the orchestrator's OpenAI-compatible API

**Future Phases**

- Phase 2: Add Unsloth + LoRA merge utilities
- Phase 3: Embeddings + RAG (SQLite VSS)
- Phase 4: ASR integration (faster-whisper)
- Phase 5: 3D avatar with live lip sync
- Phase 6: Full plugin ecosystem

**Known Technical Debt**

- Error handling needs improvement across subprocess communications
- Logging infrastructure not fully implemented
- Configuration management scattered across projects
- Unit test coverage insufficient for runner implementations
- Plugin system architecture planned but not implemented (uses AssemblyLoadContext for hot-loading)

## Project Structure

```
ProjectLazarus/
  src/
    App.Desktop/                # WPF shell (.exe)
    App.Orchestrator/           # ASP.NET Core OpenAI-compatible API
    App.Orchestrator.Host/      # Console host for orchestrator
    App.Backend/                # Process orchestration, runners, trainers, audio, avatar
    App.Data/                   # EF Core models, migrations, vector storage
    App.SDK/                    # C# client library
    App.Shared/                 # shared enums, contracts, utilities
```

This project represents a sophisticated attempt to unify the fragmented LLM tooling landscape into a single, user-friendly Windows executable. The architecture is sound, but the complexity of managing multiple subprocess runners while maintaining a responsive UI requires careful attention to async patterns and proper resource disposal.

## Prompts

### 🔧 Debugging

You are acting as a senior C#/.NET engineer.  
I will paste compiler or runtime errors from Project Lazarus.  
Explain the cause of the issue step-by-step, then show a corrected version of the affected file(s).  
Never remove unrelated code. Provide fully updated code blocks ready to paste back into the repo.

### 🚀 Feature Implementation

You are working on Project Lazarus, a WPF + ASP.NET Core app.  
When I describe a new feature, generate the minimal set of updated files needed to implement it.  
Use MVVM best practices for WPF and clean architecture for services.  
Always give me the full file contents, not diffs or snippets.

### 🧹 Refactor

Look at this file from Project Lazarus.  
Refactor for readability and maintainability without altering external behavior.  
Ensure it follows MVVM and .NET conventions.  
Provide the full updated file.

### 🔗 Integration

I want to connect [component A] with [component B].  
Generate the orchestration code that handles API routing, dependency injection, and lifecycle.  
Follow the existing project structure:

- App.Desktop (UI)
- App.Orchestrator (API)
- App.Backend (services)
- App.Data (EF Core)

### 📖 Documentation

Generate clear, concise technical documentation for this module in markdown.  
Explain purpose, key classes/methods, and how it integrates with the rest of Project Lazarus.  
Target audience: developers adding new runners/trainers or debugging the orchestrator.
