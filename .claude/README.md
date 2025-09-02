# Lazarus Claude Agent Kit

This folder (`.claude`) contains the **full agent ecosystem** used by Lazarus when working with Claude Code.  
It provides **sub-agents**, governance rules, and shared checklists that ensure work is consistent, safe, and high-quality across the project.

---

## 📂 Folder Structure

.claude/  
│  
├── CLAUDE.md # Global project instructions for Claude Code  
├── verification-checklist.md # Mandatory checklist for all plans & diffs  
├── challenge-mode.md # Rules for Skeptic.Challenger's pushback mode  
│  
├── agents/ # Individual agent definitions  
│ ├── review-verifier.md  
│ ├── skeptic-challenger.md  
│ ├── comments-scribe.md  
│ ├── repo-surgeon.md  
│ ├── wpf-stylist.md  
│ ├── asset-keeper.md  
│ ├── rag-mason.md  
│ ├── eval-judge.md  
│ ├── perf-tuner.md  
│ ├── safety-warden.md  
│ ├── crash-handler.md  
│ ├── dx-scaffolder.md  
│ ├── release-falconer.md  
│ ├── docs-scribe.md  
│ ├── ui-curator.md  
│ ├── telemetry-curator.md  
│ ├── migration-sentinel.md  
│ ├── test-carpenter.md  
│ ├── prompt-caretaker.md  
│ ├── lora-alchemist.md  
│ └── runner-whisperer.md  
│  
└── catalog.yaml # Master index of all agents

---

## 🧩 How It Works

- **CLAUDE.md** is the root authority file. It defines the Lazarus philosophy, workflow rules, and baseline guardrails.
- **catalog.yaml** lists every agent and its file so Claude Code can discover, delegate, and chain them consistently.
- **agents/** contains fully fleshed-out agents, each with:
  - Frontmatter (`name`, `description`)
  - System instructions
  - Inputs, procedures, rejection triggers, handoffs
  - Output formats

---

## 🚦 Governance Flow

1. **Every plan or diff** must be checked by **Review.Verifier** → applies the `verification-checklist.md`.
2. **Skeptic.Challenger** pushes back on first proposals → requires alternatives/tradeoffs.
3. **Eval.Judge** scores correctness, completeness, safety.
4. **Safety.Warden** enforces security and guardrails.
5. **Repo.Surgeon**, **WPF.Stylist**, **DX.Scaffolder**, and others handle execution work.
6. **Crash.Handler**, **Test.Carpenter**, and **Telemetry.Curator** ensure stability and observability.
7. **Release.Falconer** handles packaging and distribution once all gates pass.

---

## 🔑 Key Principles

- **No rubber-stamping** → every change is challenged, verified, and scored.
- **Safety first** → security, guardrails, and stability override speed.
- **Reproducibility** → everything must be documented, reversible, and auditable.
- **Agents are modular** → each one has a narrow mission and clear handoffs.

---

## 📌 Usage Notes

- Copy the entire `.claude` folder into any new Lazarus-related repo.
- Update `catalog.yaml` if you add new agents.
- Keep docs in sync: agents reference `CLAUDE.md`, `verification-checklist.md`, and `challenge-mode.md`.
