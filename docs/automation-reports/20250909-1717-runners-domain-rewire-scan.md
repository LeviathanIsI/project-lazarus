# Automation Report Runners Domain Rewire (Scanner)

- **Date:** 2025-09-09 17:17
- **Agents:** codex
- **Branch:** main
- **Before SHA:** 90bc8e19f275ee07f9d25df2fbd3b576f90b64b8
- **After SHA:** uncommitted

## Files Changed
```txt
modified  src/App.Desktop/ViewModels/ModelsViewModel.cs
```

## Summary
- Runner discovery now scans domain roots (Runners/Chats) and legacy flat Runners to detect engines (llama.cpp, vllm, exllamav2).
- De-duplicates results and keeps existing exe pattern matching.

## Validation
- Build succeeded; open Models screen and confirm runners under Runners/Chats/* are detected.
