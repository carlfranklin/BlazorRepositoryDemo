# AI Current

Concise checkpoint of the current task: goal, current state, relevant files/classes, what has been tried, what worked or failed, unresolved problems, and recommended next steps.

## Current task

None active. Awaiting next task from Carl (likely the next numbered folder, 1-5, one at a time).

## Recent state

- **Completed 2026-09-01:** Root `RepositoryDemo` solution upgraded to .NET 10 — clean build, 0 errors. Changes uncommitted (Carl commits). See AI_MEMORY.md for the AvnRepository 10.0.2 republish details and package versions.
- Numbered folders 1-5 still have uncommitted .NET 10 csproj changes from a prior session that reference the broken AvnRepository 10.0.1 (dotnet tool) — they will need AvnRepository bumped to 10.0.2 + Newtonsoft.Json 13.0.4 before they build.
- Untracked files: `AI_RULES.md`, `AI_MEMORY.md`, `AI_CURRENT.md`, `ahem.wav`, `startup.md`.
