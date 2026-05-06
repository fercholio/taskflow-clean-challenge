# Copilot Instructions — TaskFlow Clean Challenge

> These instructions are the **single source of truth** for GitHub Copilot (Chat, Agent Mode, code completions) when working in this repository. Follow them strictly. If a request conflicts with these rules, **flag the conflict** and propose an alternative instead of silently breaking them.

---

## 1. Project Context

This repository is a **.NET technical interview exercise** demonstrating senior/tech-lead capability. It is a Task Management system built with:

- **Backend:** .NET 8 (LTS), ASP.NET Core Web API + ASP.NET Core MVC, C# 12.
- **Frontend:** React 18 + Vite + TypeScript.
- **Database:** PostgreSQL 16 accessed via **raw `Npgsql` / ADO.NET** (no ORM).
- **Auth:** JWT bearer tokens, custom `Users` table, password hashing via `Microsoft.AspNetCore.Identity.PasswordHasher<T>` (the hasher only — **not** EF Identity).
- **Architecture:** Clean Architecture (Domain → Application → Infrastructure → Api → Web).
- **Testing:** xUnit + FluentAssertions + Moq + `WebApplicationFactory` (integration).
- **Delivery:** Docker (multi-stage), `docker-compose` for local dev, GitHub Actions CI (build + test).
- **Methodology:** Test-Driven Development (red → green → refactor) wherever practical.

The audience for the code is a **technical interview panel** evaluating: Clean Architecture adherence, test coverage, code quality, functionality, and GenAI fluency.

---

## 2. Hard Constraints (Non-Negotiable)

These come from the official test brief. **Never** violate them — even if asked:

1. **DO NOT use Entity Framework Core, Dapper, or MediatR.** Anywhere. Not even for a quick prototype.
   - Persistence is implemented with `Npgsql` (`NpgsqlConnection`, `NpgsqlCommand`, `NpgsqlDataReader`).
   - Application orchestration uses **plain handlers/services**, not MediatR `IRequest`/`IRequestHandler`.
2. **DO NOT** put business rules in controllers or repositories. Business logic lives in `Application` (use cases) and `Domain` (entities/value objects/invariants).
3. **DO NOT** reference `Infrastructure` or `Api` from `Domain` or `Application`. Dependency rule points inward only.
4. **DO NOT** commit secrets. Use `appsettings.Development.json` (gitignored if it contains secrets) and `.env` for compose. Production secrets come from environment variables.
5. **DO NOT** generate code without an accompanying or pre-existing test when the change touches Domain or Application layers.

---

## 3. Solution Layout (authoritative)

```
taskflow-clean-challenge/
├── src/
│   ├── TaskFlow.Domain/           # Entities, Value Objects, Domain Errors, Domain Events. No deps.
│   ├── TaskFlow.Application/      # Use cases (handlers), DTOs, abstractions (IUserRepository, ITaskRepository, IPasswordHasher, IJwtTokenService, IUnitOfWork). Depends on Domain only.
│   ├── TaskFlow.Infrastructure/   # Npgsql repositories, JWT service, password hasher adapter, migrations runner (DbUp or plain SQL scripts). Depends on Application + Domain.
│   ├── TaskFlow.Api/              # ASP.NET Core host: Web API controllers + MVC controller(s) for the "MVC" requirement, DI composition, middleware, Swagger. Depends on Application + Infrastructure.
│   └── TaskFlow.Web/              # React + Vite + TS SPA. Independent build, served separately or via reverse proxy.
├── tests/
│   ├── TaskFlow.Domain.Tests/
│   ├── TaskFlow.Application.Tests/
│   ├── TaskFlow.Infrastructure.Tests/    # Integration tests against a real Postgres (Testcontainers) — optional if time-constrained, otherwise mocked.
│   └── TaskFlow.Api.Tests/               # WebApplicationFactory integration tests.
├── db/
│   ├── migrations/                # Numbered .sql files: 0001_init.sql, 0002_seed.sql, ...
│   └── seed/                      # Demo seed data (users + tasks).
├── docs/
│   ├── ROADMAP.md                 # Day-by-day plan to deadline.
│   ├── ARCHITECTURE.md            # Diagrams + ADRs.
│   ├── USER-STORY.md              # The informal user story driving development.
│   └── GENAI.md                   # Prompts used, validation, corrections (test requirement).
├── .github/
│   ├── workflows/ci.yml           # Build + test on push/PR.
│   └── copilot-instructions.md    # THIS FILE.
├── docker-compose.yml             # api + web + postgres
├── Dockerfile.api                 # Multi-stage build for Api.
├── Dockerfile.web                 # Multi-stage build for Web.
├── TaskFlow.sln
└── README.md
```

When asked to create a new file, **place it in the correct project per the above table**. If the correct project does not yet exist, scaffold it.

---

## 4. Coding Conventions

### General C#
- Target **net8.0**. Enable `Nullable`, `ImplicitUsings`, `TreatWarningsAsErrors=true`, `LangVersion=latest`.
- Prefer **records** for DTOs and value objects, **sealed classes** for entities.
- Use **primary constructors** where they reduce noise.
- File-scoped namespaces. One public type per file. PascalCase types, camelCase locals, `_camelCase` private fields.
- `using` directives outside the namespace, sorted, `System.*` first.
- Async all the way down: every I/O method returns `Task<T>` / `ValueTask<T>` and accepts a `CancellationToken` (last parameter, defaulted to `default` only at API boundary).
- **No `async void`** except event handlers. **No `.Result` / `.Wait()`**.
- Throw `DomainException` (or specific subclasses) for invariant violations; let the API layer translate to HTTP via a `ProblemDetails` exception filter.

### Domain layer
- Entities encapsulate state; **no public setters** — mutate via methods that enforce invariants.
- Constructors validate; static factory methods (`Create(...)`) return either the entity or a `Result<T>`.
- No framework attributes. No `JsonPropertyName`. No `[Table]`.

### Application layer
- One **use case per file**: `CreateTaskHandler`, `UpdateTaskHandler`, `LoginUserHandler`, etc.
- Pattern (no MediatR):
  ```csharp
  public sealed class CreateTaskHandler(ITaskRepository repo, IUnitOfWork uow)
  {
      public async Task<Result<TaskDto>> HandleAsync(CreateTaskCommand cmd, CancellationToken ct) { ... }
  }
  ```
- Register handlers as `Scoped` in DI.
- DTOs/Commands/Queries are `record`s in `Application/Tasks/`, `Application/Users/`, etc.
- Validation lives here (FluentValidation **is allowed**; it is not an ORM/mediator).

### Infrastructure layer
- Repositories use `NpgsqlDataSource` injected as singleton.
- Always parameterize queries (`@id`, `@title`). **Never** concatenate SQL.
- SQL strings live in `const string` fields at the top of the repository, named `SelectById`, `InsertTask`, etc.
- Use a transaction-aware `IUnitOfWork` that wraps `NpgsqlTransaction` for multi-step writes.
- Migrations: plain `.sql` files in `db/migrations/`, applied at startup by a small bootstrapper (or DbUp — DbUp is allowed, it is not an ORM).

### Api layer
- Controllers are **thin**: model-bind → call handler → map `Result<T>` to `IActionResult`.
- One MVC controller (e.g., `HomeController` returning a Razor view that lists demo info) to satisfy the "ASP.NET MVC" wording in the brief; the rest are `[ApiController]`.
- Versioned routes: `/api/v1/tasks`, `/api/v1/auth`.
- Swagger/OpenAPI enabled in Development.
- Global `ExceptionHandlingMiddleware` → RFC7807 `ProblemDetails`.
- `[Authorize]` by default on task endpoints; `[AllowAnonymous]` on `/auth/login` and `/auth/register`.

### Frontend (React + Vite + TS)
- Strict TS (`"strict": true`). ESLint + Prettier.
- Folder layout: `src/features/{auth,tasks}/{components,hooks,api,types}`, `src/shared/`.
- State: React Query for server state, React Context (or Zustand) for auth state. **No Redux** unless justified.
- API client: a single `apiClient.ts` using `fetch` with an interceptor that attaches the JWT and refreshes on 401.
- Forms: React Hook Form + Zod.
- Styling: Tailwind CSS (fast to ship, responsive by default).
- Components are functional, typed, and small (<150 LOC). Co-locate tests as `*.test.tsx` with Vitest + React Testing Library.

---

## 5. Testing Rules (TDD-First)

- **Red → Green → Refactor.** When implementing a Domain or Application change, write the failing test first.
- Test naming: `MethodName_StateUnderTest_ExpectedBehavior` (e.g., `Create_WhenTitleIsEmpty_ReturnsValidationError`).
- Arrange / Act / Assert with blank-line separation.
- FluentAssertions for all asserts (`result.Should().BeOfType<...>()`).
- Moq for interface fakes; **do not mock concrete classes**.
- API tests use `WebApplicationFactory<Program>` with an in-memory or Testcontainers Postgres.
- Coverage targets: Domain 95%+, Application 90%+, Infrastructure 70%+ (integration), Api smoke-tested end-to-end for happy + 401 + 400 paths.
- Every bug fix gets a regression test **first**.

---

## 6. Git & Commit Hygiene

- Branch per feature: `feat/tasks-crud`, `feat/auth-jwt`, `chore/ci`, `docs/readme`.
- **Conventional Commits**: `feat:`, `fix:`, `test:`, `refactor:`, `docs:`, `chore:`, `ci:`.
- Small, focused commits that tell a story (the panel will read the log).
- Never force-push `main`. PRs squash-merge.
- Tag the final submission `v1.0.0`.

---

## 7. GenAI Workflow (this is graded)

When Copilot generates code, the human author **must**:
1. Read it line-by-line; reject suggestions that violate Section 2.
2. Run the tests. If none exist for the touched code, write them before merging.
3. Record notable prompts and validation steps in `docs/GENAI.md` (the brief explicitly asks for this).
4. Prefer asking Copilot for *small, well-scoped* outputs (one handler, one repository method) over "build the whole feature."
5. Cross-check security-sensitive code (auth, SQL, password handling) against OWASP guidance — never trust AI output blindly here.

---

## 8. Definition of Done (per feature)

A feature is **done** only when **all** are true:
- [ ] Failing test written first, now passing.
- [ ] All layers respect the dependency rule.
- [ ] No EF / Dapper / MediatR introduced.
- [ ] No warnings (`TreatWarningsAsErrors` enforces this).
- [ ] `dotnet format` clean; ESLint/Prettier clean for the SPA.
- [ ] README updated if the public contract changed.
- [ ] Docker compose still boots `docker compose up` cleanly.
- [ ] CI green.

---

## 9. When in Doubt

- Prefer **boring, well-known patterns** over clever ones — this is a panel review.
- Optimize for **readability and testability**, not micro-perf.
- If a request would take >30 minutes of pure scaffolding, propose a smaller vertical slice first.
- If a requirement is ambiguous, ask **one** crisp clarifying question before generating code.

---

## 10. Verification Discipline (GenAI guardrail) — Phase 9

> **Generation without verification is just speculation.** This section is what we show the panel: GenAI was used to *build* **and** to *verify*.

Every GenAI-assisted change — no matter how small — must pass the **verification battery** before it is considered done:

| Gate                | Command                                                            | Pass criterion                |
|---------------------|--------------------------------------------------------------------|-------------------------------|
| Build               | `dotnet build TaskFlow.sln -c Release`                             | 0 warnings, 0 errors          |
| Tests               | `dotnet test TaskFlow.sln -c Release --no-build`                   | 100% pass                     |
| Format              | `dotnet format TaskFlow.sln --verify-no-changes`                   | `format: OK`                  |
| SPA build           | `npm run build` in `src/TaskFlow.Web`                              | `dist/` emitted, no TS errors |
| Container sanity    | `docker compose up --build` (smoke, when touching infra/Docker/CI) | API + Web + DB all healthy    |

### Rules

1. **Never claim "done" from AI output alone.** A green editor is not a green build. A green build is not a green test run.
2. **Re-run the battery after every AI-assisted change** that touches code, config, Dockerfiles, CI, or migrations.
3. **Mismatch detection is mandatory.** When AI suggests a value (TFM, package version, env var name, route), cross-check against the actual solution before accepting. The Phase 8/9 net8 → net9 mismatch is the canonical example.
4. **Evidence in the commit.** When verification reveals a fix, the commit message must reference the gate that caught it (e.g., `fix(ci): align setup-dotnet to 9.0.x — caught by docker job`).
5. **Document the verification pass at every phase boundary.** Append to `docs/JOURNAL.md` with the verification numbers (tests passed, warnings, format status). See `docs/PHASE-9-VERIFICATION.md` for the canonical template.
6. **Security-sensitive AI output gets manual review** even when the gates are green — auth flow, SQL strings, password handling, JWT signing keys. Cross-check against OWASP, never against vibes.

### What this proves to the panel

- The repository at `v1.0.0` is reproducibly verifiable in under 5 minutes.
- GenAI accelerated authoring; the **verification discipline** above is what made the output trustworthy.
- This file is the audit trail: the rules existed *before* the code, and the code was checked *against* the rules at every phase boundary.
