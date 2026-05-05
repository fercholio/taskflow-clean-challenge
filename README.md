# TaskFlow — Clean Architecture Technical Challenge

A small Task Management system built as a .NET technical interview exercise.
Backend: ASP.NET Core 8 (Web API + MVC). Frontend: React + Vite + TypeScript. Storage: PostgreSQL via raw Npgsql (no ORM). Auth: JWT.

> **Status:** scaffolding in progress. See [`docs/ROADMAP.md`](docs/ROADMAP.md) for the day-by-day plan to the submission deadline (Fri Nov 8, 14:00 MX).

## Quick links

- 📋 Roadmap → [`docs/ROADMAP.md`](docs/ROADMAP.md)
- 🤖 Copilot rules → [`.github/copilot-instructions.md`](.github/copilot-instructions.md)
- 📖 User story → `docs/USER-STORY.md` *(coming)*
- 🏛️ Architecture → `docs/ARCHITECTURE.md` *(coming)*
- 🧠 GenAI process → `docs/GENAI.md` *(coming)*

## Demo credentials (after seeding)

| Email | Password |
|---|---|
| `demo@taskflow.dev` | `Demo123!` |

## Run with Docker (one command, after Phase 6)

```bash
docker compose up --build
```

- Web: http://localhost:8080
- API + Swagger: http://localhost:5080/swagger
- Postgres: localhost:5432 (user `taskflow` / db `taskflow`)

## Run from source

```bash
# API
dotnet run --project src/TaskFlow.Api

# Web
cd src/TaskFlow.Web
npm install
npm run dev
```

## Tests

```bash
dotnet test
cd src/TaskFlow.Web && npm test
```

## Stack rationale (short)

- **No EF / Dapper / MediatR** — explicit brief constraint; demonstrates control over SQL and pipeline composition.
- **Clean Architecture** — Domain has zero deps; Application talks via ports; Infrastructure is swappable.
- **TDD** — failing test first for Domain and Application layers; integration tests with `WebApplicationFactory` and Testcontainers.
- **PostgreSQL + Npgsql** — aligns with the JD bonus stack and showcases raw ADO.NET fluency.
- **React + Vite + TS** — fastest modern SPA stack, strong typing end-to-end with the API DTOs.

## License

MIT.
