# Automation Report Export Settings Button

- **Date:** 2025-09-07 17:02
- **Agents:** codex
- **Branch:** main
- **Before SHA:** 9ff3f6d0e0e08de692a14aa944092afd61d9ce7b
- **After SHA:** uncommitted

## 1) Intent

Add an 'Export settings to JSON' button in Global Actions to save the current settings snapshot to a user-chosen .json file.

## 2) Outcome

- New command wired in GlobalActionsViewModel using SaveFileDialog and System.Text.Json with camelCase + indented output.
- UI button added next to the reset button.

## 3) Files Changed

`	xt
modified  src/App.Desktop/ViewModels/GlobalActionsViewModel.cs
modified  src/App.Desktop/Views/GlobalActionsView.xaml
`"
# Automation Report Export Settings Button  - **Date:** 2025-09-07 17:02 - **Agents:** codex - **Branch:** main - **Before SHA:** 9ff3f6d0e0e08de692a14aa944092afd61d9ce7b - **After SHA:** uncommitted  ## 1) Intent  Add an 'Export settings to JSON' button in Global Actions to save the current settings snapshot to a user-chosen .json file.  ## 2) Outcome  - New command wired in GlobalActionsViewModel using SaveFileDialog and System.Text.Json with camelCase + indented output.
- UI button added next to the reset button.  ## 3) Files Changed  `	xt modified  src/App.Desktop/ViewModels/GlobalActionsViewModel.cs
modified  src/App.Desktop/Views/GlobalActionsView.xaml += "
# Automation Report Export Settings Button  - **Date:** 2025-09-07 17:02 - **Agents:** codex - **Branch:** main - **Before SHA:** 9ff3f6d0e0e08de692a14aa944092afd61d9ce7b - **After SHA:** uncommitted  ## 1) Intent  Add an 'Export settings to JSON' button in Global Actions to save the current settings snapshot to a user-chosen .json file.  ## 2) Outcome  - New command wired in GlobalActionsViewModel using SaveFileDialog and System.Text.Json with camelCase + indented output.
- UI button added next to the reset button.  ## 3) Files Changed  `	xt modified  src/App.Desktop/ViewModels/GlobalActionsViewModel.cs
modified  src/App.Desktop/Views/GlobalActionsView.xaml += 
- src/App.Desktop/ViewModels/GlobalActionsViewModel.cs Add ExportSettingsCommand and serialization logic.
- src/App.Desktop/Views/GlobalActionsView.xaml Add 'Export settings to JSON' button.

## 5) Commands / Scripts Touched
`
- dotnet build Lazarus.sln -c Debug
``"
# Automation Report Export Settings Button  - **Date:** 2025-09-07 17:02 - **Agents:** codex - **Branch:** main - **Before SHA:** 9ff3f6d0e0e08de692a14aa944092afd61d9ce7b - **After SHA:** uncommitted  ## 1) Intent  Add an 'Export settings to JSON' button in Global Actions to save the current settings snapshot to a user-chosen .json file.  ## 2) Outcome  - New command wired in GlobalActionsViewModel using SaveFileDialog and System.Text.Json with camelCase + indented output.
- UI button added next to the reset button.  ## 3) Files Changed  `	xt modified  src/App.Desktop/ViewModels/GlobalActionsViewModel.cs
modified  src/App.Desktop/Views/GlobalActionsView.xaml `"
# Automation Report Export Settings Button  - **Date:** 2025-09-07 17:02 - **Agents:** codex - **Branch:** main - **Before SHA:** 9ff3f6d0e0e08de692a14aa944092afd61d9ce7b - **After SHA:** uncommitted  ## 1) Intent  Add an 'Export settings to JSON' button in Global Actions to save the current settings snapshot to a user-chosen .json file.  ## 2) Outcome  - New command wired in GlobalActionsViewModel using SaveFileDialog and System.Text.Json with camelCase + indented output.
- UI button added next to the reset button.  ## 3) Files Changed  `	xt modified  src/App.Desktop/ViewModels/GlobalActionsViewModel.cs
modified  src/App.Desktop/Views/GlobalActionsView.xaml += "
# Automation Report Export Settings Button  - **Date:** 2025-09-07 17:02 - **Agents:** codex - **Branch:** main - **Before SHA:** 9ff3f6d0e0e08de692a14aa944092afd61d9ce7b - **After SHA:** uncommitted  ## 1) Intent  Add an 'Export settings to JSON' button in Global Actions to save the current settings snapshot to a user-chosen .json file.  ## 2) Outcome  - New command wired in GlobalActionsViewModel using SaveFileDialog and System.Text.Json with camelCase + indented output.
- UI button added next to the reset button.  ## 3) Files Changed  `	xt modified  src/App.Desktop/ViewModels/GlobalActionsViewModel.cs
modified  src/App.Desktop/Views/GlobalActionsView.xaml +=  - src/App.Desktop/ViewModels/GlobalActionsViewModel.cs Add ExportSettingsCommand and serialization logic.
- src/App.Desktop/Views/GlobalActionsView.xaml Add 'Export settings to JSON' button.  ## 5) Commands / Scripts Touched += "
# Automation Report Export Settings Button  - **Date:** 2025-09-07 17:02 - **Agents:** codex - **Branch:** main - **Before SHA:** 9ff3f6d0e0e08de692a14aa944092afd61d9ce7b - **After SHA:** uncommitted  ## 1) Intent  Add an 'Export settings to JSON' button in Global Actions to save the current settings snapshot to a user-chosen .json file.  ## 2) Outcome  - New command wired in GlobalActionsViewModel using SaveFileDialog and System.Text.Json with camelCase + indented output.
- UI button added next to the reset button.  ## 3) Files Changed  `	xt modified  src/App.Desktop/ViewModels/GlobalActionsViewModel.cs
modified  src/App.Desktop/Views/GlobalActionsView.xaml `"
# Automation Report Export Settings Button  - **Date:** 2025-09-07 17:02 - **Agents:** codex - **Branch:** main - **Before SHA:** 9ff3f6d0e0e08de692a14aa944092afd61d9ce7b - **After SHA:** uncommitted  ## 1) Intent  Add an 'Export settings to JSON' button in Global Actions to save the current settings snapshot to a user-chosen .json file.  ## 2) Outcome  - New command wired in GlobalActionsViewModel using SaveFileDialog and System.Text.Json with camelCase + indented output.
- UI button added next to the reset button.  ## 3) Files Changed  `	xt modified  src/App.Desktop/ViewModels/GlobalActionsViewModel.cs
modified  src/App.Desktop/Views/GlobalActionsView.xaml += "
# Automation Report Export Settings Button  - **Date:** 2025-09-07 17:02 - **Agents:** codex - **Branch:** main - **Before SHA:** 9ff3f6d0e0e08de692a14aa944092afd61d9ce7b - **After SHA:** uncommitted  ## 1) Intent  Add an 'Export settings to JSON' button in Global Actions to save the current settings snapshot to a user-chosen .json file.  ## 2) Outcome  - New command wired in GlobalActionsViewModel using SaveFileDialog and System.Text.Json with camelCase + indented output.
- UI button added next to the reset button.  ## 3) Files Changed  `	xt modified  src/App.Desktop/ViewModels/GlobalActionsViewModel.cs
modified  src/App.Desktop/Views/GlobalActionsView.xaml +=  - src/App.Desktop/ViewModels/GlobalActionsViewModel.cs Add ExportSettingsCommand and serialization logic.
- src/App.Desktop/Views/GlobalActionsView.xaml Add 'Export settings to JSON' button.  ## 5) Commands / Scripts Touched += 
- Build succeeded locally
- Save dialog opens and writes a JSON file to chosen path
- Exported JSON shape matches app settings (camelCase)

## 7) Next Steps
1. Add an Import button to load settings from JSON with validation.
2. Optionally include schema version in the filename.

## 8) Risks / Rollback
- **Risk:** User overwrites an existing file. **Mitigation:** SaveFileDialog OverwritePrompt=true.
- **Rollback:** git revert <after_sha> or revert this commit.
