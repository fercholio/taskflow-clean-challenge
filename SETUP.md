# SETUP — Demo Guide

> **Goal:** prove the brief was met by **running the verification battery and a live end-to-end flow in under 5 minutes**, not by reading code.

This document describes the exact steps that are executed during the demo. Everything is automated by `scripts/panel-demo.ps1` (Windows / PowerShell 7) and `scripts/panel-demo.sh` (macOS / Linux). The two scripts are mirrors — pick the one that matches your shell.

---

## 0. Prerequisites (one-time, on the demo machine)

| Tool | Version | Why |
|---|---|---|
| .NET SDK | **9.0.x** | Solution targets `net9.0`. |
| Node.js | **20+** (24 tested) | SPA build (`tsc -b && vite build`). |
| Docker Desktop | running | Postgres + API + SPA containers. |
| `git` | any recent | Repo identity / log audit. |
| PowerShell | **7+** (Windows) | The `.ps1` script. |
| `bash` / `curl` | any recent (macOS / Linux) | The `.sh` script. |

Check the daemon is up:

```pwsh
docker info > $null; if ($LASTEXITCODE -ne 0) { Write-Host "Start Docker Desktop first." -ForegroundColor Red }
```

---

## 1. The one-command demo

From the repo root, with Docker Desktop running:

### Windows (PowerShell 7)
```pwsh
.\scripts\panel-demo.ps1
```

### macOS / Linux
```bash
chmod +x scripts/panel-demo.sh scripts/panel-demo-down.sh   # first time only
./scripts/panel-demo.sh
```

That single command performs **every gate that needs to be seen**, prints a green `PASS` per step, and exits non-zero on the first failure (so it is obvious which gate caught it). Total runtime: ~2–4 minutes after a warm Docker cache.

---

## 2. What the script proves, step by step

Each step in the script maps directly to a brief requirement.

| # | Step | What it proves | Brief requirement |
|---|---|---|---|
| 1 | **Pre-flight** — checks `dotnet`, `docker`, `npm`, `git` are present and Docker daemon is up. | Reproducibility on any machine. | "runs locally" |
| 2 | **Repo identity** — branch, latest tag (`v1.0.0`), last 10 commits. | Conventional commits, discrete phases, no force-push. | "evaluation: code quality" |
| 3 | **`dotnet build -c Release`** | Solution compiles cleanly with `TreatWarningsAsErrors=true`. | ".NET 9 / clean build" |
| 4 | **`dotnet test -c Release`** — runs **43 tests** across 4 projects. | TDD coverage on Domain (24), Application (12), Infrastructure (3), Api (4). | "tests" |
| 5 | **`dotnet format --verify-no-changes`** | Style discipline. CI enforces the same gate. | "code quality" |
| 6 | **SPA `npm run build`** — strict TS (`tsc -b`) + Vite production build. | Frontend type-checks end-to-end against API contract. | "React frontend" |
| 7 | **`docker compose up -d`** — postgres + api + web. | One-command demo, no manual setup. | "easy to run" |
| 8 | **Health probes** on Swagger + SPA roots until 200. | The stack is actually live, not just "started". | "working app" |
| 9 | **`POST /auth/register`** — ephemeral panel user. | User registration use case (Application + Infrastructure + Api). | "auth" |
| 10 | **`POST /tasks`** — creates a task as the new user. | CRUD-Create + JWT bearer flow. | "CRUD + auth" |
| 11 | **`GET /tasks`** — list contains the new id. | CRUD-Read + per-user data isolation. | "CRUD" |
| 12 | **`DELETE /tasks/{id}`** | CRUD-Delete. | "CRUD" |
| 13 | **`GET /tasks` without token → 401** | Auth boundary actually enforced (not just decorated). | "authorized + non-authorized endpoints" |
| 14 | **`POST /auth/login` with seeded `demo@taskflow.dev`** | Seed migration + login use case work end-to-end. | "demo data" |
| 15 | **Opens** SPA + Swagger + Razor MVC home in the browser. | Web API **and** ASP.NET MVC view both present, as the brief asks. | "ASP.NET MVC + Web API" |

After step 15 the UI can be driven manually with the seeded credentials below.

---

## 3. Demo credentials & URLs

| Surface | URL |
|---|---|
| SPA (React + Vite + Tailwind) | http://localhost:8080 |
| Swagger UI | http://localhost:5092/swagger |
| Razor MVC home view | http://localhost:5092/ |
| Postgres (host machine) | `localhost:5432`  user `taskflow`  pass `taskflow`  db `taskflow` |

| User | Password | How it was created |
|---|---|---|
| `demo@taskflow.dev` | `Demo123!` | Seeded automatically by `DatabaseBootstrapper` on API startup. |
| `panel+<timestamp>@taskflow.dev` | `Panel-Demo-1!` | Created live by step 9 of the script and deleted by step 12. |

---

## 4. Manual smoke tour (after the script finishes)

Suggested 60-second click-through:

1. **Open** http://localhost:8080 → click **Login** → use the demo creds.
2. You'll see the seeded tasks. **Create** a new one — confirm it appears in the list.
3. **Edit** its status to *Done* → badge color changes.
4. **Delete** a task.
5. **Logout**.
6. Open http://localhost:5092/swagger → click **Authorize**, paste the token from `POST /auth/login`, then call `GET /tasks/{id}` and `DELETE /tasks/{id}`.
7. Open http://localhost:5092/ → shows the **Razor MVC home view** (proves the "ASP.NET MVC" leg of the brief).

---

## 5. Reading the proof artifacts (no code reading required)

Written evidence, in addition to (or instead of) the live run:

| Document | What it shows |
|---|---|
| [`README.md`](README.md) | Project summary, API surface, architecture rationale, "what I would do with more time". |
| [`docs/USER-STORY.md`](docs/USER-STORY.md) | Persona, MoSCoW stories, Gherkin acceptance criteria. |
| [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) | Mermaid diagrams + 6 ADRs (no EF, no MediatR, JWT, etc.). |
| [`docs/GENAI.md`](docs/GENAI.md) | How GitHub Copilot was used, prompts, corrections, what was **not** delegated. |
| [`docs/PHASE-9-VERIFICATION.md`](docs/PHASE-9-VERIFICATION.md) | Verification battery report — the same gates the script runs, with results. |
| [`.github/copilot-instructions.md`](.github/copilot-instructions.md) | Repo-wide GenAI guardrail (Section 10 = "verify, don't just generate"). |
| [`.github/workflows/ci.yml`](.github/workflows/ci.yml) | CI runs the **same** gates on every push: build + test + SPA build + docker build. |

---

## 6. Script options

```pwsh
.\scripts\panel-demo.ps1                # full panel run (default)
.\scripts\panel-demo.ps1 -SkipTests     # skip backend tests for a faster click-through
.\scripts\panel-demo.ps1 -SkipBuild     # skip docker rebuild (containers already built)
.\scripts\panel-demo.ps1 -StopAfter     # stop containers automatically when done
```

```bash
./scripts/panel-demo.sh                 # full panel run
SKIP_TESTS=1 ./scripts/panel-demo.sh    # skip backend tests
SKIP_BUILD=1 ./scripts/panel-demo.sh    # skip docker rebuild
STOP_AFTER=1 ./scripts/panel-demo.sh    # stop containers when done
```

---

## 7. Tear down

```pwsh
.\scripts\panel-demo-down.ps1           # stop containers, keep postgres volume
.\scripts\panel-demo-down.ps1 -Purge    # also drop the postgres volume (resets seed)
```

```bash
./scripts/panel-demo-down.sh
./scripts/panel-demo-down.sh -p
```

---

## 8. Troubleshooting

| Symptom | Cause | Fix |
|---|---|---|
| `Network Error` in the browser, `ERR_CONNECTION_REFUSED` | Containers stopped (e.g. terminal closed). | Re-run `.\scripts\panel-demo.ps1 -SkipBuild -SkipTests`. |
| `docker info` fails | Docker Desktop not running. | Start Docker Desktop, wait until tray icon is steady, retry. |
| `dotnet test` says `net9.0 not found` | .NET 9 SDK missing. | Install [https://dot.net](https://dot.net) → SDK 9.0. |
| `npm run build` complains about `@types/node` | Stale `node_modules`. | `cd src/TaskFlow.Web && rm -r node_modules && npm ci`. |
| `POST /api/v1/tasks` returns **422** | Backend validation rejected the payload (empty title, malformed `dueDateUtc`). | Check the response body — the `detail` field names the failing rule. |
| `POST /api/v1/auth/login` returns **401** | Wrong password, or the postgres volume was wiped without re-seeding. | Use `Demo123!` exactly, or run `.\scripts\panel-demo-down.ps1 -Purge` then re-run the demo script (the bootstrapper re-seeds on startup). |
| Port `5092` or `8080` already in use | Another local app holds the port. | Stop the conflicting app, or change the host-side port in `docker-compose.yml`. |

---

## 9. Project summary

TaskFlow is a Clean-Architecture .NET 9 task manager. The script in §1 runs the same gates that CI runs on every commit: build, 43 tests, format check, SPA strict-TS build, `docker compose up`, and a live register → create → list → delete flow against a freshly-seeded Postgres. Every gate prints `PASS` or `FAIL`. GitHub Copilot was used to accelerate authoring; Section 10 of `.github/copilot-instructions.md` makes verification a hard rule — that is why this script exists. When it finishes the SPA is available at `:8080` (demo user above) and Swagger at `:5092`.
