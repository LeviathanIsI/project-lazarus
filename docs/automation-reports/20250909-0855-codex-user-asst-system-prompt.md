# Automation Report Add Chat Persona + System Prompt

- **Date:** 2025-09-09 08:55
- **Agents:** codex
- **Branch:** main
- **Before SHA:** 0965fa521c16247e626e7cad01db998935733642
- **After SHA:** uncommitted

## 1) Intent

Add User Name, Assistant Name, and System Prompt settings; persist them; show names in the chat UI; and inject a synthesized system message into /v1/chat/completions so every request uses these values.

## 2) Outcome

- Extended shared AppSettings with UserName, AssistantName, and SystemPrompt (defaults: "You", "Assistant", "").
- Surfaced fields in GeneralSettingsView with TwoWay bindings; wired through GeneralSettingsViewModel.
- ChatSessionsViewModel now exposes UserDisplayName/AssistantDisplayName; header shows assistant name; bubbles label according to settings; payload prepends a synthesized system message combining names + custom prompt.

## 3) Files Changed

`	xt
src/App.Desktop/Extensions/ServiceCollectionExtensions.cs
src/App.Desktop/Services/ChatService.cs
src/App.Desktop/ViewModels/ChatSessionsViewModel.cs
src/App.Desktop/ViewModels/ModelsViewModel.cs
src/App.Desktop/ViewModels/NavigationViewModel.cs
src/App.Desktop/ViewModels/SettingsSections.cs
src/App.Desktop/ViewModels/ViewModelLocator.cs
src/App.Desktop/Views/ChatSessionsView.xaml
src/App.Desktop/Views/GeneralSettingsView.xaml
src/App.Shared/Settings/SettingsSchema.cs
src/App.Desktop/Services/ChatSessionService.cs
src/App.Desktop/Services/RunnerStatusProvider.cs
`

## 4) Per-File Notes

- src/App.Shared/Settings/SettingsSchema.cs Added three properties and defaults; participates in existing JSON persistence.
- src/App.Desktop/ViewModels/SettingsSections.cs Bound to new properties; apply/refresh/reset updated.
- src/App.Desktop/Views/GeneralSettingsView.xaml Added UI for User Name, Assistant Name, System Prompt.
- src/App.Desktop/ViewModels/ChatSessionsViewModel.cs Added display-name properties; injected system prompt; updated message build.
- src/App.Desktop/Views/ChatSessionsView.xaml Shows assistant name in header and uses names for message labels.

## 5) Commands / Scripts Touched

`
None
`

## 6) Validation

- Built with zero warnings: dotnet build Lazarus.sln -c Debug.
- Settings UI binds and saves via existing service.
- New chat request payload now begins with a system role message including names and custom text.
- UI labels reflect configured names dynamically.

## 7) Next Steps

1. Consider exposing these fields in a dedicated "Conversations" section if desired.
2. Add a quick preview under System Prompt showing effective first line or token count.

## 8) Risks / Rollback

- **Risk:** Very long system prompts can reduce available context. Mitigation: optionally show length counter.
- **Rollback:** git revert <after_sha> or revert the commit.
