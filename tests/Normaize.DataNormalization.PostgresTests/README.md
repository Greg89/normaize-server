# Normaize.DataNormalization.PostgresTests

This test project is intentionally **not** part of the default solution test runs.

It’s for **real-world PostgreSQL behavior** testing (transactions/locking/concurrency) using **Testcontainers**.

## CI behavior

There are two GitHub Actions paths for these tests:

- Main CI pipeline: runs these tests as **non-blocking** (warns on failure / Docker unavailable).
- Nightly workflow: runs these tests as **blocking** and fails if Docker is unavailable or any test fails.

Nightly workflow file:

- [.github/workflows/nightly-postgres-tests.yml](../../.github/workflows/nightly-postgres-tests.yml)

## Prerequisites

- Docker running locally (or in CI)

## Run

From the server repo root:

- `dotnet test .\tests\Normaize.DataNormalization.PostgresTests\Normaize.DataNormalization.PostgresTests.csproj -v minimal`

## Run in GitHub Actions

- Manual run: GitHub → Actions → “Nightly Postgres Tests” → Run workflow

## Structure

- `BackgroundJobs/` — background worker / job queue tests (locking, dequeue semantics, retries, etc.)
- (future) `Transactions/`, `Repositories/` — additional Postgres-backed tests
