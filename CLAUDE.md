# gstack

Use the `/browse` skill from gstack for all web browsing. Never use `mcp__claude-in-chrome__*` tools.

## Available gstack skills

- `/office-hours` — structured Q&A / coaching session
- `/plan-ceo-review` — review a plan from a CEO perspective
- `/plan-eng-review` — review a plan from an engineering perspective
- `/plan-design-review` — review a plan from a design perspective
- `/design-consultation` — get design feedback and guidance
- `/review` — code review
- `/ship` — ship a change end-to-end
- `/browse` — web browsing (use this for ALL web browsing)
- `/qa` — QA a feature or change
- `/qa-only` — run QA without other steps
- `/design-review` — review designs
- `/setup-browser-cookies` — configure browser cookies for authenticated browsing
- `/retro` — run a retrospective
- `/investigate` — investigate a bug or issue
- `/document-release` — document a release
- `/codex` — run a task with OpenAI Codex
- `/careful` — extra-careful mode for risky changes
- `/freeze` — freeze a file from edits
- `/guard` — guard a file or directory
- `/unfreeze` — unfreeze a frozen file
- `/gstack-upgrade` — upgrade gstack to the latest version

If gstack skills aren't working, run `cd .claude/skills/gstack && ./setup` to build the binary and register skills.
