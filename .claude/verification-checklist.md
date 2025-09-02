# Solution Verification Checklist

This checklist must be applied to **significant changes** before acceptance.  
Review.Verifier enforces this with practical focus on real risks and impacts.

---

## Change Classification

**High-Risk Changes** (Full checklist required):

- Security-related modifications
- Breaking API changes
- Database schema changes
- Major architectural refactoring
- Performance-critical code paths

**Routine Maintenance** (Abbreviated checklist):

- Namespace alignment and cleanup
- Code comments and documentation
- Dependency version updates
- File organization and references
- Bug fixes following established patterns

---

## Core Quality Standards (Always Required)

- [ ] **Security Impact**: No new vulnerabilities introduced
- [ ] **Build Integrity**: Clean compilation with no new warnings
- [ ] **Functional Integrity**: Existing features continue working
- [ ] **Code Quality**: Follows established project patterns

---

## Extended Validation (High-Risk Changes Only)

### Root Cause & Research

- [ ] Identified root cause, not just symptoms
- [ ] Analyzed existing codebase patterns for consistency
- [ ] Research conducted where gaps in knowledge exist

### Architecture & Design

- [ ] Evaluated current architecture fit
- [ ] Technical debt impact assessed
- [ ] NOT a yes-man — honest assessment provided

### Solution Quality

- [ ] CLAUDE.md compliant
- [ ] Simple, streamlined, no unnecessary complexity
- [ ] Complete solution (not partial implementation)
- [ ] Trade-offs explicitly documented
- [ ] Long-term maintainability considered

### Security & Safety (Critical Changes)

- [ ] Input validation and sanitization appropriate
- [ ] Authentication/authorization handled correctly
- [ ] Sensitive data protected
- [ ] OWASP guidelines followed where applicable

### Integration & Testing

- [ ] Upstream and downstream impacts considered
- [ ] All affected files updated consistently
- [ ] Integration follows established patterns
- [ ] Testing strategy appropriate to change scope

---

## Application-Specific Validation

_(Customize for project needs)_

- [ ] WPF MVVM patterns maintained
- [ ] Dependency injection container updated
- [ ] Logging and error handling consistent
- [ ] Theme and styling compliance

---

## Process Guidelines

**For Routine Maintenance**: Focus on core quality standards only.  
**For High-Risk Changes**: Apply full extended validation.

**Evidence Standard**: Provide sufficient evidence for the change scope, not academic research papers.

**Practical Focus**: Block real problems, approve obvious improvements efficiently.
