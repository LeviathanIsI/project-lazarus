# Automation Report Desktop-style fallbacks for timeout/port

- **Date:** 2025-09-07 13:41
- **Agents:** codex
- **Branch:** main
- **Before SHA:** 2656e3c972a806958d3398e2d158638b1e959b0f
- **After SHA:** uncommitted

## 1) Intent

Ensure orchestrator can optionally read Desktop-style keys (Runners:LlamaCpp:StartupTimeout/DefaultPort) as fallbacks to reduce confusion.

## 2) Outcome

- GetStartupTimeout now checks 'Runners:LlamaCpp:StartupTimeout' and '*Ms' as fallbacks.
- Port resolver checks 'Runners:LlamaCpp:DefaultPort' as a fallback.
- Build succeeded.

## 3) Files Changed

```txt
modified  src/App.Orchestrator.Host/Program.cs
```

## 4) Per-File Notes

- src/App.Orchestrator.Host/Program.cs Add Desktop-style config fallbacks for timeout and port.

## 5) Commands / Scripts Touched

```
Fallback keys (optional):
- Runners:LlamaCpp:StartupTimeout (TimeSpan)
- Runners:LlamaCpp:StartupTimeoutMs (milliseconds)
- Runners:LlamaCpp:DefaultPort (int)
```

## 6) Validation

- Build succeeded locally

## 7) Next Steps

1. If desired, mirror Desktop config into the Host's appsettings to keep behavior consistent.

## 8) Risks / Rollback

- **Risk:** None; only used if keys present.
