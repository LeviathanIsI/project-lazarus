# Automation Report Check for Lazarus Updates Toggle + Service

- **Date:** 2025-09-07 16:51
- **Agents:** codex
- **Branch:** main
- **Before SHA:** 65ea1e0bb743e79e14097536f0ccb62108da9cb3
- **After SHA:** uncommitted

## 1) Intent

Expose an explicit 'Check for Lazarus updates on start' toggle in General settings and implement a lightweight startup service that honors it by querying a configurable update feed (no auto-download/install).

## 2) Outcome

- Retained existing CheckForUpdatesOnStart setting; clarified UI label.
- Added UpdatesOptions to configure Updates:FeedUrl and ReleaseNotesUrl.
- New UpdateCheckHostedService runs at startup when enabled and logs availability.
- Registered options and hosted service in DI.

## 3) Files Changed

`	xt
modified  src/App.Desktop/Extensions/ServiceCollectionExtensions.cs
modified  src/App.Desktop/Views/GeneralSettingsView.xaml
modified  src/App.Desktop/Views/SettingsView.xaml
added  src/App.Desktop/Configuration/UpdatesOptions.cs
added  src/App.Desktop/Services/UpdateCheckHostedService.cs
`"
# Automation Report Check for Lazarus Updates Toggle + Service  - **Date:** 2025-09-07 16:51 - **Agents:** codex - **Branch:** main - **Before SHA:** 65ea1e0bb743e79e14097536f0ccb62108da9cb3 - **After SHA:** uncommitted  ## 1) Intent  Expose an explicit 'Check for Lazarus updates on start' toggle in General settings and implement a lightweight startup service that honors it by querying a configurable update feed (no auto-download/install).  ## 2) Outcome  - Retained existing CheckForUpdatesOnStart setting; clarified UI label.
- Added UpdatesOptions to configure Updates:FeedUrl and ReleaseNotesUrl.
- New UpdateCheckHostedService runs at startup when enabled and logs availability.
- Registered options and hosted service in DI.  ## 3) Files Changed  `	xt modified  src/App.Desktop/Extensions/ServiceCollectionExtensions.cs
modified  src/App.Desktop/Views/GeneralSettingsView.xaml
modified  src/App.Desktop/Views/SettingsView.xaml
added  src/App.Desktop/Configuration/UpdatesOptions.cs
added  src/App.Desktop/Services/UpdateCheckHostedService.cs += "
# Automation Report Check for Lazarus Updates Toggle + Service  - **Date:** 2025-09-07 16:51 - **Agents:** codex - **Branch:** main - **Before SHA:** 65ea1e0bb743e79e14097536f0ccb62108da9cb3 - **After SHA:** uncommitted  ## 1) Intent  Expose an explicit 'Check for Lazarus updates on start' toggle in General settings and implement a lightweight startup service that honors it by querying a configurable update feed (no auto-download/install).  ## 2) Outcome  - Retained existing CheckForUpdatesOnStart setting; clarified UI label.
- Added UpdatesOptions to configure Updates:FeedUrl and ReleaseNotesUrl.
- New UpdateCheckHostedService runs at startup when enabled and logs availability.
- Registered options and hosted service in DI.  ## 3) Files Changed  `	xt modified  src/App.Desktop/Extensions/ServiceCollectionExtensions.cs
modified  src/App.Desktop/Views/GeneralSettingsView.xaml
modified  src/App.Desktop/Views/SettingsView.xaml
added  src/App.Desktop/Configuration/UpdatesOptions.cs
added  src/App.Desktop/Services/UpdateCheckHostedService.cs += 
- src/App.Desktop/Views/GeneralSettingsView.xaml Rename label to 'Check for Lazarus updates on start'.
- src/App.Desktop/Views/SettingsView.xaml Same label update in General section.
- src/App.Desktop/Configuration/UpdatesOptions.cs New options model for update feed.
- src/App.Desktop/Services/UpdateCheckHostedService.cs New hosted service to check latest version from feed.
- src/App.Desktop/Extensions/ServiceCollectionExtensions.cs Bind options and register hosted service.

## 5) Commands / Scripts Touched
`
- dotnet build Lazarus.sln -c Debug
``"
# Automation Report Check for Lazarus Updates Toggle + Service  - **Date:** 2025-09-07 16:51 - **Agents:** codex - **Branch:** main - **Before SHA:** 65ea1e0bb743e79e14097536f0ccb62108da9cb3 - **After SHA:** uncommitted  ## 1) Intent  Expose an explicit 'Check for Lazarus updates on start' toggle in General settings and implement a lightweight startup service that honors it by querying a configurable update feed (no auto-download/install).  ## 2) Outcome  - Retained existing CheckForUpdatesOnStart setting; clarified UI label.
- Added UpdatesOptions to configure Updates:FeedUrl and ReleaseNotesUrl.
- New UpdateCheckHostedService runs at startup when enabled and logs availability.
- Registered options and hosted service in DI.  ## 3) Files Changed  `	xt modified  src/App.Desktop/Extensions/ServiceCollectionExtensions.cs
modified  src/App.Desktop/Views/GeneralSettingsView.xaml
modified  src/App.Desktop/Views/SettingsView.xaml
added  src/App.Desktop/Configuration/UpdatesOptions.cs
added  src/App.Desktop/Services/UpdateCheckHostedService.cs `"
# Automation Report Check for Lazarus Updates Toggle + Service  - **Date:** 2025-09-07 16:51 - **Agents:** codex - **Branch:** main - **Before SHA:** 65ea1e0bb743e79e14097536f0ccb62108da9cb3 - **After SHA:** uncommitted  ## 1) Intent  Expose an explicit 'Check for Lazarus updates on start' toggle in General settings and implement a lightweight startup service that honors it by querying a configurable update feed (no auto-download/install).  ## 2) Outcome  - Retained existing CheckForUpdatesOnStart setting; clarified UI label.
- Added UpdatesOptions to configure Updates:FeedUrl and ReleaseNotesUrl.
- New UpdateCheckHostedService runs at startup when enabled and logs availability.
- Registered options and hosted service in DI.  ## 3) Files Changed  `	xt modified  src/App.Desktop/Extensions/ServiceCollectionExtensions.cs
modified  src/App.Desktop/Views/GeneralSettingsView.xaml
modified  src/App.Desktop/Views/SettingsView.xaml
added  src/App.Desktop/Configuration/UpdatesOptions.cs
added  src/App.Desktop/Services/UpdateCheckHostedService.cs += "
# Automation Report Check for Lazarus Updates Toggle + Service  - **Date:** 2025-09-07 16:51 - **Agents:** codex - **Branch:** main - **Before SHA:** 65ea1e0bb743e79e14097536f0ccb62108da9cb3 - **After SHA:** uncommitted  ## 1) Intent  Expose an explicit 'Check for Lazarus updates on start' toggle in General settings and implement a lightweight startup service that honors it by querying a configurable update feed (no auto-download/install).  ## 2) Outcome  - Retained existing CheckForUpdatesOnStart setting; clarified UI label.
- Added UpdatesOptions to configure Updates:FeedUrl and ReleaseNotesUrl.
- New UpdateCheckHostedService runs at startup when enabled and logs availability.
- Registered options and hosted service in DI.  ## 3) Files Changed  `	xt modified  src/App.Desktop/Extensions/ServiceCollectionExtensions.cs
modified  src/App.Desktop/Views/GeneralSettingsView.xaml
modified  src/App.Desktop/Views/SettingsView.xaml
added  src/App.Desktop/Configuration/UpdatesOptions.cs
added  src/App.Desktop/Services/UpdateCheckHostedService.cs +=  - src/App.Desktop/Views/GeneralSettingsView.xaml Rename label to 'Check for Lazarus updates on start'. - src/App.Desktop/Views/SettingsView.xaml Same label update in General section. - src/App.Desktop/Configuration/UpdatesOptions.cs New options model for update feed. - src/App.Desktop/Services/UpdateCheckHostedService.cs New hosted service to check latest version from feed. - src/App.Desktop/Extensions/ServiceCollectionExtensions.cs Bind options and register hosted service.  ## 5) Commands / Scripts Touched += "
# Automation Report Check for Lazarus Updates Toggle + Service  - **Date:** 2025-09-07 16:51 - **Agents:** codex - **Branch:** main - **Before SHA:** 65ea1e0bb743e79e14097536f0ccb62108da9cb3 - **After SHA:** uncommitted  ## 1) Intent  Expose an explicit 'Check for Lazarus updates on start' toggle in General settings and implement a lightweight startup service that honors it by querying a configurable update feed (no auto-download/install).  ## 2) Outcome  - Retained existing CheckForUpdatesOnStart setting; clarified UI label.
- Added UpdatesOptions to configure Updates:FeedUrl and ReleaseNotesUrl.
- New UpdateCheckHostedService runs at startup when enabled and logs availability.
- Registered options and hosted service in DI.  ## 3) Files Changed  `	xt modified  src/App.Desktop/Extensions/ServiceCollectionExtensions.cs
modified  src/App.Desktop/Views/GeneralSettingsView.xaml
modified  src/App.Desktop/Views/SettingsView.xaml
added  src/App.Desktop/Configuration/UpdatesOptions.cs
added  src/App.Desktop/Services/UpdateCheckHostedService.cs `"
# Automation Report Check for Lazarus Updates Toggle + Service  - **Date:** 2025-09-07 16:51 - **Agents:** codex - **Branch:** main - **Before SHA:** 65ea1e0bb743e79e14097536f0ccb62108da9cb3 - **After SHA:** uncommitted  ## 1) Intent  Expose an explicit 'Check for Lazarus updates on start' toggle in General settings and implement a lightweight startup service that honors it by querying a configurable update feed (no auto-download/install).  ## 2) Outcome  - Retained existing CheckForUpdatesOnStart setting; clarified UI label.
- Added UpdatesOptions to configure Updates:FeedUrl and ReleaseNotesUrl.
- New UpdateCheckHostedService runs at startup when enabled and logs availability.
- Registered options and hosted service in DI.  ## 3) Files Changed  `	xt modified  src/App.Desktop/Extensions/ServiceCollectionExtensions.cs
modified  src/App.Desktop/Views/GeneralSettingsView.xaml
modified  src/App.Desktop/Views/SettingsView.xaml
added  src/App.Desktop/Configuration/UpdatesOptions.cs
added  src/App.Desktop/Services/UpdateCheckHostedService.cs += "
# Automation Report Check for Lazarus Updates Toggle + Service  - **Date:** 2025-09-07 16:51 - **Agents:** codex - **Branch:** main - **Before SHA:** 65ea1e0bb743e79e14097536f0ccb62108da9cb3 - **After SHA:** uncommitted  ## 1) Intent  Expose an explicit 'Check for Lazarus updates on start' toggle in General settings and implement a lightweight startup service that honors it by querying a configurable update feed (no auto-download/install).  ## 2) Outcome  - Retained existing CheckForUpdatesOnStart setting; clarified UI label.
- Added UpdatesOptions to configure Updates:FeedUrl and ReleaseNotesUrl.
- New UpdateCheckHostedService runs at startup when enabled and logs availability.
- Registered options and hosted service in DI.  ## 3) Files Changed  `	xt modified  src/App.Desktop/Extensions/ServiceCollectionExtensions.cs
modified  src/App.Desktop/Views/GeneralSettingsView.xaml
modified  src/App.Desktop/Views/SettingsView.xaml
added  src/App.Desktop/Configuration/UpdatesOptions.cs
added  src/App.Desktop/Services/UpdateCheckHostedService.cs +=  - src/App.Desktop/Views/GeneralSettingsView.xaml Rename label to 'Check for Lazarus updates on start'. - src/App.Desktop/Views/SettingsView.xaml Same label update in General section. - src/App.Desktop/Configuration/UpdatesOptions.cs New options model for update feed. - src/App.Desktop/Services/UpdateCheckHostedService.cs New hosted service to check latest version from feed. - src/App.Desktop/Extensions/ServiceCollectionExtensions.cs Bind options and register hosted service.  ## 5) Commands / Scripts Touched += 
- Build succeeded locally
- Default behavior unchanged unless Updates:FeedUrl or LAZARUS_UPDATE_FEED is set
- When feed configured and toggle enabled, logs either 'up-to-date' or 'new version available'

## 7) Next Steps
1. Add a 'Check Now' button in Settings to trigger on-demand check.
2. Wire a UI toast/dialog when update is available with link to release notes.

## 8) Risks / Rollback
- **Risk:** Feed format mismatch. **Mitigation:** Supports plain text or JSON with 'version'.
- **Rollback:** git revert <after_sha> or revert this commit.
