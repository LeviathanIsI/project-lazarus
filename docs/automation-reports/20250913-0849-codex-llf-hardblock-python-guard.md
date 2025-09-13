# Automation Report LF: Hard Block + Python Guard

- **Date:** 2025-09-13 08:49
- **Agents:** codex
- **Branch:** main
- **Before SHA:** 53c0754b9935ab80bad1fa61e70832314f4fc472
- **After SHA:** uncommitted

## 1) Intent

Block Create/Start on LLaMAFactory errors (e.g., GGUF), and add a Python 3.12+ guard prior to starting.

## 2) Outcome

- ConversationsDesignerViewModel:
  -  now returns false when Trainer=LLaMAFactory and status=Error.
  -  checks LF status and Python 3.12+ before invoking the service.
  - Python version detection using Python 3.12.0 or ; sets status error on failure.

## 3) Files Changed



## 4) Validation

- Build succeeded; Start is blocked if GGUF model chosen or Python <3.12/not found.

## 5) Next Steps

1. Add a small inline callout next to the status banner with a link to Python 3.12 installer if desired.

## 6) Risks / Rollback

- **Risk:** Python detection may be slow on some machines; timeout is 2s.
- **Rollback:** .
