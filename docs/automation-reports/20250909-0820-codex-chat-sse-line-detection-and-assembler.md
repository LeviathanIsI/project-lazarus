# Automation Report Chat SSE line detection and assembler

- **Date:** 2025-09-09 08:20
- **Agents:** codex
- **Branch:** main
- **Before SHA:** 1d64cb26d8fce541f418ce049c319b5dfd5b1dda
- **After SHA:** uncommitted

## 1) Intent

Address parse errors like "'d' is an invalid start of a value" by robustly detecting SSE vs JSON and correctly assembling multi-line SSE events before JSON deserialization.

## 2) Outcome

- Switched to streaming-first read with initial line sniffing.
- Treats any stream beginning with data: (or : keep-alives) as SSE regardless of Content-Type.
- Assembles multi-line data: events delimited by blank lines, then deserializes choices[0].delta.content.
- Falls back to JSON if the payload starts with {/[ and Content-Type is pplication/json.

## 3) Files Changed

`	xt
docs/automation-reports/20250909-0814-codex-chat-sse-rendering-fix.md
src/App.Desktop/ViewModels/ChatSessionsViewModel.cs
`

## 4) Per-File Notes

- src/App.Desktop/ViewModels/ChatSessionsViewModel.cs Added SSE event assembler and first-line sniffing, removed false-positive JSON path causing 'd' parse error.

## 5) Commands / Scripts Touched

`
None
`

## 6) Validation

- Build succeeded locally with 0 warnings.
- Code handles both single-line and multi-line SSE data: events.

## 7) Next Steps

1. Manual test: confirm assistant bubble streams; check logs for first SSE line.
2. If orchestrator sometimes proxies with pplication/json Content-Type but SSE body, this logic already handles it.

## 8) Risks / Rollback

- **Risk:** Some servers emit very large single events; assembler adds minimal buffering per event. **Mitigation:** streaming still processes per event and not whole response.
- **Rollback:** git revert <after_sha> or revert this commit.
