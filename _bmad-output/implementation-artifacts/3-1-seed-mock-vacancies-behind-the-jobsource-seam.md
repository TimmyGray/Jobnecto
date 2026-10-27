# Story 3.1: Seed mock vacancies behind the JobSource seam

Status: ready-for-dev

## Story

As a **product team**,
I want **realistic seeded vacancies behind a swappable source seam**,
so that **the vacancy board has data now and can be swapped for real ingestion later without touching the client or the generation contract**.

<!-- Track: [SEED] (backend only) — Covers: FR26 (+ FR23/FR25 data prerequisite), AR9, Decision 3.7, NFR7 (synthetic only), FR41 (ownership unchanged) -->

## Decision Record

- 2026-10-27: **Vacancies are per-user, so seeding means per-user ingestion, not a global table seed.**
  - **Evidence:**
    - `Vacancy.UserId`, plus per-user state: `IsViewed`, `IsChosen`, `IsHidden`, `MatchScore`.
    - `FilterVacanciesQueryHandler` filters on `v.UserId == request.UserId` (`VacancyRepository.GetFilteredAsync`, around line 25).
    - `GetVacancyQueryHandler` turns another user's vacancy into a 404.
    - `authorization-contract-matrix.md` row 13 makes cross-user 404 the contract.
  - **Rejected:** EF `HasData` can't create rows per user. A shared global pool would break the ownership contract and the per-user flags. A real ingestion job will also store vacancies per user.
- 2026-10-27: **Trigger (user decision): ingest on registration, plus an idempotent startup backfill for existing users.**
  - Options considered: registration + backfill, lazily on the first board load, or an explicit `POST /vacancies/sync` endpoint.
  - This needs no new API and no frontend work, the board is never empty, and it matches a future background ingestion job.
  - Both paths are gated by **`Seed:Vacancies:Enabled`**:
    - `appsettings.json` default: `false`
    - `appsettings.Development.json`: `true`
    - `Test` environment: unset, so off. Existing vacancy and authorization tests that count a user's vacancies stay unaffected.
- 2026-10-27: **Seam shape (Clean Architecture):**
  - **Application** owns the contracts:
    - `IJobSource`: returns `JobPosting` records, an Application DTO. A real adapter maps an external API to `JobPosting` and never touches EF or `Vacancy`.
    - `IVacancyIngestionService.IngestForUserAsync(userId, ct)`: maps `JobPosting` to `Vacancy`, de-duplicates, and saves once.
  - **`JobNecto.Infrastructure.JobSources`** (an empty project today; it is already in `JobNecto.slnx` and referenced by the API and test projects) holds the adapter and wiring:
    - `StaticJobSource : IJobSource`, which reads an **embedded JSON resource**.
    - `AddJobSources(IConfiguration)`.
    - The startup backfill as an `IHostedService`.
  - Swapping mock for real ingestion later = **changing one DI registration**. Nothing on the read path (`/vacancies` controller, handlers, `VacancyRepository.GetFilteredAsync`) changes, so the `/generate` contract (Decision 3) and the frontend are untouched (AR9, Decision 3.7).
- 2026-10-27: **Registration hook = MediatR notification**, not a direct call from `CreateUserCommandHandler`.
  - The handler publishes `UserRegisteredNotification(UserId)` through `IPublisher` **after** `SaveChangesAsync`.
  - `SeedVacanciesOnUserRegisteredHandler` (Application) checks the option and ingests.
  - This keeps user creation unaware of vacancies.
  - This is the **first** `INotification` in the codebase. `AddMediatR(... RegisterServicesFromAssembly(Application))` already registers notification handlers; verify with a test.
  - **Failure isolation:** seeding must never fail sign-up or startup. The notification handler and the hosted service catch, log a warning (`ILogger`), and continue. MediatR re-throws notification-handler exceptions by default, so the catch must be inside the handler.
- 2026-10-27: **Idempotency key = `JobSource.Url` per user.**
  - `JobSource` is stored as a JSON string through a value converter (`VacancyConfiguration`, around line 81), so EF can't query inside it.
  - Load the user's existing source URLs with a projection (`IVacancyRepository.GetJobSourceUrlsAsync(userId, ct)`), then skip matching postings in memory.
  - At 40 rows per user this is trivial.
- 2026-10-27: **The catalogue:**
  - **Size and source:** 40 synthetic vacancies; `JobSource.Name = "Jobnecto Demo Catalogue"`; `JobSource.Url = https://jobs.example.test/demo/{slug}` (the reserved `.test` TLD, never a real site).
  - **Paging:** 40 rows = two full pages of 20, so load-more is exercised in Story 3.2.
  - **Timestamps:** set at ingestion as `CreatedAt = UpdatedAt = utcNow - (index × 6h)`, so the default `createdAt` sort is deterministic and looks realistic. Don't rely on the DB default: InMemory test databases don't apply `defaultValueSql`.
  - `MatchScore` is `null` and all flags are `false`.
- 2026-10-27: **Out of scope:**
  - real ingestion, Quartz, and scheduling
  - the frontend (board = 3.2, filter = 3.3, detail = 3.4)
  - match scoring
  - a sync endpoint
  - removing or refreshing seeded vacancies, and catalogue versioning (a changed catalogue only adds postings with new URLs)

## Acceptance Criteria

1. **The seam exists**
   **Given** the Application layer
   **When** the seed track is built
   **Then** `IJobSource` (returning `JobPosting` records) and `IVacancyIngestionService` live in `JobNecto.Application`, with no EF or Infrastructure types
   **And** `StaticJobSource` in `JobNecto.Infrastructure.JobSources` returns the 40-posting catalogue from an embedded resource (AR9)

2. **A new user gets the catalogue**
   **Given** `Seed:Vacancies:Enabled = true`
   **When** a user registers (`POST /api/v1/users` → `201`)
   **Then** that user's `POST /api/v1/vacancies/filter` browse (empty criteria) returns `totalCount: 40`, a first page of 20 and `hasNext: true`, and `GET /api/v1/vacancies/{id}` returns `200` for those ids (FR26, FR23, FR25)
   **And** ownership is unchanged: another user's `GET` of those ids is `404` (FR41, matrix row 13)

3. **Ingestion is idempotent, and the backfill covers existing users**
   **Given** a user who already has the catalogue
   **When** ingestion runs again (registration retry or startup backfill)
   **Then** no duplicates are created and the method reports 0 added
   **And** on startup with seeding enabled, every existing non-deleted user without the catalogue ends up with exactly the 40 postings

4. **Disabled means no-op**
   **Given** `Seed:Vacancies:Enabled` is false or missing (including the `Test` environment)
   **When** a user registers or the app starts
   **Then** no vacancies are created and the existing test suite's vacancy counts are unaffected

5. **Failure isolation**
   **Given** ingestion throws (for example, the database is unavailable mid-ingest, or the source fails)
   **When** it runs during registration or at startup
   **Then** registration still returns `201` with the auth cookie, startup completes, and a warning is logged with the user id and exception, never credentials

6. **The data is synthetic and filterable** (NFR7, PRD domain requirements)
   **Given** the catalogue
   **When** it is loaded
   **Then**:
   - every enum value parses: `WorkLocationType`, `WorkTimeType`, `Location`, `Currency`
   - every row has `SalaryMin ≤ SalaryMax` when both are set
   - every URL is unique and under `https://jobs.example.test/`
   - no description contains an email address, a phone-number pattern or a real person's name
   - company names are invented
   **And** the set gives filters something to hit: all 3 work-location types, ≥ 4 work-time types, ≥ 6 distinct locations, ≥ 4 currencies, ≥ 4 rows with no salary, and skills that overlap the résumé seed vocabulary (for example `Angular`, `TypeScript`, `C#`, `.NET`, `React`, `Python`, `SQL`, `AWS`, `Docker`), so Epic 3's filters and Epic 4's grounding have hits

7. **Swap-ready**
   **Given** a future real adapter
   **When** it replaces `StaticJobSource`
   **Then** only the DI registration changes. This story makes **no** changes to `VacanciesController`, `FilterVacanciesQuery*`, `GetVacancyQuery*` or the read methods of `VacancyRepository` (AR9, Decision 3.7)

## Tasks / Subtasks

- [ ] Task 0: Test-first baseline
  - [ ] Confirm the backend is green before changing anything: `dotnet test backend/JobNecto.slnx` (expect 670 passing at `50c33bc`).

- [ ] Task 1: Application contracts and DTO (AC: 1, 7)
  - [ ] `backend/src/JobNecto.Application/JobSources/JobPosting.cs` (CREATE): `public sealed record JobPosting`, with the fields needed to build a `Vacancy`:
    - `Title`, `Description`, `Company`, `CompanyWebsite?`
    - `Location?`, `WorkTimeType?`, `WorkLocationType?`, `JobCategories`, `Skills`
    - `SalaryMin?`, `SalaryMax?`, `Currency?`, `ExperienceLevel?`
    - `SourceName`, `SourceUrl` (required)
    - Use the Domain enums directly; Application already references Domain.
    - XML-document every field.
  - [ ] `backend/src/JobNecto.Application/JobSources/IJobSource.cs` (CREATE): `Task<IReadOnlyList<JobPosting>> FetchAsync(CancellationToken ct)`, plus a `string Name { get; }`.
  - [ ] `backend/src/JobNecto.Application/JobSources/IVacancyIngestionService.cs` + `VacancyIngestionService.cs` (CREATE). `Task<int> IngestForUserAsync(Guid userId, CancellationToken ct)`:
    - Fetch the postings.
    - Load the existing URLs with `GetJobSourceUrlsAsync`.
    - Map each new posting to a `Vacancy`, with `UserId`, `JobSource = new JobSource { Name, Url }`, and staggered `CreatedAt`/`UpdatedAt` from an injected `TimeProvider` (register `TimeProvider.System`). Use `TimeProvider` rather than `DateTime.UtcNow`, so the stagger is testable.
    - Call `CreateAsync` for each, then `SaveChangesAsync` **once**, and return the number added.
    - Leave out the per-posting index in the stagger for postings that already exist, so re-ingesting an extended catalogue doesn't reorder existing rows.
  - [ ] `backend/src/JobNecto.Application/JobSources/VacancySeedOptions.cs` (CREATE): `public sealed class VacancySeedOptions { public const string SectionName = "Seed:Vacancies"; public bool Enabled { get; set; } }`.
  - [ ] Register `IVacancyIngestionService` in `backend/src/JobNecto.Application/ApplicationCollectionExtensions.cs` (`AddApplication`, where MediatR is registered; UPDATE).

- [ ] Task 2: Repository additions, read-only and no schema change (AC: 3, 7)
  - [ ] `backend/src/JobNecto.Application/Interfaces/IVacancyRepository.cs` + `backend/src/JobNecto.Infrastructure/Repositories/VacancyRepository.cs` (UPDATE): `Task<IReadOnlyCollection<string>> GetJobSourceUrlsAsync(Guid userId, CancellationToken ct)`.
    - Project `v.JobSource` for the user's vacancies, **including soft-deleted ones** (`IgnoreQueryFilters()`), so a vacancy the user removed is never resurrected. Collect the non-null `Url`s in memory.
    - Don't touch `GetFilteredAsync`.
  - [ ] `backend/src/JobNecto.Application/Interfaces/IUserRepository.cs` + `backend/src/JobNecto.Infrastructure/Repositories/UserRepository.cs` (UPDATE): `Task<IReadOnlyList<Guid>> GetAllIdsAsync(CancellationToken ct)`, for non-deleted users (global filter on).
  - [ ] No migration. Verify with `dotnet ef migrations has-pending-model-changes` or an empty `migrations add` dry run, and record the result in the Debug Log.

- [ ] Task 3: Registration notification (AC: 2, 4, 5)
  - [ ] `backend/src/JobNecto.Application/Users/UserRegisteredNotification.cs` (CREATE): `public sealed record UserRegisteredNotification(Guid UserId) : INotification`.
  - [ ] `backend/src/JobNecto.Application/Users/CreateUserCommandHandler.cs` (UPDATE):
    - Inject `IPublisher`.
    - After `SaveChangesAsync`, call `await _publisher.Publish(new UserRegisteredNotification(user.Id), cancellationToken)`.
    - The result and the 409/400 behaviour stay unchanged.
  - [ ] `backend/src/JobNecto.Application/JobSources/SeedVacanciesOnUserRegisteredHandler.cs` (CREATE): `INotificationHandler<UserRegisteredNotification>`.
    - Inject `IOptions<VacancySeedOptions>`, `IVacancyIngestionService` and `ILogger<…>`.
    - If disabled, return.
    - Otherwise `try { ingest } catch (Exception ex) when (ex is not OperationCanceledException) { log warning with UserId }`.

- [ ] Task 4: Static adapter, catalogue and wiring (AC: 1, 6, 7)
  - [ ] `backend/src/JobNecto.Infrastructure.JobSources/SeedData/demo-vacancies.json` (CREATE): 40 postings meeting every AC6 rule.
    - Enum values as strings; `System.Text.Json` with `JsonStringEnumConverter` and case-insensitive matching.
    - Mark it `<EmbeddedResource Include="SeedData\demo-vacancies.json" />` in the `.csproj`.
    - Write realistic descriptions of 3–6 sentences: role, team, stack, responsibilities and requirements. Company names must be invented, so don't use real brands or Microsoft sample names like Contoso or Northwind.
    - `ExperienceLevel` comes from a small fixed set: `Junior`, `Middle`, `Senior`, `Lead`.
  - [ ] `backend/src/JobNecto.Infrastructure.JobSources/Static/StaticJobSource.cs` (CREATE): `IJobSource` with `Name = "Jobnecto Demo Catalogue"`.
    - Load and deserialize the resource **once**, using a `Lazy<>` and a singleton lifetime.
    - Map to `JobPosting`, setting `SourceName` and `SourceUrl`.
    - A missing or invalid resource throws `InvalidOperationException` with a clear message.
  - [ ] `backend/src/JobNecto.Infrastructure.JobSources/Seeding/VacancySeedBackfillService.cs` (CREATE): an `IHostedService`.
    - `StartAsync`: if disabled, return. Otherwise create a scope, call `GetAllIdsAsync`, then `IngestForUserAsync` for each user in sequence.
    - Log one info line with the user count and rows added. Catch per user, so one failure doesn't stop the rest, and wrap the whole run so startup never fails.
    - `StopAsync`: a no-op.
  - [ ] `backend/src/JobNecto.Infrastructure.JobSources/DependencyInjection.cs` (CREATE): `AddJobSources(this IServiceCollection, IConfiguration)`, which:
    - calls `Configure<VacancySeedOptions>(configuration.GetSection(VacancySeedOptions.SectionName))`
    - registers `AddSingleton<IJobSource, StaticJobSource>()`
    - registers `AddHostedService<VacancySeedBackfillService>()`
    - Namespace: `JobNecto.Infrastructure.JobSources`.
  - [ ] `backend/src/JobNecto.API/Program.cs` (UPDATE): `builder.Services.AddJobSources(builder.Configuration);` after `AddInfrastructure`.
  - [ ] `backend/src/JobNecto.API/appsettings.json` (UPDATE) `"Seed": { "Vacancies": { "Enabled": false } }`, and `appsettings.Development.json` (UPDATE) `true`. Leave `appsettings.Test.json` without the key.

- [ ] Task 5: Tests (AC: all)
  - [ ] `backend/tests/JobNecto.Tests/Infrastructure/JobSources/StaticJobSourceTests.cs` (CREATE):
    - Returns exactly 40 postings.
    - Every AC6 rule is checked in its own test: enum validity, `min ≤ max`, unique `.test` URLs, no email or phone regex matches (`@`, `\+?\d[\d\s-]{7,}`), and the coverage thresholds (work-location types, work-time types, locations, currencies, null-salary count, skills overlap).
    - Calling twice returns the same data (cached).
  - [ ] `backend/tests/JobNecto.Tests/Application/JobSources/VacancyIngestionServiceTests.cs` (CREATE), with Moq'd `IUnitOfWork`/repositories, a fake `IJobSource` and a fake `TimeProvider`, in the style of `FilterVacanciesQueryHandlerTests`:
    - A new user gets all postings added, with the right `UserId` and `JobSource`, staggered timestamps, and `SaveChangesAsync` called once.
    - Postings whose URL already exists are skipped, and the return value counts only new ones.
    - When everything exists, nothing is added (assert `SaveChangesAsync` is not called, or is a harmless no-op; pick one and pin it).
    - The source throwing propagates. The *callers* isolate failures, not the service.
  - [ ] `backend/tests/JobNecto.Tests/Application/JobSources/SeedVacanciesOnUserRegisteredHandlerTests.cs` (CREATE):
    - Disabled → the ingestion service is never called.
    - Enabled → called with the notification's `UserId`.
    - Ingestion throws → no exception escapes and a warning is logged.
    - `OperationCanceledException` is **not** swallowed.
  - [ ] `backend/tests/JobNecto.Tests/Application/Users/CreateUserCommandHandlerTests.cs` (UPDATE): publishes `UserRegisteredNotification` with the new id exactly once, **after** save (`MockSequence` or callback ordering), and does not publish on 409 or validation failure.
  - [ ] `backend/tests/JobNecto.Tests/Infrastructure/JobSources/VacancySeedBackfillServiceTests.cs` (CREATE):
    - Disabled → no repository calls.
    - Enabled → ingests for each user id.
    - One user failing doesn't stop the others.
    - Total failure, e.g. `GetAllIdsAsync` throwing → `StartAsync` completes.
  - [ ] Repository tests, following the existing `backend/tests/JobNecto.Tests/Infrastructure/VacancyRepositoryTests.cs` style:
    - `GetJobSourceUrlsAsync` returns URLs for that user only, and includes soft-deleted vacancies.
    - `GetAllIdsAsync` excludes soft-deleted users.
  - [ ] `backend/tests/JobNecto.Tests/API/Vacancies/VacancySeedingApiTests.cs` (CREATE): a factory with seeding **enabled** (see Trap 3).
    - Register → filter browse gives `totalCount 40`, 20 items and `hasNext`; the second page gives 20 and `hasNext: false`.
    - `GET` of a returned id → 200; another user's `GET` of that id → 404.
    - A second user also gets 40 of their own (different ids).
    - Plus one test with the **default** factory: register → `totalCount 0` (AC4).
  - [ ] Add a DI smoke test that `IPublisher` resolves the `UserRegisteredNotification` handler from the real container. The API test above covers this implicitly; if so, note it.

- [ ] Task 6: Docs hygiene
  - [ ] `_bmad-output/planning-artifacts/architecture/demo-mvp-architecture-decisions.md` (UPDATE), under §3.7 "What stays unchanged": add one paragraph naming the seam (`IJobSource`/`JobPosting`/`IVacancyIngestionService`), the per-user ingestion model, the `Seed:Vacancies:Enabled` flag and its two triggers, and "swap = one DI registration".
  - [ ] `README.md` (UPDATE): one line under local development saying that in Development, every user gets the 40-vacancy demo catalogue on sign-up and at startup.

- [ ] Task 7: Verification and test coverage (AC: all)
  - [ ] `dotnet test backend/JobNecto.slnx`: all pass.
  - [ ] `dotnet build backend/JobNecto.slnx --configuration Release --warnaserror`: 0 warnings.
  - [ ] Backend coverage gate: `dotnet test backend/JobNecto.slnx --collect:"XPlat Code Coverage" --settings backend/coverlet.runsettings --results-directory ./coverage/backend`, then `python scripts/check_coverage.py ./coverage/backend --threshold 80`. Use `python`, not `python3`. The new `JobSources` project must meet ≥80% per file. If `JobNecto.Infrastructure.JobSources` isn't picked up by `coverlet.runsettings`, find out why before excluding anything. The JSON resource isn't code.
  - [ ] Live check against a **throwaway** Postgres, never the developer's database; see Dev Notes → *Running the API safely*:
    - Start in Development with the flag on, register two users through the API, and confirm 40 vacancies each, paging and cross-user 404.
    - Restart the API: the backfill adds 0 (idempotent).
    - Register a user with the flag off: 0 vacancies.
    - Record the results in Completion Notes.
  - [ ] Frontend untouched: `git diff --stat` shows no `frontend/` changes, so the frontend gates aren't needed. Say so explicitly.

- [ ] Task 8: PR Review (RV) recorded before merge (AGENTS.md completion checklist item 5)
  - [ ] `jobnecto-qa` (or `bmad-code-review`) runs the adversarial review on the PR and fills in `## PR Review (RV)` below. Nothing merges before that's recorded; a self-review doesn't count.

## Dev Notes

### Technical Requirements

- **Clean Architecture:** Application holds `IJobSource`, `JobPosting`, the ingestion service, the notification and its handler, and the options class. Infrastructure.JobSources holds the adapter, resource, hosted service and DI. Infrastructure holds only the two repository methods.
  - No EF types in Application (agent-learning 2026-05-10 "Keep EF Core types out of Application handlers").
  - No `Vacancy` construction inside `JobNecto.Infrastructure.JobSources`; the adapter emits `JobPosting` only.
- **Namespaces match folders** (AGENTS.md), e.g. `JobNecto.Infrastructure.JobSources.Static`, `JobNecto.Infrastructure.JobSources.Seeding`, `JobNecto.Application.JobSources`.
- XML-doc every public type and every non-trivial method (AGENTS.md, `function-comments` skill).
- Don't add Quartz, Redis or `HttpClient` usage. `Microsoft.Extensions.Http` is already referenced by the JobSources project; leave it, unused.
- **Logging:** structured `ILogger` messages with `UserId` and counts. Never log connection strings or user emails.

### API / Contract Guardrail

- **No new endpoints. No request/response shape changes.** `POST /api/v1/users` still returns `201` with the cookie. Seeding is a side effect after save, and it can never turn that into an error.
- `/api/v1/vacancies/filter` and `/api/v1/vacancies/{id}` are untouched, and their ownership contract (404 on cross-user read, matrix row 13) must still hold for seeded rows.
- `/openapi/v1.json` must be unchanged. Check by diffing it before and after with the API running, or by confirming that no controller or DTO changed.

### ⚠️ Trap 1: Existing tests count vacancies per user

`VacanciesApiTests`, `VacanciesAuthorizationTests`, `AuthorizationTestFixture` and the cover-letter tests register users and insert their own vacancies. If seeding ran in the `Test` environment, every count would be off by 40.

The flag defaults to **off**, and the `Test` environment doesn't set it. Don't "fix" those tests; if one fails, the flag leaked.

### ⚠️ Trap 2: MediatR notification exceptions propagate

`IPublisher.Publish` re-throws handler exceptions. A missing `try/catch` in `SeedVacanciesOnUserRegisteredHandler` would turn a seeding hiccup into a 500 on sign-up, after the user row was already committed, which is the worst outcome.

Catch inside the handler. Don't swallow `OperationCanceledException`.

### ⚠️ Trap 3: Enabling the flag in the API test factory

`JobNectoApiFactory` runs `UseEnvironment("Test")` and swaps in an InMemory database. For the seeding API tests, enable the flag with `builder.UseSetting("Seed:Vacancies:Enabled", "true")`, or with `ConfigureAppConfiguration` + `AddInMemoryCollection`, in a small subclass or an optional constructor argument.

Remember `appsettings.Local.json` is loaded **last** in `Program.cs`. It isn't present in CI, but if it exists locally and sets `Seed`, it would override; it shouldn't set `Seed`.

The hosted service also runs in the test host. With the flag on and an empty InMemory database it's a no-op. Make sure it doesn't throw before the database exists.

### ⚠️ Trap 4: `JobSource` is a JSON string column

`VacancyConfiguration` converts `JobSource` with `JsonSerializer`, so `v.JobSource.Url == x` cannot be translated by Npgsql, and on InMemory it would silently behave differently.

Project `v.JobSource` and read `.Url` in memory (Task 2). Don't add a computed column or migration for this story.

### ⚠️ Trap 5: Timestamps

Nothing in `AppDbContext` stamps `CreatedAt`. Postgres fills `Now()` from `defaultValueSql` only when the CLR value is default, and InMemory never does. Set `CreatedAt`/`UpdatedAt` explicitly from `TimeProvider` in the ingestion service (Decision Record: `index × 6h` stagger), or the default `createdAt` sort and keyset cursor will tie on every seeded row.

### ⚠️ Trap 6: Verify enum members before writing the JSON

`Location` has **122** members (Story 2.1 found the plan's assumption of 127 was wrong). Use only names that exist; the `StaticJobSourceTests` enum check is the guard (agent-learning 2026-05-10 "Verify enum members before writing new tests and DTO fixtures").

Verified 2026-10-27:
- `WorkTimeType` = `FullTime, PartTime, Freelance, Contract, Internship, Volunteer, Other`
- `WorkLocationType` = `OnSite, Remote, Hybrid`
- `Currency` includes `USD, EUR, GBP, PLN, UAH, CAD, CHF, SEK`
- `Location` includes `Ukraine, Poland, Germany, France, Italy, Spain, Portugal, Netherlands, Belgium, Switzerland, Austria`

### Running the API safely (live check)

Use the procedure proven in Stories 2.5 and A3:
1. Start a throwaway `postgres:16-alpine` container on a spare port (e.g. 55434).
2. Run `dotnet ef database update --project backend/src/JobNecto.Infrastructure --startup-project backend/src/JobNecto.API --connection "<conn>"`.
3. Copy `backend/src/JobNecto.API/bin/Debug/net10.0` to a temp folder **without** `appsettings.Local.json`.
4. Run `dotnet JobNecto.API.dll` there with `ASPNETCORE_ENVIRONMENT=Development`, `ConnectionStrings__Postgres=<conn>`, `JwtSettings__SecretKey=<random>`, and `Seed__Vacancies__Enabled=true|false`.

Never read, print or commit secrets. Remove the container afterwards.

### File Structure Requirements

- `backend/src/JobNecto.Application/JobSources/JobPosting.cs` (CREATE)
- `backend/src/JobNecto.Application/JobSources/IJobSource.cs` (CREATE)
- `backend/src/JobNecto.Application/JobSources/IVacancyIngestionService.cs` (CREATE)
- `backend/src/JobNecto.Application/JobSources/VacancyIngestionService.cs` (CREATE)
- `backend/src/JobNecto.Application/JobSources/VacancySeedOptions.cs` (CREATE)
- `backend/src/JobNecto.Application/JobSources/SeedVacanciesOnUserRegisteredHandler.cs` (CREATE)
- `backend/src/JobNecto.Application/Users/UserRegisteredNotification.cs` (CREATE)
- `backend/src/JobNecto.Application/Users/CreateUserCommandHandler.cs` (UPDATE)
- `backend/src/JobNecto.Application/ApplicationCollectionExtensions.cs` (UPDATE)
- `backend/src/JobNecto.Application/Interfaces/IVacancyRepository.cs`, `IUserRepository.cs` (UPDATE)
- `backend/src/JobNecto.Infrastructure/Repositories/VacancyRepository.cs`, `UserRepository.cs` (UPDATE)
- `backend/src/JobNecto.Infrastructure.JobSources/JobNecto.Infrastructure.JobSources.csproj` (UPDATE: EmbeddedResource)
- `backend/src/JobNecto.Infrastructure.JobSources/SeedData/demo-vacancies.json` (CREATE)
- `backend/src/JobNecto.Infrastructure.JobSources/Static/StaticJobSource.cs` (CREATE)
- `backend/src/JobNecto.Infrastructure.JobSources/Seeding/VacancySeedBackfillService.cs` (CREATE)
- `backend/src/JobNecto.Infrastructure.JobSources/DependencyInjection.cs` (CREATE)
- `backend/src/JobNecto.API/Program.cs`, `appsettings.json`, `appsettings.Development.json` (UPDATE)
- Tests as listed in Task 5 (CREATE / UPDATE)
- `_bmad-output/planning-artifacts/architecture/demo-mvp-architecture-decisions.md`, `README.md` (UPDATE)

### Testing Requirements

- **Unit tests:** xUnit + FluentAssertions + Moq, mocking `IUnitOfWork` → repositories as in `backend/tests/JobNecto.Tests/Application/Vacancies/FilterVacanciesQueryHandlerTests.cs`.
- **API tests:** a fresh `JobNectoApiFactory` per test (InMemory database per factory), `HandleCookies = false`, explicit auth cookie, as in `VacanciesApiTests`.
- **Compile early:** after creating each new test file, run a fast `dotnet build` (agent-learning 2026-05-21).
- **Test-first:**
  - Write `VacancySeedingApiTests` before the implementation and watch it fail with `totalCount 0`.
  - Mutation-check the failure isolation: removing the `catch` in the notification handler must turn the "ingestion throws → still 201" test red.

### Previous Story Intelligence (2.5, Epic 2 retro)

- **B1 (2.5):** MVC adds an implicit `[Required]` to non-nullable reference-type properties. `JobPosting` is not a request DTO, so this doesn't apply here, but **don't** add any new request command in this story.
- **HTTP-level tests catch what unit tests can't:** B1 was invisible below HTTP. AC2 and AC4 must have API tests, not just service tests.
- **Run every gate, and record each one** (AGENTS.md item 5, agent-learning 2026-10-26): the backend coverage gate with `python`, and the Release `--warnaserror` build.
- **Self-merge is forbidden until an RV is recorded** (AGENTS.md item 5).

### Git Intelligence Summary

- `30a871d` (PR #104, Story 2.5): nullable `CreateResumeCommand`; résumé PATCH clears. Not related to vacancies.
- Phase B vacancy work: Epic 4, stories 4-1 to 4-3 (`POST /vacancies/filter` keyset pagination with `sortBy`, `GET /vacancies/{id}`). Read `backend/src/JobNecto.Infrastructure/Repositories/VacancyRepository.cs` and `backend/tests/JobNecto.Tests/API/Vacancies/VacanciesApiTests.cs` for the contract this story must keep.
- `ce9f214` (PR #97): the `System.Security.Cryptography.Pkcs` pin. If NU1902/NU1903 appears again in CI, it's a transitive advisory, not this story.

### Latest Stack Information

- .NET 10, ASP.NET Core 10.0.3, EF Core 10 (Npgsql), MediatR 14.0.0 (`INotification` / `IPublisher`), FluentValidation 12.1.1. `TimeProvider` is in the BCL. From `_bmad-output/project-context.md`.

### References

- [Source: `_bmad-output/planning-artifacts/epics.md` - Story 3.1; AR9]
- [Source: `_bmad-output/planning-artifacts/prd-demo-mvp.md` - Tier 2 "Real UI, mocked data"; FR26; Data/seed track]
- [Source: `_bmad-output/planning-artifacts/architecture/demo-mvp-architecture-decisions.md` - §3.7 What stays unchanged]
- [Source: `_bmad-output/planning-artifacts/architecture/authorization-contract-matrix.md` - row 13 (GET /vacancies/{id})]
- [Source: `backend/src/JobNecto.Domain/Entities/Vacancy.cs`, `backend/src/JobNecto.Domain/ValueObjects/JobSource.cs`, `backend/src/JobNecto.Infrastructure/Persistance/Config/VacancyConfiguration.cs`]
- [Source: `_bmad-output/agent-learnings.md` - 2026-05-10 (EF types out of Application; verify enum members), 2026-05-21 (compile check), 2026-10-26 (never skip a gate)]

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

### File List

## PR Review (RV)

<!-- Filled by jobnecto-qa / bmad-code-review before merge (AGENTS.md completion checklist item 5). Not a self-review. -->

## Change Log

- 2026-10-27: Story created (ready-for-dev). User decision: ingest on registration + startup backfill, gated by `Seed:Vacancies:Enabled`.
