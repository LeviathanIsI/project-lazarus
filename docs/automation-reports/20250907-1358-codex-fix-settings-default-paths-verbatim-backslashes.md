# Automation Report Fix settings default paths (verbatim backslashes)

- **Date:** 2025-09-07 13:58
- **Agents:** codex
- **Branch:** main
- **Before SHA:** bcb79ff69cf026ee9aeac295dcf248e17efe50ae
- **After SHA:** uncommitted

## 1) Intent

Align AppSettings defaults with C# verbatim path syntax (single backslashes) per design snippet.

## 2) Outcome

- Changed ModelsDirectory to @"D:\\models" -> @"D:\models"
- Changed CacheDirectory to @"%LOCALAPPDATA%\\Lazarus\\cache" -> @"%LOCALAPPDATA%\Lazarus\cache"

## 3) Files Changed

```txt
modified  src/App.Shared/Settings/SettingsSchema.cs
```

## 4) Per-File Notes

- src/App.Shared/Settings/SettingsSchema.cs Defaults corrected to match C# verbatim expectations.

## 5) Commands / Scripts Touched

```
None
```

## 6) Validation

- Build succeeded locally

## 7) Next Steps

1. Continue filling out AppSettings per full clipboard spec when provided.

## 8) Risks / Rollback

- **Risk:** None.
