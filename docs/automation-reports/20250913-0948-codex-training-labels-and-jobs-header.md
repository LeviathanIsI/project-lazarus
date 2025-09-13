# Automation Report Training Labels + Jobs Header

- **Date:** 2025-09-13 09:48
- **Agents:** codex
- **Branch:** main
- **Before SHA:** 27e2b2607f51b7c64a324963bf0af905699cdbfe
- **After SHA:** uncommitted

## 1) Intent

Clarify UI labeling so users understand the left panel lists jobs and the right inspector shows training details; rename dataset header.

## 2) Outcome

- Left header for Conversations: "Training Jobs" (was "Conversation Models").
- Right header for Conversations: "Training Details" (was "Conversation Details").
- Middle-left header: "Training Datasets" (was "Conversation Datasets").

## 3) Files Changed



## 4) Validation

- Desktop build succeeded; headers update immediately.

## 5) Next Steps

1. Optionally add a short blurb in the left panel explaining selection controls.

## 6) Risks / Rollback

- **Risk:** None (copy-only change).
- **Rollback:** .
