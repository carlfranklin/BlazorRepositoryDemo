# AI Current

Concise checkpoint of the current task: goal, current state, relevant files/classes, what has been tried, what worked or failed, unresolved problems, and recommended next steps.

## Current task

None — waiting for instructions.

## Recent state

- **Completed 2026-09-01:** `5-Added IndexedDB Sync Repository` solution upgraded to .NET 10 — both projects retargeted to net10.0, AvnRepository 8.0.1→10.0.2, WebAssembly(.Server) 8.0.4→10.0.11, Newtonsoft.Json 13.0.3→13.0.4, EF Core SqlServer/Tools 8.0.4→10.0.11, CodeGeneration.Design 8.0.2→10.0.2 (BlazorIndexedDB 0.3.1, Dapper/Dapper.Contrib/System.Data.SqlClient already correct). Clean build, 0 errors (149 pre-existing warnings). Left uncommitted for Carl. **All 6 solution variants (root + folders 1-5) are now on .NET 10.**
- **Completed 2026-09-01:** `4-Added IndexedDB Repository` solution upgraded to .NET 10 — both projects retargeted to net10.0, AvnRepository 8.0.1→10.0.2, WebAssembly(.Server) 8.0.4→10.0.11, Newtonsoft.Json 13.0.3→13.0.4, EF Core SqlServer/Tools 8.0.4→10.0.11, CodeGeneration.Design 8.0.2→10.0.2 (BlazorIndexedDB 0.3.1, Dapper/Dapper.Contrib/System.Data.SqlClient already correct). Clean build, 0 errors (91 pre-existing warnings). Left uncommitted for Carl.
- **Completed 2026-09-01:** `3-Added Dapper Repository` solution upgraded to .NET 10 — both projects retargeted to net10.0, AvnRepository 8.0.1→10.0.2, WebAssembly(.Server) 8.0.4→10.0.11, Newtonsoft.Json 13.0.3→13.0.4, EF Core SqlServer/Tools 8.0.4→10.0.11, CodeGeneration.Design 8.0.2→10.0.2 (Dapper/Dapper.Contrib/System.Data.SqlClient already at correct versions). Clean build, 0 errors (80 pre-existing warnings). Left uncommitted for Carl.
- **Completed 2026-09-01:** `2-Added EF Repository` solution upgraded to .NET 10 — both projects retargeted to net10.0, AvnRepository 8.0.1→10.0.2, WebAssembly(.Server) 8.0.4→10.0.11, Newtonsoft.Json 13.0.3→13.0.4, EF Core SqlServer/Tools 8.0.4→10.0.11, CodeGeneration.Design 8.0.2→10.0.2. Clean build, 0 errors (62 pre-existing warnings). Left uncommitted for Carl.
- **Completed 2026-09-01:** `1-In Memory Only` solution upgraded to .NET 10 — both projects retargeted to net10.0, AvnRepository 8.0.1→10.0.2, WebAssembly(.Server) 8.0.4→10.0.11, Newtonsoft.Json 13.0.3→13.0.4. Clean build, 0 errors (46 pre-existing nullable/unused-var warnings). Left uncommitted for Carl.
- **Completed 2026-09-01:** Memory-system restructure — `agentmemory/startup.md` is the single source of truth for session rules; per-repo `startup.md` is a one-line pointer, `AI_RULES.md`/`ahem.wav` deleted. Committed by Carl as f08ec5b.
- **Completed 2026-09-01:** Root `RepositoryDemo` solution upgraded to .NET 10 — clean build, 0 errors. Committed by Carl as 30b584b. See AI_MEMORY.md for the AvnRepository 10.0.2 republish details and package versions.
- All 6 solution variants (root + folders 1-5) are now on net10.0 / AvnRepository 10.0.2.
- Memory files are tracked in git (committed in 30b584b).
