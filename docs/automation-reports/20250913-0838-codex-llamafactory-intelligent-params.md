# Automation Report Intelligent LLaMAFactory Parameter Management

- **Date:** $(date '+%Y-%m-%d %H:%M')
- **Agents:** codex
- **Branch:** main
- **Before SHA:** 0c45c06a893ce91e6d953d9caeb848a1b8f9405c
- **After SHA:** uncommitted

## 1) Intent

Add trainer-aware behaviors for LLaMAFactory: auto-hide incompatible options, auto-set safe defaults, model-format validation, VRAM estimation warnings, template filtering, dataset conversion to ShareGPT, and job folder materialization.

## 2) Outcome

- ConversationsDesignerViewModel: trainer intelligence for LLaMAFactory (hide GC/FA2/Pack; set LR/optimizer/scheduler/GA; auto-detect chat template), validation + VRAM estimate, status indicator.
- ConversationsDesignerView.xaml: bindings to hide incompatible options and show status; LR ToggleButtons preserved.
- ConversationTrainingService: Creates Jobs/<jobId> folder with profile.json; on Start, converts train JSONL → sharegpt JSONL, writes dataset_info.json (no BOM), and creates a direct_train.py wrapper.
- TrainingView bottom bar: Create Job routes to Conversations.CreateJobCommand (restored button).

## 3) Files Changed

```txt
modified  src/App.Desktop/ViewModels/Training/ConversationsDesignerViewModel.cs
modified  src/App.Desktop/Views/Training/ConversationsDesignerView.xaml
modified  src/App.Backend/Services/ConversationTrainingService.cs
modified  src/App.Desktop/Views/Training/TrainingView.xaml
```

## 4) Per-File Notes

- VM: Added status properties, visibility flags, heuristics, validation, and VRAM estimator; hooked into SelectedTrainer/SelectedTask.
- XAML: Visibility bindings for GC/FA2/Pack; simple status bar for LF messages.
- Service: Job workspace creation; ShareGPT conversion and dataset_info.json generation; wrapper creation.
- View: Global Create Job calls Conversations command.

## 5) Commands / Scripts Touched

```
None
```

## 6) Validation

- Builds succeeded for Backend and Desktop.
- Manual flow: Select LLaMAFactory → incompatible options disappear, defaults adjust; Create Job → Jobs/<id>/profile.json exists; Start → train_converted.jsonl + dataset_info.json under Jobs/<id>.

## 7) Next Steps

1. Bind Chat Template ComboBox to a filtered list derived from model family (qwen/llama/mistral/yi) and Params[ChatTemplate].
2. Surface status colors/icons (green/yellow/red) using visual states.
3. When we integrate real trainer calls, pass the ShareGPT path with forward slashes.

## 8) Risks / Rollback

- **Risk:** Some users may provide model names instead of folders; we warn but don’t block unless GGUF detected.
- **Rollback:** `git revert <after_sha>`.
