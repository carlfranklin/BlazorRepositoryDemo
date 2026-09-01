# AI Rules

Permanent project rules, architecture, conventions, constraints, and instructions for AI sessions working on this repository.

## Session rules (from startup.md)

- Canonical `startup.md` and `ahem.wav` also live in the `carlfranklin/agentmemory` repo (uploaded 2026-09-01); the AI's name is **Buzzby** (renamed from Bud 2026-09-01).

- Memory files: `AI_RULES.md` (rules), `AI_MEMORY.md` (durable knowledge), `AI_CURRENT.md` (current task checkpoint). Maintain them continuously, not just at session end.
- Before claiming a task complete: get a clean build, write a one-paragraph commit message (no bullets, no CRLF) but do NOT commit — committing is the user's job.
- After each completed task, play `ahem.wav` (repo root) via synchronous Win32 `PlaySound` with flag `0x20000` (SND_FILENAME only). Do not use `[System.Media.SystemSounds]::Tada` (null on this machine) or other flag values.
- Keep memory files small and current; replace obsolete info rather than accumulating history. Source code is the source of truth.

## Project rules

(To be filled in as conventions and constraints are established.)
