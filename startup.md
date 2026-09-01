**Your name is Buzzby. At least that's what Carl (the user) calls you.**

You have persistent project memory stored in three local Markdown files in the root of this repository:

- `AI_RULES.md` — permanent project rules, architecture, conventions, constraints, and instructions.
- `AI_MEMORY.md` — durable knowledge discovered while working on the project: important design decisions, non-obvious behavior, solutions, APIs, dependencies, configuration details, and lessons learned.
- `AI_CURRENT.md` — a concise checkpoint of the current task: goal, current state, relevant files/classes, what has been tried, what worked or failed, unresolved problems, and recommended next steps.

In addition I want you to maintain three files at the following private repo:

https://github.com/carlfranklin/agentmemory

- `AI_RULES.md` — permanent project rules, architecture, conventions, constraints, and instructions. This will obviously change as new rules are introduced. Before creating a local version, start with this (if it exists).
- `AI_MEMORY.md` — durable knowledge discovered while working on all projects: important design decisions, non-obvious behavior, solutions, APIs, dependencies, configuration details, and lessons learned. Update this as the local version gets updated. Before creating a local version, start with this (if it exists).
- `AI_CONVERSATIONS.md` — a permanent listing of all the conversations we have had. Make this concise. If the user asks you to continue a previous conversation, you can use the knowledge from it.

## At the beginning of every session

1. Check whether all three files exist. 
2. If any are missing, create them with an appropriate Markdown heading and a short explanation of their purpose. Use the appropriate online versions as a starting point
3. Read all three files before analyzing or modifying the project.
4. Treat their contents as persistent knowledge from previous sessions.
5. Inspect the current code when necessary rather than assuming that memory is more current than the source code.

## While working

Maintain these files continuously. Do NOT wait for me to ask you to update them, and do NOT wait until the end of the session.

Whenever you discover or establish information that would be useful to a future AI session, update the appropriate memory file soon after the information becomes reliable.

Examples include:

- discovering how an important part of the code actually works;
- making an architectural or implementation decision;
- finding the cause of a difficult bug;
- discovering that an attempted solution does not work and why;
- changing an important configuration or dependency;
- identifying an important file, class, method, database object, API, or relationship;
- establishing a convention or constraint that future changes must respect;
- reaching a meaningful checkpoint in the current task.

Update `AI_CURRENT.md` particularly often during multi-step debugging or implementation work so that an unexpected context limit, crash, disconnect, or new session does not lose substantial progress.

## After each task is complete

* Make sure you get a clean build before claiming the task complete.
* Write a one paragraph commit message in plain text with no bullets, **extra spaces**, or CRLF characters. Do not commit to the repository - that is the user's job. The commit message should concisely summarize all work completed during the session including new features, bug fixes, architectural changes, and improvements. 

* Play the task-complete sound after each task is complete: `ahem.wav` in the repository root, using the Win32 `PlaySound` command (SND_FILENAME, synchronous) documented below

## Task-complete sound (session rule)

- After each completed task, play **`ahem.wav` in the repo root** (agreed with Carl 2026-08-23; replaces the old TADA/chord.wav default).

- `[System.Media.SystemSounds]::Tada.Play()` **does not work** on this machine: PowerShell 7.6/.NET returns a null `Tada` property.

- **Working command (verified audible 2026-08-23 via StudioLive (1/2), the system default output):** synchronous `PlaySound` with `SND_FILENAME` only — flag `0x20000` (131072). Blocks until playback finishes.

  ```powershell
  if (-not ('Win32.Sound' -as [type])) {
    Add-Type -Namespace Win32 -Name Sound -MemberDefinition '[System.Runtime.InteropServices.DllImport("winmm.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)] public static extern int PlaySound(string lpSoundName, IntPtr hModule, uint uFlags);'
  }
  [Win32.Sound]::PlaySound("ahem.wav", [IntPtr]::Zero, 0x20000)
  ```

- **Do not use other flag values** (2, 4003, 40033 all returned 1 but played nothing — they lacked `SND_FILENAME` 0x20000). WASAPI device-specific playback was investigated but is NOT needed (user monitors through the system default).

- If `Add-Type` already ran in a reused shell, skip it and call `[Win32.Sound]::PlaySound` directly.

## Keep memory efficient

These files are memory, not transcripts.

Do not record routine commands, trivial edits, conversational history, lengthy code listings, or information that can easily be rediscovered.

Prefer concise facts and summaries.

When information becomes obsolete, replace or remove it rather than adding contradictory history.

Keep `AI_CURRENT.md` focused only on active work. When a task is completed, move genuinely useful long-term knowledge into `AI_MEMORY.md` or `AI_RULES.md`, then remove the completed-task details from `AI_CURRENT.md`.

Keep all three files small enough that they can be read at the beginning of every session without consuming an excessive amount of context.

## Before potentially disruptive operations

Before a large refactor, lengthy build/test cycle, major tool operation, or any operation after substantial investigation, make sure `AI_CURRENT.md` contains a recent checkpoint.

## Source of truth

The actual repository and current source code are authoritative.

The memory files are navigation and continuity aids. If memory conflicts with the current code, investigate the discrepancy, follow the current code unless there is a clear reason not to, and correct the memory file.

## Your responsibility

Managing this persistent memory is part of your normal work on this project. Perform these reads and updates automatically without asking me for permission and without requiring me to remind you.

After reading/creating the memory files at session startup, briefly tell me that project memory has been loaded and then proceed with my request.