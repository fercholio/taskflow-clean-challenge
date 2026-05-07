# TaskFlow — Clean Architecture Technical Challenge

[![CI](https://github.com/fercholio/taskflow-clean-challenge/actions/workflows/ci.yml/badge.svg)](https://github.com/fercholio/taskflow-clean-challenge/actions/workflows/ci.yml)

A small Task Management system built as a .NET technical interview exercise — designed to be **read in 5 minutes** and **run with one command**.

- **Backend:** ASP.NET Core 9 (Web API + Razor MVC), C# 13, raw `Npgsql` (no ORM).
- **Frontend:** React 18 + Vite + TypeScript, TanStack React Query, Tailwind CSS.
- **Database:** PostgreSQL 16.
- **Auth:** JWT bearer tokens (HS256), `Microsoft.AspNetCore.Identity.PasswordHasher<T>` for hashing only.
- **Architecture:** Clean Architecture (Domain ← Application ← Infrastructure ← Api).
- **Testing:** xUnit, FluentAssertions, Moq, `WebApplicationFactory`, Testcontainers.

> **Hard constraints honoured throughout the codebase:** no Entity Framework, no Dapper, no MediatR. See [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) for the rationale (ADR-001).

---

## Quick links

- 🎬 **Demo script + setup → [`SETUP.md`](SETUP.md)**
- 📖 User story → [`docs/USER-STORY.md`](docs/USER-STORY.md)
- 🏛️ Architecture + ADRs → [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md)
- 🤖 GenAI process → [`docs/GENAI.md`](docs/GENAI.md)
- ✅ Verification report → [`docs/PHASE-9-VERIFICATION.md`](docs/PHASE-9-VERIFICATION.md)
- 📋 Roadmap (planning artefact) → [`docs/ROADMAP.md`](docs/ROADMAP.md)
- ⚙️ Repo-wide Copilot rules → [`.github/copilot-instructions.md`](.github/copilot-instructions.md)

## Demo credentials (after seed)

| Email | Password |
|---|---|
| `demo@taskflow.dev` | `Demo123!` |

---

## Run with Docker (one command)

```bash
cp .env.example .env
docker compose up --build
```

| Service | URL |
|---|---|
| SPA | http://localhost:8080 |
| API + Swagger | http://localhost:5092/swagger |
| Razor MVC home | http://localhost:5092/ |
| PostgreSQL | localhost:5432 (user `taskflow`, db `taskflow`) |

The SPA at `:8080` reverse-proxies `/api/*` to the API container, so the browser only talks to one origin.

## Run from source

**API**
```bash
dotnet run --project src/TaskFlow.Api
# http://localhost:5092
```

**SPA**
```bash
cd src/TaskFlow.Web
npm install
npm run dev
# http://localhost:5173 (Vite proxies /api -> :5092)
```

You will need a Postgres reachable at the connection string in `src/TaskFlow.Api/appsettings.json` — the easiest way is `docker compose up postgres`.

## Tests

```bash
# Backend (Domain, Application, Infrastructure, Api)
dotnet test

# Frontend type-check + production build
cd src/TaskFlow.Web && npm run build
```

CI runs the same gates plus a Docker image build on every push and PR — see [`.github/workflows/ci.yml`](.github/workflows/ci.yml).

---

## API surface

Versioned under `/api/v1`.

| Verb | Route | Auth | Purpose |
|---|---|---|---|
| `POST` | `/auth/register` | anon | Create user, returns JWT. |
| `POST` | `/auth/login` | anon | Sign in, returns JWT. |
| `GET` | `/ping` | anon | Liveness check. |
| `GET` | `/ping/secure` | JWT | Demonstrates the auth boundary. |
| `GET` | `/tasks` | JWT | List **my** tasks. |
| `GET` | `/tasks/{id}` | JWT | Get one of my tasks. |
| `POST` | `/tasks` | JWT | Create a task. |
| `PUT` | `/tasks/{id}` | JWT | Update a task. |
| `DELETE` | `/tasks/{id}` | JWT | Delete a task. |

Sample curl (register → create):

```bash
TOKEN=$(curl -s http://localhost:5092/api/v1/auth/register \
  -H 'content-type: application/json' \
  -d '{"email":"maya@taskflow.dev","password":"S3cret-Pass!"}' \
  | jq -r .accessToken)

curl http://localhost:5092/api/v1/tasks \
  -H "authorization: Bearer $TOKEN" \
  -H 'content-type: application/json' \
  -d '{"title":"Send invoice","description":null,"dueDateUtc":"2026-12-31T17:00:00Z"}'
```

Full schema is documented at `/swagger` when the API runs in Development.

---

## Repository layout

```
taskflow-clean-challenge/
├── src/
│   ├── TaskFlow.Domain/         Entities, value objects, domain errors. No deps.
│   ├── TaskFlow.Application/    Use-case handlers, DTOs, abstractions, Result<T>.
│   ├── TaskFlow.Infrastructure/ Npgsql repos, JWT, password hasher, migrations.
│   ├── TaskFlow.Api/            Web API + Razor MVC home view, DI, Swagger.
│   └── TaskFlow.Web/            React + Vite + TS SPA.
├── tests/                       xUnit projects per layer.
├── db/migrations/               Plain ordered .sql files applied at startup.
├── docs/                        USER-STORY, ARCHITECTURE, GENAI, ROADMAP.
├── .github/workflows/ci.yml     dotnet + node + docker pipelines.
├── docker-compose.yml           postgres + api + web.
├── Dockerfile.api               sdk:9.0 → aspnet:9.0 multi-stage.
└── Dockerfile.web               node:20 build → nginx:1.27 static.
```

See [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) for the dependency-rule diagram and ADRs.

---

## Stack rationale (short)

- **Raw `Npgsql`, no EF/Dapper.** Brief constraint; payoff is reviewable, parameterised SQL with no surprises.
- **Plain handler classes, no MediatR.** Brief constraint; payoff is a transparent request pipeline anyone can read top-to-bottom.
- **Clean Architecture.** Domain has zero dependencies. Application talks to ports only. Infrastructure is swappable.
- **TDD on Domain + Application.** Failing test first. Integration tests on Infrastructure (Testcontainers) and end-to-end via `WebApplicationFactory`.
- **React + Vite + TS.** Strict TS end-to-end gives confidence the SPA matches the API contract.
- **Tailwind CSS.** Fastest way to a polished, responsive UI without inventing a design system.

## What I would do with more time

1. **Refresh tokens + httpOnly cookie** instead of `localStorage` (mitigates XSS — flagged in ADR-003).
2. **Rate limiting** on `/auth/login` and `/auth/register`.
3. **OpenTelemetry** traces + Serilog structured logs shipped to a collector.
4. **Role-based authorisation** (admin can list users) and audit log of mutations.
5. **End-to-end tests with Playwright** covering register → create → edit → delete.
6. **Task tags, search, pagination** and SignalR-based real-time updates.

---

## License

MIT.
