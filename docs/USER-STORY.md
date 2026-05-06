# User Story

## The story behind TaskFlow

> Maya runs a two-person product studio. Between client work, freelance gigs and personal commitments she keeps "to-do" items in three different places: sticky notes, a Slack channel with herself, and her email inbox. Items fall through the cracks every week.
>
> She wants **one private, no-friction place** where she can sign in, capture a task in seconds, mark progress, and clean up what's done — accessible from her laptop and her phone, with **her data isolated from anyone else's**.
>
> TaskFlow is the smallest possible product that solves Maya's problem honestly: an authenticated task list with status, due dates, and CRUD that never lies about persistence.

## Personas

| Persona | Goal | Pain |
|---|---|---|
| **Maya — solo operator** | Capture and track personal tasks. | Tasks scattered across tools; no single source of truth. |
| **Demo reviewer** | Validate the technical exercise quickly. | Wants seeded data and a one-command demo. |

## Stories (MoSCoW)

### Must have
1. **As a new user** I can register with an email and a password so that I get a private workspace.
2. **As a returning user** I can sign in and receive a token so that my session persists in my browser until it expires.
3. **As a signed-in user** I can create a task with a title, optional description, and due date so that I capture work the moment I think of it.
4. **As a signed-in user** I can list my own tasks (and only mine) so that I can see what's on my plate.
5. **As a signed-in user** I can update a task's title, description, due date and status so that I can keep it accurate.
6. **As a signed-in user** I can delete a task so that I can clean up cancelled or duplicated items.
7. **As any user** I am denied access to other users' tasks so that my data is private.

### Should have
8. **As a signed-in user** I can move a task through `Pending → InProgress → Done` (or cancel it) so that I can track progress meaningfully.
9. **As a reviewer** I can run the entire stack with `docker compose up` so that I don't have to install anything except Docker.
10. **As a reviewer** I can read a Razor page that explains the demo and links to Swagger so that I have one entry point.

### Could have (out of scope, listed in README "What I'd do with more time")
- Refresh tokens / password reset.
- Task tags, search, and pagination.
- Real-time updates over SignalR.
- Per-user theming and i18n.

### Won't have (this iteration)
- Multi-tenant team workspaces.
- Email or push notifications.
- Mobile native app.

## Acceptance criteria — happy path

```
GIVEN I am a registered user "maya@taskflow.dev"
WHEN  I sign in with the correct password
THEN  I receive a JWT and land on my Tasks page

GIVEN I am signed in and have no tasks
WHEN  I submit a new task with title "Send invoice" and due date tomorrow
THEN  it appears at the top of my list with status "Pending"

GIVEN I have a task in "Pending"
WHEN  I edit it and set status to "Done"
THEN  the badge updates and the change persists across reloads

GIVEN another user "ada@taskflow.dev" exists with their own tasks
WHEN  I (maya) call GET /api/v1/tasks
THEN  Ada's tasks are NOT in the response
```

## Definition of Done (per story)

- Failing test written first for any Domain or Application change.
- All four layers respect the dependency rule (Domain ← Application ← Infrastructure ← Api).
- No EF / Dapper / MediatR introduced.
- `dotnet build` and `npm run build` succeed with no warnings.
- README still describes the public contract correctly.
