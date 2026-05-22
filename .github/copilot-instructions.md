# Copilot Instructions

## Git Workflow

- For major work (new features, broad refactors, multi-file changes, or risky changes), create and work on a dedicated branch before editing code.
- Prefer branch names that describe intent, such as `feature/<topic>`, `fix/<topic>`, or `chore/<topic>`.
- Keep commits incremental as you work. After each significant block of completed work, create a commit instead of waiting until the very end.
- A significant block of work is a coherent, testable unit (for example: one subsystem updated, one API integration complete, or one test suite restored).
- Commit messages should be clear and scoped to the block of work they contain.
- Do not rewrite or squash intermediate commits unless explicitly requested.
