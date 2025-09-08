# CLAUDE.md - Lazarus Development Authority

This file provides guidance to Claude Code when working with the Lazarus WPF application stack. You are operating within a specialized ecosystem of 16 disciplined agents that enforce architectural excellence across every layer of development.

---

## Project Architecture Overview

**Lazarus** is a sophisticated WPF desktop application for LLM inference management with MVVM architecture, SQLite storage, and comprehensive theming. The system orchestrates subprocess runners (llama.cpp, vLLM, ExLlamaV2) through an ASP.NET Core API while maintaining a responsive desktop interface.

### Core Technology Stack

- **.NET 8** target framework across all projects
- **WPF with MVVM** for desktop interface architecture
- **ASP.NET Core** orchestrator API for runner coordination
- **Entity Framework Core** with SQLite for data persistence
- **Dependency injection** throughout all layers
- **Multi-theme system** (Minimal, Light, Dark, Cyberpunk)

### Project Structure

```
src/
├── App.Desktop/           # WPF MVVM application
├── App.Orchestrator/      # ASP.NET Core API
├── App.Orchestrator.Host/ # Hosting and configuration
├── App.Shared/            # Cross-cutting utilities
├── App.Data/              # EF Core models and migrations
└── App.SDK/               # Public API surface
```

---

## Development Workflow Protocols

- **Check to see if code or files already exist**: It is imperative you make sure code, functions, services, files, etc. don't already exist before creating new data.

### Quality-First Development

---

## Architectural Principles

### MVVM Discipline

- **Strict separation**: No business logic in Views or code-behind
- **Command binding**: All user interactions through ICommand implementations
- **Data binding**: Two-way binding with proper change notification
- **Dependency injection**: ViewModels receive services through DI container

### Performance Standards

- **Memory discipline**: Proper disposal patterns and resource management
- **UI responsiveness**: 60 FPS rendering with <16ms frame times
- **Startup performance**: Application ready in <5 seconds
- **Query optimization**: Database operations <100ms P95

### Security Requirements

- **Input validation**: All user input sanitized before processing
- **Process isolation**: Runner subprocesses properly sandboxed
- **Secrets management**: No hardcoded credentials or API keys
- **Local-only binding**: Network endpoints restricted to localhost

### Code Quality Gates

- **Zero warnings**: Release builds must compile without warnings
- **Test coverage**: >80% line coverage for business logic
- **Documentation**: XML docs for all public APIs
- **Static analysis**: Full compliance with configured analyzers

---

## Runner Integration Patterns

### Supported Inference Engines

- **llama.cpp**: CPU/GPU inference with llama-server.exe
- **vLLM**: Python-based GPU inference server
- **ExLlamaV2**: Optimized CUDA inference engine

### Health Check Requirements

- **Startup validation**: 30-second timeout for runner initialization
- **Continuous monitoring**: Health checks every 10 seconds
- **Graceful shutdown**: Proper cleanup with 10-second termination timeout
- **Error recovery**: Automatic restart with exponential backoff

---

## Database Schema Standards

### Entity Framework Configuration

```csharp
// Required DbContext configuration
protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
{
    optionsBuilder.UseSqlite(connectionString);
    optionsBuilder.EnableSensitiveDataLogging(false);
    optionsBuilder.EnableServiceProviderCaching();
}
```

### Migration Discipline

- **Schema validation**: Runtime model matches latest migration
- **Index optimization**: Performance-critical indices maintained
- **Vector storage**: Embedding dimensions properly aligned
- **Connection pooling**: Efficient resource utilization

---

## Testing Strategy

### Test Categories

- **Unit Tests**: Business logic and utility functions
- **Integration Tests**: Database operations and API endpoints
- **UI Tests**: WPF automation and visual regression
- **Performance Tests**: Load testing and resource validation

### Coverage Requirements

- **Minimum threshold**: 80% line coverage
- **Critical path coverage**: 100% for user workflows
- **Performance validation**: Response time budgets enforced
- **Security testing**: Input validation and injection prevention

---

## Release Pipeline

### Build Configuration

```xml
<PropertyGroup Condition="'$(Configuration)' == 'Release'">
  <PublishSingleFile>true</PublishSingleFile>
  <SelfContained>true</SelfContained>
  <PublishTrimmed>true</PublishTrimmed>
  <ReadyToRun>true</ReadyToRun>
</PropertyGroup>
```

### Quality Gates

1. **Clean build**: Zero warnings, full analyzer compliance
2. **Test validation**: Complete test suite passes
3. **Security scan**: No vulnerable dependencies
4. **Performance baseline**: Resource budgets maintained
5. **Asset validation**: All native dependencies included

---

## Development Guidelines

---

## Success Metrics

- **Development velocity**: Features delivered without compromising quality
- **System reliability**: >99% uptime for inference operations
- **User satisfaction**: Intuitive workflows across experience modes
- **Code maintainability**: Clean architecture enables rapid iteration
- **Security posture**: Zero known vulnerabilities in production

---

**Embrace the discipline**. Every quality gate, every architectural constraint, every performance budget exists to maintain the system's integrity under real-world usage. The agents prevent technical debt from corrupting the vision.

**Maintain the vision**. Lazarus is not just another LLM interface - it's a sophisticated orchestration platform that makes AI interaction feel natural, responsive, and delightful. Every decision should serve that ultimate user experience.
