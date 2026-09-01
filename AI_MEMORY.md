# AI Memory

Durable knowledge discovered while working on this project: design decisions, non-obvious behavior, solutions, APIs, dependencies, configuration details, and lessons learned.

## Project structure

- Blazor WebAssembly repository-pattern demo (tutorial by Carl Franklin). Root `RepositoryDemo/` is the final variant; numbered folders `1-In Memory Only` through `5-Added IndexedDB Sync Repository` are progressive stages, each with its own `RepositoryDemo/` (server) + `RepositoryDemo.Client/` (client) projects.
- Key packages: `AvnRepository` (repository plumbing), `BlazorIndexedDB`, `Dapper`/`Dapper.Contrib`, `Microsoft.EntityFrameworkCore.SqlServer`, `Newtonsoft.Json`, SignalR client.
- `README.md` is a large (~190 KB) step-by-step tutorial document.

## .NET 10 upgrade (2026-09-01)

- Carl upgrades ONE solution at a time (numbered folders 1-5 are separate tasks; do not touch them while working on another).
- Root `RepositoryDemo/` has NO .sln file — build the server csproj directly: `dotnet build RepositoryDemo/RepositoryDemo/RepositoryDemo.csproj` (it references the Client project).
- Root solution upgraded to net10.0: `AvnRepository` 10.0.2, `Microsoft.AspNetCore.Components.WebAssembly(.Server)` 10.0.11, `Microsoft.EntityFrameworkCore.*` 10.0.11, `Microsoft.VisualStudio.Web.CodeGeneration.Design` 10.0.2, `BlazorIndexedDB` 0.3.1, `Dapper` 2.1.35, `Dapper.Contrib` 2.0.78, `Newtonsoft.Json` 13.0.4, `System.Data.SqlClient` 4.8.6. Clean build: 0 errors, 158 pre-existing nullable/unused-var warnings.
- `1-In Memory Only` upgraded to net10.0 (2026-09-01): same package bumps (AvnRepository 10.0.2, WebAssembly(.Server) 10.0.11, Newtonsoft.Json 13.0.4). Clean build: 0 errors, 46 pre-existing warnings. Folders 2-5 still on net8.0.
- SDK 10.0.400 installed on this machine.

## AvnRepository tool-vs-library gotcha (IMPORTANT)

- `AvnRepository` 9.0.0, 9.0.1, and 10.0.1 were published as **dotnet tools** (`PackAsTool=True` in the AvnRepository csproj). A dotnet-tool package CANNOT be used as a `PackageReference` — restore fails with `NU1212: Invalid project-package combination ... DotnetToolReference project style can only contain references of the DotnetTool type`. Only 8.0.1 was a normal library (net8.0).
- **Fixed 2026-09-01:** removed `PackAsTool=True`, bumped to **10.0.2**, tag `v10.0.2` → trusted-publishing workflow published 10.0.2 as a normal library (verified nuspec on nuget.org: no DotnetTool packageType, depends on `Microsoft.EntityFrameworkCore` 10.0.11). Use **10.0.2** (not 10.0.1) in all consuming projects.
- `Newtonsoft.Json` must be **13.0.4** (not 13.0.3): `Microsoft.EntityFrameworkCore.Tools` 10.0.11 → `Microsoft.EntityFrameworkCore.Design` 10.0.11 pulls Newtonsoft.Json >= 13.0.4, so 13.0.3 causes `NU1605` package-downgrade error.
