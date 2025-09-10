# Automation Report Remove duplicate userText append in BuildRequestMessages

- **Date:** 2025-09-10 09:41
- **Agents:** codex
- **Branch:** main
- **Before SHA:** 785f9eec0ae8db952f86c979371f8b39d3b7a8f3
- **After SHA:** uncommitted

## 1) Intent

Ensure the chat request payload does not re-append the current user prompt; SendMessageAsync already inserts it into Messages.

## 2) Outcome

- Deleted the conditional block that added { role: "user", content: userText } in BuildRequestMessages.
- Payload now reflects Messages history only (excluding streaming placeholder), eliminating duplicate prompt bugs in the right panel.

## 3) Files Changed

`	xt
modified  src/App.Desktop/ViewModels/ChatSessionsViewModel.cs
`

## 4) Per-File Notes

- ChatSessionsViewModel.cs In BuildRequestMessages, iterate over Messages.Where(!IsStreaming) only; removed extra user append.

## 5) Commands / Scripts Touched

`
None
`

## 6) Validation

- Build: dotnet build src/App.Desktop/App.Desktop.csproj -c Debug -p:OutDir=bin2/ → Success (0 errors).
- Behavior: User message appears once; assistant streams normally; no duplicate prompt in payload/UI.

## 7) Next Steps

1. If any service expects the current userText outside history, document that history now includes it pre-request.

## 8) Risks / Rollback

- Risk: If a future refactor delays adding the user message to Messages before request build, the user prompt might be missing from the payload.
- Mitigation: Keep SendMessageAsync inserting prior to building the request; add unit tests when test suite exists.
- Rollback: git revert <after_sha> or revert this commit.
