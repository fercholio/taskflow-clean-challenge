# GenAI Process

The brief explicitly grades **how** GenAI is used, not just whether. This document captures the prompting workflow, the AI outputs that shipped, the corrections that were necessary, and the boundaries the human author kept in their head while reviewing.

## Tooling

- **GitHub Copilot Chat** in Visual Studio 2026 Insiders (Agent Mode for multi-step refactors, inline completions for boilerplate).
- A repository-level [`.github/copilot-instructions.md`](../.github/copilot-instructions.md) acts as a **persistent system prompt** that pins the hard constraints (no EF/Dapper/MediatR, raw Npgsql, Clean Architecture, TDD).

## Workflow

For every feature the loop was:

1. **Frame** the prompt with intent + constraint, not just "build X".
2. Ask Copilot for a **small, well-scoped** artefact (one handler, one repository method, one component).
3. **Read the output line by line.** Reject anything that violates the repo instructions.
4. **Run the tests.** Add a failing test first when touching Domain or Application.
5. Record the prompt, the deviation, and the correction in this file when it taught us something.

Copilot was never trusted with security-sensitive code (auth, password handling, SQL) without an explicit cross-check against OWASP guidance.

## Representative prompts and what we did with the output

### 1. Domain entity scaffold
> "Generate a sealed `TaskItem` entity for a Clean Architecture domain layer. Title required (max 200), description optional (max 2000), `DueDateUtc` must be UTC, default status `Pending`. No public setters. Static `Create` factory returning a `Result<TaskItem>`."

- **Kept:** sealed class, factory pattern, private setters, status enum.
- **Corrected:** initial output threw a generic `ArgumentException`; we wanted a `Result<T>` (per ADR-002) so failures stay first-class.
- **Tests written first:** `Create_WhenTitleIsEmpty_ReturnsValidationError`, `Create_WhenDueDateInPast_ReturnsValidationError`.

### 2. Npgsql repository skeleton
> "Write an `NpgsqlTaskRepository` for this `ITaskRepository` interface using `NpgsqlDataSource`. All SQL parameterised. SQL constants at the top of the file."

- **Kept:** `NpgsqlDataSource` injection, `const string` SQL declarations, `await using` connection/command scopes.
- **Corrected:** the AI initially concatenated a `userId` filter into the `WHERE` clause as interpolation — replaced with `@user_id` parameter binding. Verified by writing a test that attempted to read another user's task and asserted `null`.
- **Cross-check:** OWASP "SQL Injection" cheat sheet — every query in the repo must use named parameters. Reviewed all final repos for that property.

### 3. JWT token service
> "Implement `IJwtTokenService.Issue(User)` returning HS256 access token with claims `sub`, `email`, `jti`, `exp`. Read issuer/audience/key from `JwtOptions`."

- **Kept:** `SymmetricSecurityKey`, `SigningCredentials`, `JwtSecurityTokenHandler`.
- **Corrected:** AI suggested a 16-byte signing key; HS256 requires ≥256 bits. The repo enforces a min-32-character key in `appsettings.json` and `.env.example`. Test added: `Issue_WhenSigningKeyTooShort_Throws`.
- **Manual verification:** decoded a generated token at jwt.io with the configured key to confirm signature validity and claim shape before wiring controllers.

### 4. Controller + `Result<T>` mapping
> "Write a thin `TasksController` for these handlers. Map `Result<T>` to `IActionResult` via a shared extension. Return ProblemDetails for failures."

- **Kept:** thin controller, single `ToActionResult()` extension, `[Authorize]` at controller level.
- **Corrected:** AI added per-action try/catch — removed in favour of the global `ExceptionHandlingMiddleware` (single source of truth for errors).

### 5. React tasks page
> "Build a `TasksPage` React component using TanStack React Query 5 hooks I provide (`useTasks`, `useCreateTask`, `useUpdateTask`, `useDeleteTask`). Tailwind styling, accessible labels."

- **Kept:** React Query mutation invalidation pattern, semantic form labels, status badge map.
- **Corrected:** initial draft used `Date.now()` directly inside JSX (re-rendered every tick); replaced with stable computed values inside the form schema's defaults. Also fixed a date picker that emitted local time without converting to UTC before posting — see `toIsoUtc()` helper in `TasksPage.tsx`.

### 6. Edge cases we explicitly asked for
We forced the assistant to enumerate failure cases instead of only happy paths. Examples that produced shipped tests:

- Login with wrong password → `Unauthorized` (no user enumeration).
- Update task you don't own → `NotFound` (not `Forbidden`, to avoid existence leak).
- Token tampered with → 401 from JwtBearer middleware.
- Title >200 chars → 400 with ProblemDetails.
- Concurrent update race → repository uses `RETURNING` and a `WHERE id = @id AND user_id = @user_id` guard so a foreign update returns 0 rows affected.

## What we deliberately did NOT delegate to AI

- Choice of architecture and project boundaries (decided up front, captured in `.github/copilot-instructions.md` and ADRs).
- The dependency rule: every PR was reviewed manually for inward-only references.
- Any change to authentication, hashing, or SQL composition without re-reading the diff.
- Test names — we kept the `Method_State_ExpectedBehavior` convention by hand because AI tends to drift toward generic names like `ShouldWork`.

## Honest limitations observed

- Copilot occasionally suggested `EntityFramework`-style patterns (e.g., `DbContext`, `IQueryable`) despite the instructions file. The repo-level system prompt reduced but did not eliminate this — human review remained the safety net.
- The model sometimes generated tests that pass for the wrong reason (asserting against the mock setup instead of the production code). Each handler test was sanity-checked by mutation: change the production code and confirm the test now fails.
- Auto-completed SQL once used Postgres-incorrect syntax (`top 1` instead of `limit 1`). Caught at first run.

## Net assessment

GenAI saved hours on boilerplate (DTO records, Tailwind markup, test scaffolds) and on remembering API surfaces (Npgsql, JwtBearerOptions, React Query hooks). It did **not** make architectural decisions and was **not** trusted with security primitives without verification. The repo's `.github/copilot-instructions.md` was the single most valuable artefact for keeping suggestions on-brief.
