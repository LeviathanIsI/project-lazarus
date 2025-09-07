# Automation Report  Runner discovery: scan Lazarus\Runners

- **Date:** 2025-09-07 11:34
- **Agents:** repo-surgeon
- **Branch:** main
- **Before SHA:** 6ca3db54084ded8faaec9d456f2f5435273e17f7
- **After SHA:** uncommitted

## 1) Intent
Discover runner entrypoints under %LOCALAPPDATA%\Lazarus\Runners\<engine>\** recursively and use them for launching (starting with llama.cpp / llama-server.exe).

## 2) Outcome
- LazarusPaths.Runners added (RootDir + conventional engine folders).
- DirectoryBootstrap creates the Runners root (no engine folders created).
- LlamaCppSupervisor priority updated:
  1. Orchestrator:Runner:BinaryDir (appsettings)
  2. Scan %LOCALAPPDATA%\Lazarus\Runners\llama.cpp\**\llama-server.exe
  3. LAZARUS_BINARIES env (and /runners)
  4. <AppContext.BaseDirectory>/binaries/runners

## 3) Files Changed
`	xt
 M src/App.Desktop/Views/ModelsView.xaml
 M src/App.Orchestrator.Host/Program.cs
 M src/App.Shared/LazarusPaths.cs
`

## 4) Per-File Notes
* src/App.Shared/LazarusPaths.cs  Added Runners tree and included in EnumerateAllDirectories().
* src/App.Orchestrator.Host/Program.cs  Updated supervisor’s executable resolution to scan Lazarus\Runners.

## 5) Commands / Scripts Touched
`
- dotnet build Lazarus.sln -c Debug
`

## 6) Validation
* Build succeeded locally
* When a llama-server.exe is placed under Lazarus\Runners\llama.cpp\..., the supervisor will discover it without additional configuration.

## 7) Next Steps
1. Add scanners for llm and exllamav2 with their entrypoints.
2. Expose /api/runners/engines endpoint to list discovered engines and paths.

## 8) Risks / Rollback
* **Risk:** Recursive scans can be heavy on large trees  **Mitigation:** Cache results and re-scan on demand.
* **Rollback:** git revert 6ca3db54084ded8faaec9d456f2f5435273e17f7 or revert the commit.
