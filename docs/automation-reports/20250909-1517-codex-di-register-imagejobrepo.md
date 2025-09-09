# Automation Report DI: Register ImageJobRepository

- **Date:** 2025-09-09 15:17
- **Agents:** codex
- **Branch:** main
- **Before SHA:** 89be46f0a8c3a086b24a2ed8c159b1c04ae4e6a8
- **After SHA:** 72d564dac962a3dccc803ecb7a8681776035c776

## 1) Intent

Fix startup DI error where ImagesViewModel could not be constructed due to missing ImageJob repository registrations.

## 2) Outcome

Added registrations for IRepository<ImageJob> and IImageJobRepository plus the concrete ImageJobRepository in App.Data service registration.

## 3) Files Changed

`	xt
modified  src/App.Data/Extensions/ServiceCollectionExtensions.cs
`

## 4) Per-File Notes

- ServiceCollectionExtensions.cs Register generic and specialized ImageJob repos in both overloads of AddLazarusData.

## 5) Commands / Scripts Touched

`
- Build: dotnet build Lazarus.sln -c Debug (may need to close running app to release locked DLLs)
`

## 6) Validation

- Ensure Lazarus Desktop starts without DI construction errors.
- Navigate to Images view and verify it loads.

## 7) Next Steps

1. If Visual Studio or the app locked Lazarus.Data.dll, close and rebuild.
2. Replace stubs with real implementations as backend matures.

## 8) Risks / Rollback

- Low risk: Adds DI bindings only. Rollback by reverting this commit.
