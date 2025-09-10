# Automation Report Remove --api from image runners; add EXTRA_ARGS

- **Date:** 2025-09-10 08:08
- **Agents:** codex
- **Branch:** main
- **Before SHA:** 2b0ed1a83c083eb1a5dbb8c33669c301dfc0660c
- **After SHA:** uncommitted

## 1) Intent

Fix startup errors from image runners that reject --api and add a flexible way to pass extra args without code changes.

## 2) Outcome

- No longer adds --api for sdwebui/stable-diffusion by default.
- Added LAZARUS_IMAGE_RUNNER_EXTRA_ARGS env var to append arbitrary arguments to the launch command (all entrypoint types).

## 3) Files Changed

`	xt
modified  src/App.Desktop/Views/ImagesView.xaml.cs
`

## 4) Per-File Notes

- BuildLaunchCommand(...): dropped --api tails; appended optional EXTRA_ARGS.

## 5) Validation

- Build succeeded; argument composition verified.

## 6) Risks / Rollback

- **Risk:** None; flags are opt-in. **Rollback:** Reintroduce flags in code or via EXTRA_ARGS.

