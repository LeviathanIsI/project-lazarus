# CLAUDE.md

## Purpose

This file defines the default behavior and workflow for Claude Code in this repository.  
Plans and changes are verified and challenged based on **risk level** and **change scope** for efficient development.

---

## Change Classification

**Routine Maintenance** (Streamlined process):

- Namespace alignment and code organization
- Documentation updates and code comments
- Dependency version updates (patch/minor)
- File reference cleanup and project organization
- Bug fixes following established patterns

**Significant Changes** (Full governance process):

- Security-related modifications
- Breaking API changes or major refactoring
- New features or architectural changes
- Database schema modifications
- Performance-critical optimizations

---

## Workflow Based on Change Type

### Routine Maintenance Process

1. **Direct Implementation**
   - Specialized agents (e.g., Repo.Surgeon, Comments.Scribe) implement changes
   - **Light verification**: Core quality standards only
   - **Quick challenge**: Focus on obvious problems, not theoretical concerns

### Significant Changes Process

1. **Plan Phase**

   - Sub-agents propose comprehensive solutions
   - Document approach and expected impacts

2. **Verification Phase**

   - Review.Verifier applies extended validation checklist
   - Focus on security, architecture, and integration impacts
   - Evidence proportional to change risk

3. **Challenge Phase**

   - Skeptic.Challenger evaluates for better alternatives
   - Push back on real risks and complexity issues
   - Accept good solutions efficiently

4. **Implementation Phase**
   - Delegate to appropriate specialized agents
   - Apply changes with appropriate testing
   - Monitor for issues post-implementation

---

## Quality Standards (Always Required)

- **Security**: No new vulnerabilities introduced
- **Build Integrity**: Clean compilation and warnings
- **Functional Integrity**: Existing features preserved
- **Code Quality**: Consistent with project patterns
- **Documentation**: Public APIs documented appropriately

---

## Agent Collaboration Guidelines

- **Trust architectural judgment** on routine improvements
- **Focus verification** on high-risk changes and real problems
- **Streamline approvals** for obvious improvements
- **Maintain quality** without bureaucratic theater
- **Collaborate efficiently** between specialized agents

---

## Project-Specific Standards

- Follow Microsoft C# documentation standards for public APIs
- Maintain WPF MVVM patterns and dependency injection
- Apply secure coding practices (OWASP guidelines)
- Preserve theme consistency and accessibility considerations
- Keep changes atomic and well-documented

---

## Agent Handoff Examples

**Routine Maintenance**: Direct execution with light verification  
**Security Fixes**: Safety.Warden → Review.Verifier → Implementation  
**UI Polish**: WPF.Stylist → UI.Curator → Light verification  
**Major Features**: Full governance chain with comprehensive validation

---

## Notes

- Governance adapts to change scope and risk level
- Efficiency and quality are both priorities
- Agents should collaborate naturally, not follow rigid bureaucracy
- Process serves development velocity, not compliance theater
