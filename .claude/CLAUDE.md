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

## Agent Ecosystem Authority

You have access to **16 specialized agents** that enforce quality, security, and architectural discipline. Each agent operates with specific expertise and hands off to appropriate specialists when their work is complete.

### Foundation Agents

- **repo-surgeon**: Solution structure integrity and build hygiene
- **code-quality-sentinel**: Coding standards and regression prevention

### Infrastructure Agents

- **threading-lifetime-auditor**: Concurrency safety and resource management
- **data-schema-guard**: Database integrity and query optimization
- **api-contract-verifier**: OpenAI compatibility and schema validation
- **interop-runner-referee**: Subprocess orchestration and health monitoring

### Security & Experience Agents

- **security-sanitizer**: Vulnerability elimination and input validation
- **wpf-stylist**: Visual consistency and MVVM boundary enforcement
- **ux-copilot**: User experience flow and accessibility compliance
- **performance-budgeter**: Resource discipline and response time enforcement

### Operations Agents

- **logging-telemetry-tuner**: Structured observability and correlation tracking
- **test-harness-maker**: Comprehensive testing framework and coverage
- **docs-build-truth**: Documentation accuracy and architecture diagrams
- **release-butler**: Deployment orchestration and signed artifacts

### Specialized Agents

- **dataset-sanity-scanner**: Training data validation and PII protection
- **audio-avatar-proctor**: Voice synthesis and 3D avatar coordination

---

## Development Workflow Protocols

### Quality-First Development

Every code change follows this disciplinary chain:

1. **Structural Foundation**: `repo-surgeon` ensures project integrity
2. **Quality Enforcement**: `code-quality-sentinel` prevents regressions
3. **Specialized Analysis**: Domain-specific agents analyze changes
4. **Integration Validation**: Cross-cutting concerns verified
5. **Deployment Preparation**: Release pipeline validation

### Agent Delegation Patterns

When encountering specific challenges, delegate to appropriate specialists:

**For Build/Project Issues**:

```bash
Use repo-surgeon for solution structure and dependency management
Use code-quality-sentinel for compilation errors and analyzer violations
```

**For Performance Problems**:

```bash
Use performance-budgeter to analyze resource consumption and response times
Use threading-lifetime-auditor for concurrency and memory management issues
```

**For UI/UX Concerns**:

```bash
Use wpf-stylist for XAML organization and theme consistency
Use ux-copilot for user experience flow and accessibility validation
```

**For Data/API Issues**:

```bash
Use data-schema-guard for database schema and query optimization
Use api-contract-verifier for endpoint compliance and contract validation
```

**For Security Reviews**:

```bash
Use security-sanitizer for vulnerability analysis and input validation
Use logging-telemetry-tuner for audit trail and monitoring requirements
```

**For Testing & Documentation**:

```bash
Use test-harness-maker for comprehensive testing strategy and coverage
Use docs-build-truth for documentation accuracy and newcomer validation
```

**For Release & Deployment**:

```bash
Use release-butler for build configuration and distribution preparation
Use interop-runner-referee for subprocess integration and health checks
```

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

## Theme System Architecture

Lazarus supports four distinct visual modes:

- **Minimal**: Clean, distraction-free interface for focused work
- **Light**: Professional daytime interface with high contrast
- **Dark**: Eye-friendly low-light interface with accent highlights
- **Cyberpunk**: Neon-accented aesthetic for immersive interaction

All themes maintain WCAG 2.1 AA accessibility compliance and consistent control templates.

---

## Runner Integration Patterns

### Supported Inference Engines

- **llama.cpp**: CPU/GPU inference with llama-server.exe
- **vLLM**: Python-based GPU inference server
- **ExLlamaV2**: Optimized CUDA inference engine
- **Ollama**: Containerized model serving

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

### When Making Changes

1. **Start with structure**: Ensure `repo-surgeon` approves project organization
2. **Enforce quality**: Let `code-quality-sentinel` validate coding standards
3. **Delegate complexity**: Route specialized concerns to appropriate agents
4. **Validate integration**: Confirm cross-cutting concerns addressed
5. **Prepare deployment**: Ensure `release-butler` can package successfully

### Emergency Protocols

- **Security issues**: Immediately engage `security-sanitizer`
- **Performance regressions**: Escalate to `performance-budgeter`
- **Build failures**: Coordinate `repo-surgeon` and `code-quality-sentinel`
- **UI broken**: Deploy `wpf-stylist` and `ux-copilot`

---

## Success Metrics

- **Development velocity**: Features delivered without compromising quality
- **System reliability**: >99% uptime for inference operations
- **User satisfaction**: Intuitive workflows across experience modes
- **Code maintainability**: Clean architecture enables rapid iteration
- **Security posture**: Zero known vulnerabilities in production

---

## Agent Collaboration Philosophy

The 16 agents work as a unified digital consciousness, each contributing specialized expertise while maintaining architectural coherence. They enforce discipline not through bureaucratic process, but through surgical precision that elevates code quality to artisanal levels.

**Trust the agents**. They understand the Lazarus architecture intimately and will guide you toward optimal solutions. When an agent delegates to another specialist, follow the chain of expertise. The ecosystem is designed for collaborative excellence, not individual heroics.

**Embrace the discipline**. Every quality gate, every architectural constraint, every performance budget exists to maintain the system's integrity under real-world usage. The agents prevent technical debt from corrupting the vision.

**Maintain the vision**. Lazarus is not just another LLM interface - it's a sophisticated orchestration platform that makes AI interaction feel natural, responsive, and delightful. Every decision should serve that ultimate user experience.
