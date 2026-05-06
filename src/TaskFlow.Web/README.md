# TaskFlow.Web

React + Vite + TypeScript SPA for the TaskFlow Clean Challenge.

## Scripts

```bash
npm install
npm run dev      # http://localhost:5173, proxies /api -> http://localhost:5092
npm run build    # type-check + production bundle to dist/
npm run preview  # serve dist/
```

## Stack

- React 18, TypeScript (strict)
- Vite 5
- React Router 6
- TanStack React Query 5 (server state)
- Axios with JWT bearer interceptor (auto-logout on 401)
- React Hook Form + Zod (form validation)
- Tailwind CSS 3

## Layout

```
src/
  features/
    auth/   (LoginPage, RegisterPage, AuthContext)
    tasks/  (TasksPage, hooks, api, types)
  shared/   (apiClient, ProtectedRoute, layout)
```

## Env

`VITE_API_BASE_URL` defaults to `/api/v1` (uses Vite dev proxy). Override per environment if needed.
