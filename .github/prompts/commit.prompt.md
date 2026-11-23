
---
mode: agent
description: Generate a commit message based on staged Git changes.
---

Analyze the staged Git changes using git diff --cached.

Generate a commit message following Conventional Commits.

Requirements:
- Format: <type>(<scope>): <description>
- Subject must be under 72 characters.
- Use imperative mood.
- No trailing period.
- Include a body only if needed.
- Do not include AI attribution.
- Do not commit automatically.

Output only the commit message.
