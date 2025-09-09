# Automation Report Chats: file-based fallback + hydrate

- **Date:** 2025-09-09 13:14
- **Agents:** codex
- **Branch:** main
- **Before SHA:** f4d66b4ec5d197d9af660ef63af4a58e063adba3
- **After SHA:** uncommitted

## 1) Intent

Make the ChatSessions list robust: if DB isn't ready or empty at first launch, still populate from JSON files, and hydrate the DB when possible.

## 2) Outcome

- GetAllAsync: if DB returns 0 and import to DB yields 0, return conversations parsed directly from JSON files.
- GetMessagesAsync: if DB returns 0 or fails, read messages directly from the conversation JSON file.
- Keeps prior change to retry DB each call; clears fallback on success.

## 3) Files Changed

`	xt
modified  src/App.Desktop/Services/ChatService.cs
`

## 4) Per-File Notes

- Added LoadConversationsFromFilesAsync and LoadMessagesFromFileAsync helpers.

## 5) Commands / Scripts Touched

`
None
`

## 6) Validation

- Build successful; list should populate even without DB.

## 7) Next Steps

- Option: add a one-time banner to prompt importing file-only conversations into DB.

## 8) Risks / Rollback

- Low risk and self-contained.
- Rollback via revert.
