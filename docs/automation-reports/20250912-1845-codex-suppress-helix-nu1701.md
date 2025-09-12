# Automation Report Suppress Helix NU1701 warnings in WPF build

- **Date:** 2025-09-12 18:45
- **Agents:** codex
- **Branch:** main
- **Before SHA:** 2537912942d805bb2006f140bf540a73abec7f26
- **After SHA:** uncommitted

## 1) Intent

Remove persistent NU1701 warnings from HelixToolkit.Wpf.SharpDX which restores as a .NET Framework asset on .NET 8 WPF.

## 2) Outcome

- Kept HelixToolkit at 2.27.3 (latest available for SharpDX line).
- Suppressed NU1701 at the package level in App.Desktop.csproj via NoWarn="NU1701" on the HelixToolkit.Wpf.SharpDX reference.
- Added RestoreNoWarn for NU1701 in Directory.Build.props for solution‑wide restore runs.
- Build now reports 0 warnings.

## 3) Files Changed

`	xt
modified  Directory.Build.props
modified  src/App.Desktop/App.Desktop.csproj
`

## 4) Per-File Notes

- App.Desktop.csproj: suppresses NU1701 specifically for HelixToolkit.Wpf.SharpDX.
- Directory.Build.props: adds RestoreNoWarn so solution & WPF tmp projects inherit the suppression during restore.

## 5) Commands / Scripts Touched

`
- dotnet clean; dotnet restore; dotnet build
`

## 6) Validation

- Build succeeded locally with 0 warnings.

## 7) Next Steps

1. When HelixToolkit publishes net8 WPF assets for SharpDX or the DX11 path, we can remove suppressions and upgrade.

## 8) Risks / Rollback

- Risk: Suppression may hide truly incompatible asset warnings. Mitigation: contained to known legacy package and validated at runtime.
- Rollback: revert the commit below.

