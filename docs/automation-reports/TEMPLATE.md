# Automation Report <Task Title>

- **Date:** <YYYY-MM-DD HH:mm>
- **Agents:** <codex >
- **Branch:** <branch-or-unknown>
- **Before SHA:** <before-or-uncommitted>
- **After SHA:** <after-or-uncommitted>

## 1) Intent

<One paragraph stating the goal and scope of the run>

## 2) Outcome

<What changed; note any deviations from plan>

## 3) Files Changed

```txt
<added|modified|deleted|renamed>  <relative/path>
```

## 4) Per-File Notes

- `<relative/path>` <1 line summary>
- `<relative/path>` <1 line summary>

## 5) Commands / Scripts Touched

```
<list any new or changed commands, tasks, scripts, or config flags>
```

## 6) Validation

- Build succeeded locally
- App launched
- Feature verified: <bullet list>
- Evidence: <paths to screenshots/logs if any>

## 7) Next Steps

1. <Actionable follow-up w/ owner if known>
2. <Actionable follow-up>

## 8) Risks / Rollback

- **Risk:** <short description> **Mitigation:** <how to mitigate>
- **Rollback:** `git revert <after_sha>` or revert the commit(s) that introduced these changes.
