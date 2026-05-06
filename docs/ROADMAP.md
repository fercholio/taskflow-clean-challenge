# TaskFlow Clean Challenge — Roadmap to Submission

**Deadline:** Friday, Nov 8, 14:00 (Mexico City time).
**Working window assumed:** Wed Nov 5 (afternoon) → Thu Nov 6 → Fri Nov 7 → Fri Nov 8 morning (buffer + polish).
**Mode:** resumable — every phase has a clear "exit state" so you can stop and restart cleanly. Each phase ends with a green build + green tests + a commit.

> **Submission artifacts required by the brief**
> 1. Public GitHub link (only the link).
> 2. Presentation file (README.md is acceptable) covering: user story, architecture, design choices, demo instructions, GenAI section.
> 3. Working app with CRUD + auth + seeded demo data.

---

## Phase 0 — Repo bootstrap (≈ 30 min) ✅ DO FIRST

**Goal:** empty-but-runnable skeleton pushed to GitHub.

- [ ] `cd C:\Users\Fercho\source\repos\taskflow-clean-challenge`
- [ ] `git init -b main`
- [ ] Create solution + projects (commands in §A below).
- [ ] Add `.gitignore` (VisualStudio + Node templates), `.editorconfig`, `Directory.Build.props` with `TreatWarningsAsErrors=true`, `Nullable=enable`.
- [ ] First commit: `chore: scaffold solution and projects`.
- [ ] `gh repo create taskflow-clean-challenge --public --source=. --remote=origin --push` (or create on github.com and push manually).
- [ ] Verify `dotnet build` and `dotnet test` succeed on an empty solution.

**Exit state:** green build, repo public, README placeholder pushed.

---

## Phase 1 — Domain + first failing tests (≈ 1.5 h)

**Goal:** TDD the core entities. No infrastructure yet.

- [ ] In `TaskFlow.Domain.Tests`, write tests for:
  - `User.Create` — valid email, rejects empty/invalid email, rejects short password marker.
  - `TaskItem.Create` — title required (≤200), description optional (≤2000), `DueDate` must be UTC future-or-now, default status = `Pending`.
  - `TaskItem.UpdateStatus` — only allowed transitions (`Pending → InProgress → Done`, `* → Cancelled`).
- [ ] Implement `User`, `TaskItem`, `TaskStatus` enum, `Email` value object, `DomainException`.
- [ ] Run tests → all green.
- [ ] Commit: `feat(domain): user and task entities with invariants`.

**Exit state:** Domain tests > 95% coverage, no Application/Infra dependencies.

---

## Phase 2 — Application layer use cases (≈ 2 h)

**Goal:** Handlers + abstractions, fully unit-tested with Moq.

- [ ] Define abstractions in `TaskFlow.Application/Abstractions/`:
  `IUserRepository`, `ITaskRepository`, `IUnitOfWork`, `IPasswordHasher`, `IJwtTokenService`, `IClock`.
- [ ] DTOs / Commands / Queries (records).
- [ ] Handlers (one file each):
  - `RegisterUserHandler`, `LoginUserHandler`
  - `CreateTaskHandler`, `UpdateTaskHandler`, `DeleteTaskHandler`, `GetTaskByIdHandler`, `ListTasksHandler`
- [ ] FluentValidation validators for each command.
- [ ] `Result<T>` type for success/failure flow (no exceptions for control flow).
- [ ] Unit tests for every handler: success path + at least one failure path each.
- [ ] Commit: `feat(application): use cases with validators and tests`.

**Exit state:** Application tests > 90%; handlers depend only on abstractions.

---

## Phase 3 — Infrastructure (Postgres + Npgsql + JWT) (≈ 2.5 h)

**Goal:** Real persistence, no ORM.

- [ ] Add NuGet: `Npgsql`, `Microsoft.AspNetCore.Identity` (only for `PasswordHasher<T>`), `Microsoft.IdentityModel.Tokens`, `System.IdentityModel.Tokens.Jwt`, `dbup-postgresql` (allowed; not an ORM).
- [ ] `db/migrations/0001_init.sql`:
  ```sql
  create table users (
      id uuid primary key,
      email varchar(254) unique not null,
      password_hash text not null,
      created_at_utc timestamptz not null
  );
  create table tasks (
      id uuid primary key,
      user_id uuid not null references users(id) on delete cascade,
      title varchar(200) not null,
      description varchar(2000),
      status smallint not null,
      due_date_utc timestamptz not null,
      created_at_utc timestamptz not null,
      updated_at_utc timestamptz not null
  );
  create index ix_tasks_user_id on tasks(user_id);
  ```
- [ ] `db/migrations/0002_seed.sql`: 1 demo user (`demo@taskflow.dev` / `Demo123!`) + 3 tasks. Hash the password offline once and paste the hash.
- [ ] Implement `NpgsqlUserRepository`, `NpgsqlTaskRepository`, `NpgsqlUnitOfWork` using `NpgsqlDataSource`.
- [ ] Implement `BcryptPasswordHasher` adapter wrapping `PasswordHasher<User>`.
- [ ] Implement `JwtTokenService` (HS256, 60 min expiry, includes `sub`, `email`, `jti`).
- [ ] Implement `MigrationsRunner` invoked from `Program.cs` on startup (idempotent).
- [ ] Integration tests using **Testcontainers.PostgreSql** for at least one repo (CRUD round-trip).
- [ ] Commit: `feat(infrastructure): npgsql repos, jwt, migrations`.

**Exit state:** `docker compose up postgres` then `dotnet test` passes integration tests.

---

## Phase 4 — Api host (Web API + 1 MVC view) (≈ 2 h)

**Goal:** HTTP endpoints + Swagger + auth + a Razor view to satisfy "ASP.NET MVC".

- [ ] `Program.cs`: DI registration, `AddControllersWithViews()`, `AddAuthentication().AddJwtBearer(...)`, `AddAuthorization()`, Swagger, CORS for the SPA, exception middleware.
- [ ] **Web API controllers** (`/api/v1/...`):
  - `AuthController`: `POST /auth/register`, `POST /auth/login`.
  - `TasksController`: `GET`, `GET/{id}`, `POST`, `PUT/{id}`, `DELETE/{id}` — all `[Authorize]`.
  - `PingController`: `GET /ping` (anonymous), `GET /ping/secure` (authorized) — satisfies "authorized + non-authorized endpoints".
- [ ] **MVC controller**: `HomeController` returning a Razor view describing the demo + linking to Swagger and the SPA.
- [ ] `WebApplicationFactory` integration tests: register → login → create task → list → update → delete; plus 401 on `tasks` without token.
- [ ] Commit: `feat(api): controllers, auth, swagger, mvc home view`.

**Exit state:** `dotnet run --project src/TaskFlow.Api` serves Swagger; integration tests green.

---

## Phase 5 — React frontend (≈ 3 h)

**Goal:** Responsive SPA with auth + task CRUD.

- [ ] In `src/TaskFlow.Web`: `npm create vite@latest . -- --template react-ts`.
- [ ] Add Tailwind, React Router, React Query, Axios, React Hook Form, Zod, Vitest + RTL.
- [ ] Pages: `/login`, `/register`, `/tasks` (list + create + edit modal + delete confirm).
- [ ] `apiClient.ts` with JWT interceptor; `authStore` (Zustand or Context) persists token in `localStorage`.
- [ ] Tests: at least one component test (TaskList) and one hook test (useTasks).
- [ ] `.env.development`: `VITE_API_BASE=http://localhost:5080`.
- [ ] Commit: `feat(web): react spa with auth and task crud`.

**Exit state:** `npm run dev` shows login → tasks flow against the running API.

---

## Phase 6 — Docker + CI (≈ 1.5 h)

**Goal:** One-command demo + green CI badge.

- [ ] `Dockerfile.api` — multi-stage `sdk → runtime`.
- [ ] `Dockerfile.web` — `node:20 → nginx:alpine` static build.
- [ ] `docker-compose.yml`: services `db` (postgres:16), `api`, `web`. Healthchecks. Wait-for-db on api.
- [ ] `.github/workflows/ci.yml`: matrix on `ubuntu-latest`, steps: setup-dotnet 8, setup-node 20, restore, build, test, frontend lint + vitest. Cache NuGet + npm.
- [ ] Add status badge to README.
- [ ] Commit: `ci: github actions build and test`.

**Exit state:** `docker compose up` brings everything live on http://localhost:8080 (web) + http://localhost:5080 (api); CI green on `main`.

---

## Phase 7 — Documentation + presentation (≈ 1.5 h)

**Goal:** Make the panel love you in 5 minutes.

- [ ] `README.md` (top-level) sections:
  1. **Project & user story** (the informal story you invented — 1 paragraph).
  2. **Architecture diagram** (Mermaid) + dependency rule explanation.
  3. **Tech choices & trade-offs** (why Npgsql, why no MediatR, why React + Vite).
  4. **Run locally**: `docker compose up` + seeded credentials box.
  5. **Run from source**: `dotnet run` + `npm run dev`.
  6. **Test**: `dotnet test` + `npm run test`.
  7. **API reference**: link to Swagger + sample curl.
  8. **GenAI section** (see `docs/GENAI.md` summary inline).
  9. **What I would do with more time**: list 5 concrete items (rate limiting, refresh tokens, OpenTelemetry, role-based authz, e2e Playwright tests).
- [ ] `docs/USER-STORY.md`: 3–5 short stories (As a user, I want to…).
- [ ] `docs/ARCHITECTURE.md`: layered diagram + ADR-style rationale.
- [ ] `docs/GENAI.md`: the prompts you used (Section §B), sample AI output, validation notes, edge cases handled. **The brief grades this explicitly.**
- [ ] Commit: `docs: readme, architecture, user story, genai`.

---

## Phase 8 — Final polish & submission (≈ 1 h)

- [ ] Run full test suite + manual smoke (register, login, CRUD, logout, 401).
- [ ] `dotnet format` and `npm run lint -- --fix`.
- [ ] Verify zero browser console warnings on the SPA (the brief mentions this).
- [ ] Bump version, tag `v1.0.0`, push tag.
- [ ] Re-read brief — tick every requirement.
- [ ] Send the email reply: confirm receipt + GitHub link.

---

## Phase 9 — Verification & GenAI guardrail (≈ 30 min)

**Goal:** prove to the panel that GenAI was used to *build* **and** to *verify*. Generation without verification is speculation.

- [ ] Re-run the verification battery from `.github/copilot-instructions.md` §10:
  - `dotnet build TaskFlow.sln -c Release` → 0 warnings / 0 errors
  - `dotnet test TaskFlow.sln -c Release --no-build` → 100% pass
  - `dotnet format TaskFlow.sln --verify-no-changes` → `format: OK`
  - `npm run build` in `src/TaskFlow.Web` → `dist/` emitted, no TS errors
- [ ] Audit Phase 0–8 file inventory via `git log --since="24 hours ago"`.
- [ ] Author `docs/PHASE-9-VERIFICATION.md` with the evidence table.
- [ ] Append **Section 10 — Verification Discipline (GenAI guardrail)** to `.github/copilot-instructions.md`.
- [ ] Update `docs/JOURNAL.md` with the verification checkpoint.
- [ ] Commit: `docs(phase-9): verification report + GenAI verification guardrail`.

**Exit state:** repo at `v1.0.0` is reproducibly verifiable in < 5 minutes by the panel.

---

## §A — Bootstrap commands (copy/paste)

```pwsh
cd C:\Users\Fercho\source\repos\taskflow-clean-challenge

dotnet new sln -n TaskFlow

dotnet new classlib  -n TaskFlow.Domain         -o src/TaskFlow.Domain         -f net8.0
dotnet new classlib  -n TaskFlow.Application    -o src/TaskFlow.Application    -f net8.0
dotnet new classlib  -n TaskFlow.Infrastructure -o src/TaskFlow.Infrastructure -f net8.0
dotnet new webapi    -n TaskFlow.Api            -o src/TaskFlow.Api            -f net8.0 --use-controllers

dotnet new xunit -n TaskFlow.Domain.Tests         -o tests/TaskFlow.Domain.Tests         -f net8.0
dotnet new xunit -n TaskFlow.Application.Tests    -o tests/TaskFlow.Application.Tests    -f net8.0
dotnet new xunit -n TaskFlow.Infrastructure.Tests -o tests/TaskFlow.Infrastructure.Tests -f net8.0
dotnet new xunit -n TaskFlow.Api.Tests            -o tests/TaskFlow.Api.Tests            -f net8.0

dotnet sln add (Get-ChildItem -r -i *.csproj).FullName

dotnet add src/TaskFlow.Application/TaskFlow.Application.csproj         reference src/TaskFlow.Domain/TaskFlow.Domain.csproj
dotnet add src/TaskFlow.Infrastructure/TaskFlow.Infrastructure.csproj   reference src/TaskFlow.Application/TaskFlow.Application.csproj
dotnet add src/TaskFlow.Api/TaskFlow.Api.csproj                         reference src/TaskFlow.Infrastructure/TaskFlow.Infrastructure.csproj
dotnet add src/TaskFlow.Api/TaskFlow.Api.csproj                         reference src/TaskFlow.Application/TaskFlow.Application.csproj

dotnet add tests/TaskFlow.Domain.Tests          reference src/TaskFlow.Domain
dotnet add tests/TaskFlow.Application.Tests     reference src/TaskFlow.Application
dotnet add tests/TaskFlow.Infrastructure.Tests  reference src/TaskFlow.Infrastructure
dotnet add tests/TaskFlow.Api.Tests             reference src/TaskFlow.Api

# common test packages
$tests = @(
  "tests/TaskFlow.Domain.Tests",
  "tests/TaskFlow.Application.Tests",
  "tests/TaskFlow.Infrastructure.Tests",
  "tests/TaskFlow.Api.Tests"
)
foreach ($t in $tests) {
  dotnet add $t package FluentAssertions
  dotnet add $t package Moq
}
dotnet add tests/TaskFlow.Infrastructure.Tests package Testcontainers.PostgreSql
dotnet add tests/TaskFlow.Api.Tests package Microsoft.AspNetCore.Mvc.Testing

# infrastructure deps
dotnet add src/TaskFlow.Infrastructure package Npgsql
dotnet add src/TaskFlow.Infrastructure package dbup-postgresql
dotnet add src/TaskFlow.Infrastructure package Microsoft.AspNetCore.Identity
dotnet add src/TaskFlow.Infrastructure package Microsoft.IdentityModel.Tokens
dotnet add src/TaskFlow.Infrastructure package System.IdentityModel.Tokens.Jwt

# application deps
dotnet add src/TaskFlow.Application package FluentValidation

# api deps
dotnet add src/TaskFlow.Api package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add src/TaskFlow.Api package Swashbuckle.AspNetCore

dotnet build
```

---

## §B — GenAI prompts checklist (paste into `docs/GENAI.md` as you go)

1. *Initial scaffold prompt* — used to generate Domain entities; validated by writing the failing tests first.
2. *Repository prompt* — asked for an `NpgsqlTaskRepository` skeleton; corrected to use `NpgsqlDataSource` and parameterized SQL.
3. *JWT prompt* — verified token claims and signing key length (≥256 bits) manually.
4. *React TaskList prompt* — corrected to use React Query and to debounce search.
5. *Edge cases* — explicitly asked the model for: invalid email, expired JWT, concurrent updates, SQL injection vectors, password complexity. Verified with tests.

---

## Risk register

| Risk | Mitigation |
|---|---|
| Time slip on React | Cut Tailwind for plain CSS modules; ship list-only first, edit-modal second. |
| Testcontainers slow on Windows | Skip integration tests in CI; run locally; document in README. |
| JWT misconfig | Use a known-good sample; test with `WebApplicationFactory` 401 + 200 paths. |
| `TreatWarningsAsErrors` blocking trivial pushes | Per-project `<NoWarn>` only with comment; never globally. |
| Last-minute deploy attempt | **Skip deploy.** Local `docker compose up` is sufficient and lower risk. |

---

## Daily checkpoint template

At the end of each working session, write 3 bullets in `docs/JOURNAL.md`:
- ✅ What I finished.
- 🚧 What is in progress (and the next concrete step).
- ❓ Open questions / risks.

This is your resume point if you have to stop mid-phase.
