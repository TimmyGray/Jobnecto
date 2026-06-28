# Jobnecto Frontend

The Jobnecto web client: an Angular 21 standalone-components SPA, feature-sliced,
themed by the Career-OS token layer. All frontend code lives in this `frontend/`
folder (sibling to `backend/`). The non-binding visual references live in
`design-examples/`.

> **Authoritative stack:** Angular standalone components, Signals + injectable
> services (no NgRx), `HttpClient` + a thin per-entity signal cache (no TanStack
> Query), typed Reactive Forms, and themeable headless primitives on Angular CDK
> (spartan-ng is deferred to the first story needing an overlay/select). See
> `docs/FRONTEND_IMPLEMENTATION_GUIDE.md` and
> `_bmad-output/planning-artifacts/architecture/demo-mvp-architecture-decisions.md`
> (Decision 1).

## Prerequisites

- Node `^20.19 || ^22.12 || >=24` and npm.
- For API type generation and live development, the backend running at
  `http://localhost:5000` (see repo root `AGENTS.md`).

## Install

```bash
npm install
```

## Development server

```bash
npm start          # ng serve, http://localhost:4200
```

The backend dev API is expected at `http://localhost:5000/api/v1` (configured in
`src/shared/config/env.ts`). The HTTP interceptor prefixes relative request URLs
with that base and sets `withCredentials: true` on every request.

## Build

```bash
npm run build      # production build into dist/
```

## Unit tests

Tests run on **Vitest** (the Angular 21 CLI default) via the
`@angular/build:unit-test` builder, using `HttpTestingController` for API mocking.

```bash
npm test           # ng test (watch)
npx ng test --no-watch   # single run (CI)
```

## Regenerating API types from OpenAPI

TypeScript DTOs/enums are **generated** from the backend OpenAPI document — never
hand-authored — so the client cannot drift from the server contract. Output lives
at `src/shared/api/generated/schema.ts`.

To regenerate (the backend must be running):

```bash
# 1. Start the backend (from repo root, in backend/src/JobNecto.API):
#    ASPNETCORE_ENVIRONMENT=Development DOTNET_URLS="http://localhost:5000" dotnet run
# 2. Then regenerate the types:
npm run gen:api
```

`gen:api` runs:

```bash
openapi-typescript http://localhost:5000/openapi/v1.json -o src/shared/api/generated/schema.ts
```

Consume generated types through the entity barrels (e.g. `@entities/user`), not
by importing `schema.ts` directly from feature code.

## Project structure (feature-sliced)

```
src/
  app/         bootstrap, providers, router skeleton
  processes/   cross-cutting lifecycles (auth)
  pages/       route-level UI (auth-sign-up, dashboard)
  widgets/     composed blocks
  features/    business actions (user/sign-up validators, …)
  entities/    domain models + signal-backed services (user)
  shared/
    api/       HTTP interceptor, ProblemDetails, generated/ OpenAPI types
    ui/        branded UI kit (TextField, Empty/Error/NotFound/Skeleton states)
    lib/       small utilities (cn)
    config/    design tokens (single source) + env
```

Path aliases (`@app/*`, `@pages/*`, `@features/*`, `@entities/*`, `@shared/*`,
`@widgets/*`, `@processes/*`) are configured in `tsconfig.json`.

## Design tokens

Token values are hand-synced across `src/shared/config/tokens.ts`,
`src/styles.scss` (CSS custom properties), and `tailwind.config.js` (Tailwind
theme mapping). Components use token-mapped Tailwind classes (e.g. `bg-canvas`,
`text-primary`, `bg-action-primary`, `text-brand-accent`) — never hardcoded
hex/px values.
