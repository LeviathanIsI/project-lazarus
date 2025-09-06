# Repository Guidelines

## Project Structure & Module Organization
- `src/App.Desktop`: WPF (.NET 8) desktop app — Views (`Views/*`), ViewModels (`ViewModels/*`), Services (`Services/*`), Themes/Resources (`Resources/*`, `Themes/*`), configuration (`appsettings*.json`).
- `src/App.Data`: Data layer with `LazarusDbContext` (EF Core Sqlite) and shared data types.
- `.vscode`: Dev tasks and launch config. Use the "Lazarus Desktop" launch to debug.
- `binaries/`: Large native runners and assets — do not modify in PRs unless necessary.

## Build, Run, and Development
- Build solution: `dotnet build Lazarus.sln`
- Run desktop app: `dotnet run --project src/App.Desktop`
- Live reload: `dotnet watch run --project src/App.Desktop` (or VS Code task: `watch`).
- Debug: VS Code `Run and Debug` → "Lazarus Desktop".
- EF Core (if changing the model): `dotnet ef migrations add <Name>` then `dotnet ef database update` (from `src/App.Data`).

## Coding Style & Naming
- C# 12, file‑scoped namespaces, 4‑space indentation.
- Nullable reference types: enabled. Treat warnings as errors: enabled.
- Naming: PascalCase for public types/members; camelCase for locals/params; private fields prefixed with `_`.
- XAML: Views end with `*View.xaml`; corresponding ViewModels end with `*ViewModel.cs`.
- Keep classes small and DI‑friendly. Place features under the nearest `Services/`, `ViewModels/`, or `Views/` folder.

## Testing Guidelines
- No test project yet. For new features/bugfixes, add `tests/App.Tests` (xUnit) and wire to solution.
  - Create: `dotnet new xunit -n App.Tests -o tests/App.Tests` → `dotnet sln add tests/App.Tests/App.Tests.csproj` → `dotnet test`.
  - Name tests `ClassNameTests.cs`; prefer AAA pattern and deterministic data.

## Commit & Pull Request Guidelines
- Commits are short and imperative; some use a type prefix (e.g., `Fix:`). Prefer Conventional Commits: `feat:`, `fix:`, `docs:`, `refactor:`, etc.
- PRs: clear description, linked issues, before/after screenshots for UI, and notes on config/migrations. Ensure `dotnet build` passes and app boots.

## Security & Configuration
- Do not commit secrets. Use `appsettings.Development.json` for local overrides.
- Avoid committing generated `bin/`, `obj/`, or large binaries changes unless required.
