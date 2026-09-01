# AI Current

Concise checkpoint of the current task: goal, current state, relevant files/classes, what has been tried, what worked or failed, unresolved problems, and recommended next steps.

## Current task

Memory-system restructure (in progress, 2026-09-01): `agentmemory/startup.md` is now the single source of truth for session rules. In this repo: `startup.md` reduced to a one-line pointer, `AI_RULES.md` and `ahem.wav` deleted (canonical copies live in agentmemory). `AI_MEMORY.md` + `AI_CURRENT.md` stay. Changes left uncommitted for Carl.

## Recent state

- **Completed 2026-09-01:** Root `RepositoryDemo` solution upgraded to .NET 10 — clean build, 0 errors. Committed by Carl as 30b584b. See AI_MEMORY.md for the AvnRepository 10.0.2 republish details and package versions.
- Numbered folders 1-5 still have uncommitted .NET 10 csproj changes from a prior session that reference the broken AvnRepository 10.0.1 (dotnet tool) — they will need AvnRepository bumped to 10.0.2 + Newtonsoft.Json 13.0.4 before they build.
- Memory files are tracked in git (committed in 30b584b).
