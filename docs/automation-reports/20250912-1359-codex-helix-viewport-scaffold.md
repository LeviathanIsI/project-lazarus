# Automation Report Scaffold Helix viewport (DX11)

- **Date:** 2025-09-12 13:59
- **Agents:** codex
- **Branch:** usage: git [-v | --version] [-h | --help] [-C <path>] [-c <name>=<value>] [--exec-path[=<path>]] [--html-path] [--man-path] [--info-path] [-p | --paginate | -P | --no-pager] [--no-replace-objects] [--no-lazy-fetch] [--no-optional-locks] [--no-advice] [--bare] [--git-dir=<path>] [--work-tree=<path>] [--namespace=<name>] [--config-env=<name>=<envvar>] <command> [<args>]  These are common Git commands used in various situations:  start a working area (see also: git help tutorial) clone      Clone a repository into a new directory init       Create an empty Git repository or reinitialize an existing one  work on the current change (see also: git help everyday) add        Add file contents to the index mv         Move or rename a file, a directory, or a symlink restore    Restore working tree files rm         Remove files from the working tree and from the index  examine the history and state (see also: git help revisions) bisect     Use binary search to find the commit that introduced a bug diff       Show changes between commits, commit and working tree, etc grep       Print lines matching a pattern log        Show commit logs show       Show various types of objects status     Show the working tree status  grow, mark and tweak your common history backfill   Download missing objects in a partial clone branch     List, create, or delete branches commit     Record changes to the repository merge      Join two or more development histories together rebase     Reapply commits on top of another base tip reset      Reset current HEAD to the specified state switch     Switch branches tag        Create, list, delete or verify a tag object signed with GPG  collaborate (see also: git help workflows) fetch      Download objects and refs from another repository pull       Fetch from and integrate with another repository or a local branch push       Update remote refs along with associated objects  'git help -a' and 'git help -g' list available subcommands and some concept guides. See 'git help <command>' or 'git help <concept>' to read about a specific subcommand or concept. See 'git help git' for an overview of the system.
- **Before SHA:** usage: git [-v | --version] [-h | --help] [-C <path>] [-c <name>=<value>] [--exec-path[=<path>]] [--html-path] [--man-path] [--info-path] [-p | --paginate | -P | --no-pager] [--no-replace-objects] [--no-lazy-fetch] [--no-optional-locks] [--no-advice] [--bare] [--git-dir=<path>] [--work-tree=<path>] [--namespace=<name>] [--config-env=<name>=<envvar>] <command> [<args>]  These are common Git commands used in various situations:  start a working area (see also: git help tutorial) clone      Clone a repository into a new directory init       Create an empty Git repository or reinitialize an existing one  work on the current change (see also: git help everyday) add        Add file contents to the index mv         Move or rename a file, a directory, or a symlink restore    Restore working tree files rm         Remove files from the working tree and from the index  examine the history and state (see also: git help revisions) bisect     Use binary search to find the commit that introduced a bug diff       Show changes between commits, commit and working tree, etc grep       Print lines matching a pattern log        Show commit logs show       Show various types of objects status     Show the working tree status  grow, mark and tweak your common history backfill   Download missing objects in a partial clone branch     List, create, or delete branches commit     Record changes to the repository merge      Join two or more development histories together rebase     Reapply commits on top of another base tip reset      Reset current HEAD to the specified state switch     Switch branches tag        Create, list, delete or verify a tag object signed with GPG  collaborate (see also: git help workflows) fetch      Download objects and refs from another repository pull       Fetch from and integrate with another repository or a local branch push       Update remote refs along with associated objects  'git help -a' and 'git help -g' list available subcommands and some concept guides. See 'git help <command>' or 'git help <concept>' to read about a specific subcommand or concept. See 'git help git' for an overview of the system.
- **After SHA:** uncommitted

## 1) Intent
Switch preview to HelixToolkit.Wpf.SharpDX (DX11) and keep build green within current warnings-as-errors policy.

## 2) Outcome
Added HelixToolkit packages and suppression for NU1701; attempted code integration but upstream TFM/namespace conflicts prevented a clean compile. Reverted view to stable WPF preview while leaving Helix packages referenced for a follow-up branch. Build passes.

## 3) Files Changed
```txt
modified  src/App.Backend/Services/ConversationTrainingService.cs
modified  src/App.Desktop/App.Desktop.csproj
modified  src/App.Desktop/Resources/Themes/DarkTheme.xaml
deleted  src/App.Desktop/Services/ISystemMetricsService.cs
deleted  src/App.Desktop/Services/SystemMetricsService.cs
modified  src/App.Desktop/ViewModels/Training/ConversationsDesignerViewModel.cs
modified  src/App.Desktop/ViewModels/Training/DesignProgressViewModel.cs
modified  src/App.Desktop/Views/ThreeDModelsView.xaml
modified  src/App.Desktop/Views/ThreeDModelsView.xaml.cs
modified  src/App.Desktop/Views/Training/ConversationsDesignerView.xaml
modified  src/App.Desktop/Views/Training/DesignProgressView.xaml
modified  src/App.Shared/Models/Training/TrainingConfiguration.cs
modified  ".sln -c Debug"
modified  "ignProgressView.xaml\357\200\272 removed System Resources card."
modified  "k\357\200\272\357\200\252\357\200\252 None"
modified  "locally after removal."
modified  "service and interface."
modified  "tem Resources UI and all system polling from DesignProgress."
modified  "\357\200\272\357\200\252\357\200\252"
```

## 4) Per-File Notes
- src/App.Desktop/App.Desktop.csproj Added HelixToolkit.Wpf.SharpDX and Core + NU1701 suppression; upgraded AssimpNet to 5.0.0-beta1.
- src/App.Desktop/Views/ThreeDModelsView.xaml Restored WPF Viewport3D after testing Helix host. 
- src/App.Desktop/Views/ThreeDModelsView.xaml.cs Restored stable loader; left Helix path commented conceptually for next work.

## 5) Commands / Scripts Touched
```
 dotnet add src/App.Desktop package HelixToolkit.Wpf.SharpDX
 dotnet add src/App.Desktop package HelixToolkit.SharpDX.Core
 dotnet add src/App.Desktop package HelixToolkit.SharpDX.Assimp
 dotnet build Lazarus.sln -c Debug
```

## 6) Validation
- Build succeeded locally
- FBX preview remains via Assimp/WPF path

## 7) Next Steps
1. Option A: Create net48 WPF plug-in (Helix host) and embed via WindowsFormsHost; communicate over IPC for selection state.
2. Option B: Relax NU1701 further and pin HelixToolkit 2.27.3 across packages, then reintroduce Viewport3DX with Phong pipeline.
3. Option C: Swap to `HelixToolkit.Wpf` (non-DX11) as an intermediate camera/control upgrade if DX11 remains blocked.

## 8) Risks / Rollback
- Risk: HelixToolkit SharpDX is netframework-targeted; compatibility relies on NuGet fallback. 
- Rollback: remove Helix packages and NU1701 suppression from App.Desktop.csproj.
