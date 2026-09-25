# Clean Architecture boundary checklist (Jobnecto)

## Dependency direction
- [ ] `JobNecto.Domain` references no other JobNecto project and no EF, ASP.NET, or MediatR types.
- [ ] `JobNecto.Application` references only Domain (plus MediatR/FluentValidation). It has no `Infrastructure*` references and no `DbContext`.
- [ ] `Infrastructure*` projects implement interfaces declared in Application (`IUnitOfWork`, repositories, `ICoverLetterGenerator`, job sources).
- [ ] `JobNecto.API` only composes: controllers dispatch via MediatR and hold no business logic.

## Application patterns (core-architectural-decisions.md)
- [ ] One command/query + handler + validator per use case, in the feature folder.
- [ ] Validation is done through the FluentValidation pipeline, not ad hoc in handlers or controllers.
- [ ] Errors are domain/application exceptions (`NotFoundException`, `ForbiddenException`, `ValidationException`) mapped by `GlobalExceptionHandler` to RFC 7807.
- [ ] Ownership is checked in the handler, and 403 vs 404 follows `authorization-contract-matrix.md`.
- [ ] Soft delete relies on the global query filter. Any `IgnoreQueryFilters()` is intentional and commented.
- [ ] Every async method takes and forwards a `CancellationToken`.
- [ ] `UpdatedAt`/`CreatedAt` use UTC.

## Contracts
- [ ] Every endpoint declares its full `[ProducesResponseType]` set.
- [ ] Routes are versioned under `/api/v1/`.
- [ ] FE types come from the generated OpenAPI client (`frontend/src/shared/api/generated`) and are not hand-copied.

## Persistence
- [ ] EF model changes ship with a migration and an updated snapshot (see agent-learnings 2026-05-11).
- [ ] Unique indexes and case-insensitive lookups are tested against PostgreSQL, not the in-memory provider.

## Frontend
- [ ] Imports respect the feature-sliced layers (`shared` ← `entities` ← `features` ← `widgets` ← `pages` ← `processes` ← `app`). There are no upward imports.
- [ ] State uses Signals + services, with no NgRx.
- [ ] Components use design tokens, with no hardcoded colors or spacing.

## Hygiene
- [ ] Namespaces match folders.
- [ ] No secrets appear in code, config committed to git, or docs.
