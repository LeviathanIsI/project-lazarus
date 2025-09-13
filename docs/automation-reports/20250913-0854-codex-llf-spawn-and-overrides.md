# Automation Report LLaMAFactory Spawn + Overrides

- **Date:** 2025-09-13 08:54
- **Agents:** codex
- **Branch:** main
- **Before SHA:** d1a9e346153ea9c4d12d264e1380dd31453c96cb
- **After SHA:** uncommitted

## 1) Intent

Add a real LLaMAFactory command builder: generate dataset metadata and spawn Python using a direct wrapper with Hydra overrides, wiring paths under Lazarus appdata.

## 2) Outcome

- ConversationTrainingService.StartTrainingAsync now:
  - Converts train JSONL → ShareGPT and writes dataset_info.json (no BOM).
  - Creates Jobs/<id>/bin/direct_train.py that calls .
  - Detects Python (prefers ).
  - Builds Hydra override args from TrainingProfile (model, dataset_dir, template, LR, steps/epochs, batch, GA, cutoff, scheduler, optim, save/eval).
  - Sets env:  (if exists),  to models root.
  - Spawns process in job dir; pumps stdout/err into System-Data/Logs/training/<jobId>.log; sets job to Completed/Failed on exit.

## 3) Files Changed



## 4) Validation

- Full solution builds.
- If LF is present under appdata Trainers, wrapper should import it; otherwise log captures import error.

## 5) Next Steps

1. Parse training logs for step progress to drive UI progress instead of stub.
2. Add cancellation support by mapping Stop to process kill.

## 6) Risks / Rollback

- **Risk:** Hydra key names may differ by LF version; adjust overrides as needed.
- **Rollback:** .
