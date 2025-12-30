# .NET 10 Upgrade Plan (normaize-server)

Date: 2025-12-28

## Summary
This document outlines what it would take to upgrade the **normaize-server** solution from **.NET 9** to **.NET 10**, based on the current repo state.

High-level: this solution is small (4 main projects + 3 test projects), so the framework bump is straightforward; the main work is aligning **EF Core + Npgsql** and ensuring **CI/Docker/tooling** are pinned to compatible versions.

## Current State (as of 2025-12-28)

### Target frameworks
All projects currently target **`net9.0`**:
- `src/Normaize.DataNormalization.API/Normaize.DataNormalization.API.csproj`
- `src/Normaize.DataNormalization.Infrastructure/Normaize.DataNormalization.Infrastructure.csproj`
- `src/Normaize.DataNormalization.Application/Normaize.DataNormalization.Application.csproj`
- `src/Normaize.DataNormalization.Domain/Normaize.DataNormalization.Domain.csproj`
- `tests/Normaize.DataNormalization.API.Tests/Normaize.DataNormalization.API.Tests.csproj`
- `tests/Normaize.DataNormalization.Infrastructure.Tests/Normaize.DataNormalization.Infrastructure.Tests.csproj`
- `tests/Normaize.DataNormalization.Application.Tests/Normaize.DataNormalization.Application.Tests.csproj`
- `tests/Normaize.DataNormalization.Domain.Tests/Normaize.DataNormalization.Domain.Tests.csproj`

### CI/workflows
Multiple workflows pin the SDK via `DOTNET_VERSION: '9.0.x'`:
- `.github/workflows/ci.yml`
- `.github/workflows/pr-checks.yml`
- `.github/workflows/code-quality.yml`
- `.github/workflows/coverage-analysis.yml`
- `.github/workflows/dependency-check.yml`

The CI database migration job installs `dotnet-ef` globally (now pinned to `9.0.10` after the pipeline fix).

### Docker
`Dockerfile` uses .NET 9 images:
- `mcr.microsoft.com/dotnet/aspnet:9.0`
- `mcr.microsoft.com/dotnet/sdk:9.0`

### Key packages in use (upgrade-impacting)
- API:
  - `Microsoft.AspNetCore.Authentication.JwtBearer` **9.0.0**
  - `Microsoft.AspNetCore.OpenApi` **9.0.0**
  - `Microsoft.EntityFrameworkCore.Design` **9.0.10**
  - `Swashbuckle.AspNetCore` **7.0.0**
  - `Serilog.AspNetCore` **8.0.2**
- Infrastructure:
  - `Microsoft.EntityFrameworkCore.Relational` **9.0.10**
  - `Microsoft.EntityFrameworkCore.Tools` **9.0.10**
  - `Npgsql.EntityFrameworkCore.PostgreSQL` **9.0.4**
  - `Microsoft.Extensions.*` **9.0.0** packages
- Application:
  - `Microsoft.Extensions.Logging.Abstractions` **9.0.0**
  - `MediatR` **12.2.0** (note: other projects use 12.4.1)
- Tests:
  - `Microsoft.EntityFrameworkCore.InMemory` **9.0.10**

### SDK pinning
No `global.json` is present, so local dev SDK selection depends on what’s installed and what CI config chooses.

## What Must Change for .NET 10

### 1) Bump Target Frameworks
Update all projects from `net9.0` → `net10.0`.

Recommended approach:
- Update the 4 `src/*` projects first.
- Then update the 4 `tests/*` projects.

### 2) Align Microsoft package wave to 10.x
When you move to `net10.0`, you should also upgrade the “wave” packages together to reduce binding conflicts:
- `Microsoft.AspNetCore.*` → **10.0.x**
- `Microsoft.Extensions.*` → **10.0.x**

If you mix 9.x and 10.x across the graph, you can get the exact kind of assembly-unification problems you already saw with EF Core (MSB3277).

### 3) Upgrade EF Core + provider together
This repo is EF-heavy (migrations run in CI), so treat it as a single unit:
- `Microsoft.EntityFrameworkCore.*` → **10.0.x** (Relational/Design/Tools/InMemory/etc.)
- `dotnet-ef` → **10.0.x**
- `Npgsql.EntityFrameworkCore.PostgreSQL` → the **provider version that matches EF Core 10** (likely **10.x**, but confirm with Npgsql release notes)

Acceptance check:
- `dotnet ef migrations list --project src/Normaize.DataNormalization.Infrastructure --startup-project src/Normaize.DataNormalization.API`
- `dotnet ef database update ...` against Postgres 15

### 4) CI updates
Update `DOTNET_VERSION` in every workflow using .NET:
- `9.0.x` → `10.0.x`

Also update any tool installs that assume 9.x (examples):
- `dotnet tool install --global dotnet-ef --version 10.0.x`

Note: `dotnet format`, `dotnet-sonarscanner`, and `dotnet-reportgenerator-globaltool` usually work across SDK versions, but if you see failures, pin them to known-good versions.

### 5) Docker updates
Update `Dockerfile` base images:
- `aspnet:9.0` → `aspnet:10.0`
- `sdk:9.0` → `sdk:10.0`

### 6) Optional but strongly recommended: add `global.json`
To make local dev and CI match (and avoid “works on my machine” issues), add a repo-level `global.json` like:

```json
{
  "sdk": {
    "version": "10.0.xxx",
    "rollForward": "latestPatch"
  }
}
```

(Use the specific patch you want. CI can still install `10.0.x`.)

## Expected Code Changes (Likely)
Most projects are plain library/API projects and should compile after package bumps.

Most likely areas requiring actual edits:
- EF Core / Npgsql breaking changes (query translation, mapping behaviors, migrations scaffolding differences)
- new SDK analyzers/warnings (especially because API uses `TreatWarningsAsErrors=true`)

## Effort Estimate (Repo-specific)
Given the current size (8 csproj total), the baseline upgrade is usually:
- **0.5–1 day** to update frameworks/packages + get CI green
- **+1–3 days** if EF Core/Npgsql requires migrations/behavior fixes and additional testing

## Concrete Checklist (Order of Operations)
1. Update all `TargetFramework` values to `net10.0`.
2. Update packages:
   - Bump `Microsoft.AspNetCore.*` to 10.0.x in API + API tests
   - Bump `Microsoft.Extensions.*` to 10.0.x (Application/Infrastructure/tests)
   - Bump all `Microsoft.EntityFrameworkCore.*` packages to 10.0.x
   - Bump `Npgsql.EntityFrameworkCore.PostgreSQL` to the EF10-compatible major
   - (Optional cleanup) unify MediatR version across projects
3. Update tooling:
   - Pin `dotnet-ef` to 10.0.x in `.github/workflows/ci.yml` database migration job
   - Consider switching to a local tool manifest (`.config/dotnet-tools.json`) + `dotnet tool restore`
4. Update `.github/workflows/*` `DOTNET_VERSION` from `9.0.x` to `10.0.x`.
5. Update `Dockerfile` to .NET 10 base images.
6. Run locally:
   - `dotnet restore`
   - `dotnet build -c Release`
   - `dotnet test -c Release`
   - `dotnet ef migrations list ...`
7. Run CI migration step against ephemeral Postgres (same as workflow).

## Common Failure Modes (and fixes)
- **Missing `System.Runtime, Version=10.0.0.0`** during migration step:
  - indicates `dotnet-ef` (or another global tool) was installed for .NET 10 but the runner SDK is 9.x (or vice versa). Fix by pinning SDK + tool versions consistently.
- **MSB3277 / assembly version conflicts**:
  - fix by aligning the “wave” packages (EF Core, Microsoft.Extensions, AspNetCore) to the same major/minor.

---
If you want, I can follow this doc by making the actual upgrade PR in the repo (frameworks, packages, workflows, Dockerfile) in a single change set.
