# Automation Report Make runner startup timeout configurable

- **Date:** 2025-09-07 13:33
- **Agents:** codex
- **Branch:** usage: git [-v | --version] [-h | --help] [-C <path>] [-c <name>=<value>]            [--exec-path[=<path>]] [--html-path] [--man-path] [--info-path]            [-p | --paginate | -P | --no-pager] [--no-replace-objects] [--no-lazy-fetch]            [--no-optional-locks] [--no-advice] [--bare] [--git-dir=<path>]            [--work-tree=<path>] [--namespace=<name>] [--config-env=<name>=<envvar>]            <command> [<args>]  These are common Git commands used in various situations:  start a working area (see also: git help tutorial)    clone      Clone a repository into a new directory    init       Create an empty Git repository or reinitialize an existing one  work on the current change (see also: git help everyday)    add        Add file contents to the index    mv         Move or rename a file, a directory, or a symlink    restore    Restore working tree files    rm         Remove files from the working tree and from the index  examine the history and state (see also: git help revisions)    bisect     Use binary search to find the commit that introduced a bug    diff       Show changes between commits, commit and working tree, etc    grep       Print lines matching a pattern    log        Show commit logs    show       Show various types of objects    status     Show the working tree status  grow, mark and tweak your common history    backfill   Download missing objects in a partial clone    branch     List, create, or delete branches    commit     Record changes to the repository    merge      Join two or more development histories together    rebase     Reapply commits on top of another base tip    reset      Reset current HEAD to the specified state    switch     Switch branches    tag        Create, list, delete or verify a tag object signed with GPG  collaborate (see also: git help workflows)    fetch      Download objects and refs from another repository    pull       Fetch from and integrate with another repository or a local branch    push       Update remote refs along with associated objects  'git help -a' and 'git help -g' list available subcommands and some concept guides. See 'git help <command>' or 'git help <concept>' to read about a specific subcommand or concept. See 'git help git' for an overview of the system.
- **Before SHA:** usage: git [-v | --version] [-h | --help] [-C <path>] [-c <name>=<value>]            [--exec-path[=<path>]] [--html-path] [--man-path] [--info-path]            [-p | --paginate | -P | --no-pager] [--no-replace-objects] [--no-lazy-fetch]            [--no-optional-locks] [--no-advice] [--bare] [--git-dir=<path>]            [--work-tree=<path>] [--namespace=<name>] [--config-env=<name>=<envvar>]            <command> [<args>]  These are common Git commands used in various situations:  start a working area (see also: git help tutorial)    clone      Clone a repository into a new directory    init       Create an empty Git repository or reinitialize an existing one  work on the current change (see also: git help everyday)    add        Add file contents to the index    mv         Move or rename a file, a directory, or a symlink    restore    Restore working tree files    rm         Remove files from the working tree and from the index  examine the history and state (see also: git help revisions)    bisect     Use binary search to find the commit that introduced a bug    diff       Show changes between commits, commit and working tree, etc    grep       Print lines matching a pattern    log        Show commit logs    show       Show various types of objects    status     Show the working tree status  grow, mark and tweak your common history    backfill   Download missing objects in a partial clone    branch     List, create, or delete branches    commit     Record changes to the repository    merge      Join two or more development histories together    rebase     Reapply commits on top of another base tip    reset      Reset current HEAD to the specified state    switch     Switch branches    tag        Create, list, delete or verify a tag object signed with GPG  collaborate (see also: git help workflows)    fetch      Download objects and refs from another repository    pull       Fetch from and integrate with another repository or a local branch    push       Update remote refs along with associated objects  'git help -a' and 'git help -g' list available subcommands and some concept guides. See 'git help <command>' or 'git help <concept>' to read about a specific subcommand or concept. See 'git help git' for an overview of the system.
- **After SHA:** uncommitted

## 1) Intent

Eliminate hard-coded 30s runner startup timeout in the orchestrator host by making it configurable and increasing the default, to prevent premature failures during heavy model initialization.

## 2) Outcome

- Introduced configuration-driven startup timeout in 'LlamaCppSupervisor' with multiple parsing paths and a safe default of 2 minutes.
- Added 'Orchestrator:Runner:StartupTimeout' to orchestrator appsettings.json with value '00:02:00'.
- Build succeeded; no functional changes outside the orchestrator host.

## 3) Files Changed

```txt
modified  rc/App.Orchestrator.Host/Program.cs
modified  rc/App.Orchestrator.Host/appsettings.json
modified  temp_tail.txt
```

## 4) Per-File Notes

- src/App.Orchestrator.Host/Program.cs Read startup timeout from configuration and environment; default 2 minutes; log the wait duration.
- src/App.Orchestrator.Host/appsettings.json Added 'StartupTimeout' under 'Orchestrator:Runner'.

## 5) Commands / Scripts Touched

```
Config keys:
- Orchestrator:Runner:StartupTimeout (TimeSpan, e.g., 00:02:00)
- Orchestrator:Runner:StartupTimeoutMs (int milliseconds, optional)
- LAZARUS_RUNNER_STARTUP_TIMEOUT (environment, seconds, optional)
```

## 6) Validation

- Build succeeded locally
- Health polling now respects configured timeout
- Evidence: Orchestrator logs include the line 'Waiting up to {Timeout} for runner health'

## 7) Next Steps

1. Tune the default timeout per hardware/model; consider exposing in Desktop UI.
2. Optionally wire DB settings provider if centralizing runner settings in App.Data is desired.

## 8) Risks / Rollback

- **Risk:** Longer timeout can delay error surfacing. **Mitigation:** Keep per-request health check timeout short; allow cancellation.
- **Rollback:** git revert <after_sha> or revert the commit introducing these changes.

