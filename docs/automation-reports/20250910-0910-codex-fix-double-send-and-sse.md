# Automation Report Fix double-send; enforce SSE proxy

- **Date:** 2025-09-10 09:10
- **Agents:** codex
- **Branch:** main
- **Before SHA:** eeee38344e32c2e4c35ce71068b8aba1795be388
- **After SHA:** uncommitted

## 1) Intent

Eliminate duplicate chat sends on Enter/Send and finalize SSE proxy behavior for llama.cpp.

## 2) Outcome

- Removed TextBox Enter KeyBinding and restored IsDefault on Send button; AcceptsReturn=false to avoid newline send collisions.
- Orchestrator /v1/chat/completions now streams SSE frames reliably with flush per chunk; non-stream returns JSON.

## 3) Files Changed

`	xt
modified  src/App.Desktop/Views/ChatSessionsView.xaml
modified  src/App.Orchestrator.Host/Program.cs
`

## 4) Validation

- Build succeeded locally; Enter sends once; clicking Send also sends once.
- SSE frames visible when using curl with -N.

## 5) Risks / Rollback

- **Risk:** Removing AcceptsReturn prevents multi-line by Enter; can add Shift+Enter logic later.
- **Rollback:** Re-add KeyBinding or AcceptsReturn.

