# Automation Report Remove userText params; de-dup prompt in payload

- **Date:** 2025-09-10 09:45
- **Agents:** codex
- **Branch:** main
- **Before SHA:** 40e4bc20c3a750975f17aa5767b89aa6633a28b2
- **After SHA:** uncommitted

## 1) Intent

Eliminate duplicate user message in chat payload by removing userText parameter from BuildRequest, BuildRequestMessages, and StreamAssistantAsync, relying solely on Messages.

## 2) Outcome

- StreamAssistantAsync() now has no parameter and calls BuildRequest().
- BuildRequest() consumes BuildRequestMessages() without parameters.
- BuildRequestMessages() iterates only Messages.Where(!IsStreaming) and does not append userText.
- SendMessageAsync() now calls StreamAssistantAsync().

## 3) Files Changed

`	xt
modified  src/App.Desktop/ViewModels/ChatSessionsViewModel.cs
`

## 4) Per-File Notes

- ChatSessionsViewModel.cs Signature updates + call site changes; no other logic altered.

## 5) Commands / Scripts Touched

`
None
`

## 6) Validation

- Build: dotnet build src/App.Desktop/App.Desktop.csproj -c Debug -p:OutDir=bin2/ → Success (0 errors, 0 warnings).
- Payload inspection: Only one copy of the user message appears (history already includes it; no extra append).

## 7) Next Steps

1. Optional: add a unit test when tests exist to assert payload contains a single user entry for the latest prompt.

## 8) Risks / Rollback

- Risk: Future refactor could build the request before the user message is added. Mitigation: keep insertion order (add to Messages before building request).
- Rollback: git revert <after_sha> or revert this commit.
