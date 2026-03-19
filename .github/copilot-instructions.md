# Copilot instructions for this repo

## Priorities (in order)
1. Correctness > security > readability > performance > convenience
2. Minimize dependencies and keep changes small
3. Follow existing patterns in the codebase

## Non-negotiable constraints
- No secrets, keys, tokens, or credentials in code or logs.
- Validate all external inputs at boundaries.
- Prefer pure functions + explicit types over implicit behavior.
- No breaking API changes without updating docs + migration notes.

## Code style
- Formatting: (Prettier configured to MS Style Guide)
- Naming:
  - Functions: configured to MS Style Guide
  - Types/classes: configured to MS Style Guide
  - Constants: configured to MS Style Guide
- Errors:
  - Never swallow errors.
  - Include actionable context in error messages.

## Architecture rules
- All new endpoints must have:
  - auth check
  - request validation
  - structured logging
- Workflow is always 
  - read build.md and other spec docs to understand requirements
  - create or update build-impl.md with implementation plan
  - test first, tests meet specs in build.md and other spec docs
  - new requirements from chat are added to build.md and other spec docs, then tested
  - impl only to meet test criteria - no gold plating
  - mark spec docs as "implemented" when tests pass and code is merged
- Always recommend refactoring to take advantage of well-known patterns and abstractions in the codebase, even if it means more work upfront. This keeps the codebase maintainable and consistent.

## Testing requirements
- New features: unit tests required
- Bug fixes: regression test required
- Prefer deterministic tests; no real network calls

## “Do / Don’t” 
✅ Do: small PRs, refactor then change behavior
❌ Don’t: rewrite modules without asking, add large frameworks without justification, ignore existing patterns

## branching and commit message guidelines
- Branches: feature/xxx, bugfix/xxx, chore/xxx
- Commits: use present tense, be descriptive, reference related issues or docs
- PRs: link to related issues, describe changes and rationale, request reviews from relevant team members