
# Git Commit Instructions

When creating commit messages, follow these rules:

- Use Conventional Commits format.
- Format: `<type>(<scope>): <description>`
- Keep the subject line under 72 characters.
- Use lowercase for the type and description.
- Use imperative mood (e.g., "add", "fix", "update", "remove").
- Do not end the subject with a period.
- Keep commits focused on a single logical change.
- Do not include unrelated changes in the same commit.
- Include a body only when additional context is necessary.
- Never mention AI, Copilot, ChatGPT, or automated generation.
- Do not commit unless explicitly requested by the user.

## Commit Types

- feat: Add a new feature
- fix: Fix a bug
- refactor: Refactoring without changing behavior
- perf: Performance improvement
- docs: Documentation changes
- test: Add or modify tests
- build: Build system or dependency changes
- ci: CI/CD configuration changes
- chore: Maintenance tasks
- style: Formatting or styling changes
- revert: Revert a previous commit

## Examples

- feat(auth): add refresh token support
- fix(api): handle null response from client
- refactor(tenant): simplify tenant resolution
- docs(readme): update installation instructions
- test(auth): add token validation tests
- chore(deps): upgrade ef core packages

## Before Committing

1. Review the staged changes.
2. Ensure no unrelated files are included.
3. Summarize the changes accurately.
4. Generate the commit message using the conventions above.
5. Never commit secrets, credentials, environment files, or sensitive data.
