# Normaize.DataNormalization.PostgresTests

This test project is intentionally **not** part of the default solution test runs.

It’s for **real-world PostgreSQL behavior** testing (transactions/locking/concurrency) using **Testcontainers**.

## Prerequisites

- Docker running locally (or in CI)

## Run

From the server repo root:

- `dotnet test .\tests\Normaize.DataNormalization.PostgresTests\Normaize.DataNormalization.PostgresTests.csproj -v minimal`

## Structure

- `BackgroundJobs/` — background worker / job queue tests (locking, dequeue semantics, retries, etc.)
- (future) `Transactions/`, `Repositories/` — additional Postgres-backed tests
