# Story 3.1: Seed mock vacancies behind the JobSource seam

Status: review

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

- [x] Task 0: Test-first baseline
  - [x] Confirm the backend is green before changing anything: `dotnet test backend/JobNecto.slnx` (expect 670 passing at `50c33bc`).

- [x] Task 1: Application contracts and DTO (AC: 1, 7)
  - [x] `backend/src/JobNecto.Application/JobSources/JobPosting.cs` (CREATE): `public sealed record JobPosting`, with the fields needed to build a `Vacancy`:
    - `Title`, `Description`, `Company`, `CompanyWebsite?`
    - `Location?`, `WorkTimeType?`, `WorkLocationType?`, `JobCategories`, `Skills`
    - `SalaryMin?`, `SalaryMax?`, `Currency?`, `ExperienceLevel?`
    - `SourceName`, `SourceUrl` (required)
    - Use the Domain enums directly; Application already references Domain.
    - XML-document every field.
  - [x] `backend/src/JobNecto.Application/JobSources/IJobSource.cs` (CREATE): `Task<IReadOnlyList<JobPosting>> FetchAsync(CancellationToken ct)`, plus a `string Name { get; }`.
  - [x] `backend/src/JobNecto.Application/JobSources/IVacancyIngestionService.cs` + `VacancyIngestionService.cs` (CREATE). `Task<int> IngestForUserAsync(Guid userId, CancellationToken ct)`:
    - Fetch the postings.
    - Load the existing URLs with `GetJobSourceUrlsAsync`.
    - Map each new posting to a `Vacancy`, with `UserId`, `JobSource = new JobSource { Name, Url }`, and staggered `CreatedAt`/`UpdatedAt` from an injected `TimeProvider` (register `TimeProvider.System`). Use `TimeProvider` rather than `DateTime.UtcNow`, so the stagger is testable.
    - Call `CreateAsync` for each, then `SaveChangesAsync` **once**, and return the number added.
    - Leave out the per-posting index in the stagger for postings that already exist, so re-ingesting an extended catalogue doesn't reorder existing rows.
  - [x] `backend/src/JobNecto.Application/JobSources/VacancySeedOptions.cs` (CREATE): `public sealed class VacancySeedOptions { public const string SectionName = "Seed:Vacancies"; public bool Enabled { get; set; } }`.
  - [x] Register `IVacancyIngestionService` in `backend/src/JobNecto.Application/ApplicationCollectionExtensions.cs` (`AddApplication`, where MediatR is registered; UPDATE).

- [x] Task 2: Repository additions, read-only and no schema change (AC: 3, 7)
  - [x] `backend/src/JobNecto.Application/Interfaces/IVacancyRepository.cs` + `backend/src/JobNecto.Infrastructure/Repositories/VacancyRepository.cs` (UPDATE): `Task<IReadOnlyCollection<string>> GetJobSourceUrlsAsync(Guid userId, CancellationToken ct)`.
    - Project `v.JobSource` for the user's vacancies, **including soft-deleted ones** (`IgnoreQueryFilters()`), so a vacancy the user removed is never resurrected. Collect the non-null `Url`s in memory.
    - Don't touch `GetFilteredAsync`.
  - [x] `backend/src/JobNecto.Application/Interfaces/IUserRepository.cs` + `backend/src/JobNecto.Infrastructure/Repositories/UserRepository.cs` (UPDATE): `Task<IReadOnlyList<Guid>> GetAllIdsAsync(CancellationToken ct)`, for non-deleted users (global filter on).
  - [x] No migration. Verify with `dotnet ef migrations has-pending-model-changes` or an empty `migrations add` dry run, and record the result in the Debug Log.

- [x] Task 3: Registration notification (AC: 2, 4, 5)
  - [x] `backend/src/JobNecto.Application/Users/UserRegisteredNotification.cs` (CREATE): `public sealed record UserRegisteredNotification(Guid UserId) : INotification`.
  - [x] `backend/src/JobNecto.Application/Users/CreateUserCommandHandler.cs` (UPDATE):
    - Inject `IPublisher`.
    - After `SaveChangesAsync`, call `await _publisher.Publish(new UserRegisteredNotification(user.Id), cancellationToken)`.
    - The result and the 409/400 behaviour stay unchanged.
  - [x] `backend/src/JobNecto.Application/JobSources/SeedVacanciesOnUserRegisteredHandler.cs` (CREATE): `INotificationHandler<UserRegisteredNotification>`.
    - Inject `IOptions<VacancySeedOptions>`, `IVacancyIngestionService` and `ILogger<…>`.
    - If disabled, return.
    - Otherwise `try { ingest } catch (Exception ex) when (ex is not OperationCanceledException) { log warning with UserId }`.

- [x] Task 4: Static adapter, catalogue and wiring (AC: 1, 6, 7)
  - [x] `backend/src/JobNecto.Infrastructure.JobSources/SeedData/demo-vacancies.json` (CREATE): 40 postings meeting every AC6 rule.
    - Enum values as strings; `System.Text.Json` with `JsonStringEnumConverter` and case-insensitive matching.
    - Mark it `<EmbeddedResource Include="SeedData\demo-vacancies.json" />` in the `.csproj`.
    - Write realistic descriptions of 3–6 sentences: role, team, stack, responsibilities and requirements. Company names must be invented, so don't use real brands or Microsoft sample names like Contoso or Northwind.
    - `ExperienceLevel` comes from a small fixed set: `Junior`, `Middle`, `Senior`, `Lead`.
  - [x] `backend/src/JobNecto.Infrastructure.JobSources/Static/StaticJobSource.cs` (CREATE): `IJobSource` with `Name = "Jobnecto Demo Catalogue"`.
    - Load and deserialize the resource **once**, using a `Lazy<>` and a singleton lifetime.
    - Map to `JobPosting`, setting `SourceName` and `SourceUrl`.
    - A missing or invalid resource throws `InvalidOperationException` with a clear message.
  - [x] `backend/src/JobNecto.Infrastructure.JobSources/Seeding/VacancySeedBackfillService.cs` (CREATE): an `IHostedService`.
    - `StartAsync`: if disabled, return. Otherwise create a scope, call `GetAllIdsAsync`, then `IngestForUserAsync` for each user in sequence.
    - Log one info line with the user count and rows added. Catch per user, so one failure doesn't stop the rest, and wrap the whole run so startup never fails.
    - `StopAsync`: a no-op.
  - [x] `backend/src/JobNecto.Infrastructure.JobSources/DependencyInjection.cs` (CREATE): `AddJobSources(this IServiceCollection, IConfiguration)`, which:
    - calls `Configure<VacancySeedOptions>(configuration.GetSection(VacancySeedOptions.SectionName))`
    - registers `AddSingleton<IJobSource, StaticJobSource>()`
    - registers `AddHostedService<VacancySeedBackfillService>()`
    - Namespace: `JobNecto.Infrastructure.JobSources`.
  - [x] `backend/src/JobNecto.API/Program.cs` (UPDATE): `builder.Services.AddJobSources(builder.Configuration);` after `AddInfrastructure`.
  - [x] `backend/src/JobNecto.API/appsettings.json` (UPDATE) `"Seed": { "Vacancies": { "Enabled": false } }`, and `appsettings.Development.json` (UPDATE) `true`. Leave `appsettings.Test.json` without the key.

- [x] Task 5: Tests (AC: all)
  - [x] `backend/tests/JobNecto.Tests/Infrastructure/JobSources/StaticJobSourceTests.cs` (CREATE):
    - Returns exactly 40 postings.
    - Every AC6 rule is checked in its own test: enum validity, `min ≤ max`, unique `.test` URLs, no email or phone regex matches (`@`, `\+?\d[\d\s-]{7,}`), and the coverage thresholds (work-location types, work-time types, locations, currencies, null-salary count, skills overlap).
    - Calling twice returns the same data (cached).
  - [x] `backend/tests/JobNecto.Tests/Application/JobSources/VacancyIngestionServiceTests.cs` (CREATE), with Moq'd `IUnitOfWork`/repositories, a fake `IJobSource` and a fake `TimeProvider`, in the style of `FilterVacanciesQueryHandlerTests`:
    - A new user gets all postings added, with the right `UserId` and `JobSource`, staggered timestamps, and `SaveChangesAsync` called once.
    - Postings whose URL already exists are skipped, and the return value counts only new ones.
    - When everything exists, nothing is added (assert `SaveChangesAsync` is not called, or is a harmless no-op; pick one and pin it).
    - The source throwing propagates. The *callers* isolate failures, not the service.
  - [x] `backend/tests/JobNecto.Tests/Application/JobSources/SeedVacanciesOnUserRegisteredHandlerTests.cs` (CREATE):
    - Disabled → the ingestion service is never called.
    - Enabled → called with the notification's `UserId`.
    - Ingestion throws → no exception escapes and a warning is logged.
    - `OperationCanceledException` is **not** swallowed.
  - [x] `backend/tests/JobNecto.Tests/Application/Users/CreateUserCommandHandlerTests.cs` (UPDATE): publishes `UserRegisteredNotification` with the new id exactly once, **after** save (`MockSequence` or callback ordering), and does not publish on 409 or validation failure.
  - [x] `backend/tests/JobNecto.Tests/Infrastructure/JobSources/VacancySeedBackfillServiceTests.cs` (CREATE):
    - Disabled → no repository calls.
    - Enabled → ingests for each user id.
    - One user failing doesn't stop the others.
    - Total failure, e.g. `GetAllIdsAsync` throwing → `StartAsync` completes.
  - [x] Repository tests, following the existing `backend/tests/JobNecto.Tests/Infrastructure/VacancyRepositoryTests.cs` style:
    - `GetJobSourceUrlsAsync` returns URLs for that user only, and includes soft-deleted vacancies.
    - `GetAllIdsAsync` excludes soft-deleted users.
  - [x] `backend/tests/JobNecto.Tests/API/Vacancies/VacancySeedingApiTests.cs` (CREATE): a factory with seeding **enabled** (see Trap 3).
    - Register → filter browse gives `totalCount 40`, 20 items and `hasNext`; the second page gives 20 and `hasNext: false`.
    - `GET` of a returned id → 200; another user's `GET` of that id → 404.
    - A second user also gets 40 of their own (different ids).
    - Plus one test with the **default** factory: register → `totalCount 0` (AC4).
  - [x] Add a DI smoke test that `IPublisher` resolves the `UserRegisteredNotification` handler from the real container. The API test above covers this implicitly; if so, note it.

- [x] Task 6: Docs hygiene
  - [x] `_bmad-output/planning-artifacts/architecture/demo-mvp-architecture-decisions.md` (UPDATE), under §3.7 "What stays unchanged": add one paragraph naming the seam (`IJobSource`/`JobPosting`/`IVacancyIngestionService`), the per-user ingestion model, the `Seed:Vacancies:Enabled` flag and its two triggers, and "swap = one DI registration".
  - [x] `README.md` (UPDATE): one line under local development saying that in Development, every user gets the 40-vacancy demo catalogue on sign-up and at startup.

- [x] Task 7: Verification and test coverage (AC: all)
  - [x] `dotnet test backend/JobNecto.slnx`: all pass.
  - [x] `dotnet build backend/JobNecto.slnx --configuration Release --warnaserror`: 0 warnings.
  - [x] Backend coverage gate: `dotnet test backend/JobNecto.slnx --collect:"XPlat Code Coverage" --settings backend/coverlet.runsettings --results-directory ./coverage/backend`, then `python scripts/check_coverage.py ./coverage/backend --threshold 80`. Use `python`, not `python3`. The new `JobSources` project must meet ≥80% per file. If `JobNecto.Infrastructure.JobSources` isn't picked up by `coverlet.runsettings`, find out why before excluding anything. The JSON resource isn't code.
  - [x] Live check against a **throwaway** Postgres, never the developer's database; see Dev Notes → *Running the API safely*:
    - Start in Development with the flag on, register two users through the API, and confirm 40 vacancies each, paging and cross-user 404.
    - Restart the API: the backfill adds 0 (idempotent).
    - Register a user with the flag off: 0 vacancies.
    - Record the results in Completion Notes.
  - [x] Frontend untouched: `git diff --stat` shows no `frontend/` changes, so the frontend gates aren't needed. Say so explicitly.

- [x] Task 8: PR Review (RV) recorded before merge (AGENTS.md completion checklist item 5)
  - [x] `jobnecto-qa` (or `bmad-code-review`) runs the adversarial review on the PR and fills in `## PR Review (RV)` below. Nothing merges before that's recorded; a self-review doesn't count.

## Dev Notes

### Technical Requirements

- **Clean Architecture:** Application holds `IJobSource`, `JobPosting`, the ingestion service, the notification and its handler, and the options class. Infrastructure.JobSources holds the adapter, resource, hosted service and DI. Infrastructure holds only the two repository methods.
  - No EF types in Application (agent-learning 2026-05-10 "Keep EF Core types out of Application handlers").
  - No `Vacancy` construction inside `JobNecto.Infrastructure.JobSources`; the adapter emits `JobPosting` only.
- **Namespaces match folders** (AGENTS.md), e.g. `JobNecto.Infrastructure.JobSources.Static`, `JobNecto.Infrastructure.JobSources.Seeding`, `JobNecto.Application.JobSources`.
- XML-doc every public type and every non-trivial method (AGENTS.md, `function-comments` skill).
- Don't add Quartz, Redis or `HttpClient` usage. `Microsoft.Extensions.Http` is already referenced by the JobSources project; leave it, unused.
  - *Note (2026-10-31, RV #3):* "unused" was not quite true. `Microsoft.Extensions.Http` transitively brought in `Microsoft.Extensions.Options.ConfigurationExtensions`, which `AddJobSources` needs for `Configure<T>(IConfiguration)`. That package is now referenced explicitly, so the Http reference really is unused and could be removed without breaking the build.
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

Claude Opus 5.5 (`claude-opus-5-5`), `jobnecto-dev` skill, story mode. Review baseline: `8d0bbcb` (the story-creation commit on top of `50c33bc`).

### Debug Log References

- **Baseline (Task 0)** at `8d0bbcb`: `dotnet test backend/JobNecto.slnx` gave `Passed! - Failed: 0, Passed: 670, Skipped: 0, Total: 670`.
- **Test-first red:** with only `IJobSource`/`JobPosting` in place, `VacancySeedingApiTests` gave `Failed: 4, Passed: 2`, each failing with `Expected firstPage.TotalCount to be 40, but found 0`. The 2 passing tests are the AC4 default-config test and the failing-source test, both expected to pass at that point.
- **Mutation check (failure isolation):** narrowing the handler's catch filter to `NotSupportedException` turned `Register_WhenJobSourceThrows_StillReturns201WithCookie_AndSeedsNothing` red (`Expected response.StatusCode to be HttpStatusCode.Created {value: 201}, but found HttpStatusCode.InternalServerError {value: 500}`). After the restore, the file was byte-identical to the backup.
- **Final Debug suite:** `dotnet test backend/JobNecto.slnx` gave `Passed! - Failed: 0, Passed: 739, Skipped: 0, Total: 739` (+69 over baseline).
- **Release build:** `dotnet build backend/JobNecto.slnx --configuration Release --warnaserror --no-incremental`:
  ```
  Build succeeded.
      0 Warning(s)
      0 Error(s)
  ```
- **Release tests:** `dotnet test backend/JobNecto.slnx --configuration Release --no-build` gave `Passed! - Failed: 0, Passed: 739, Skipped: 0, Total: 739`.
- **Backend coverage gate:** `dotnet test ... --collect:"XPlat Code Coverage" --settings backend/coverlet.runsettings --results-directory ./coverage/backend`, then `python scripts/check_coverage.py ./coverage/backend --threshold 80`:
  ```
  Coverage: 1980/2052 lines = 96.5% across 102 files (threshold 80%)
  PASS: all files meet the threshold.
  ```
  - `JobNecto.Infrastructure.JobSources` is picked up by `coverlet.runsettings` (`[JobNecto.*]*`), so no exclusion was needed. Per file: `Static/StaticJobSource.cs` 50/50, `Seeding/VacancySeedBackfillService.cs` 7/7, `DependencyInjection.cs` 6/6, all 100%. `VacancyIngestionService.cs` 26/26, `SeedVacanciesOnUserRegisteredHandler.cs` 5/5, `CreateUserCommandHandler.cs` 5/5, `ApplicationCollectionExtensions.cs` 11/11, `UserRepository.cs` 1/1, `Program.cs` 27/27, `VacancyRepository.cs` 115/128 (89.8%, the misses are pre-existing `ApplyFilters` branches).
  - **Gate blind spot (pre-existing, deferred):** `ExcludeByAttribute` includes `CompilerGeneratedAttribute`, so `async` method bodies (state machines) aren't measured; that's why the line counts above are small. A trial run with a scratch copy of the runsettings without that attribute put every Story 3.1 file at 100%: `StaticJobSource` 50/50, `VacancySeedBackfillService` 44/44, `VacancyIngestionService` 48/48, `SeedVacanciesOnUserRegisteredHandler` 20/20, `CreateUserCommandHandler` 31/31. Five pre-existing files would fall below 80%; see `deferred-work.md`.
- **No pending migration:** `dotnet ef migrations has-pending-model-changes --project backend/src/JobNecto.Infrastructure --startup-project backend/src/JobNecto.API` gave `No changes have been made to the model since the last migration.`
- **Contract unchanged:** `git diff --name-only 8d0bbcb` lists nothing under `backend/src/JobNecto.API/Controllers`, `backend/src/JobNecto.Application/Vacancies` or `backend/src/JobNecto.Infrastructure/Migrations`. `GetFilteredAsync` is untouched, so `/openapi/v1.json` can't have changed, and `OpenApiSchemaTests` pass.
- **Frontend:** `git diff --name-only 8d0bbcb -- frontend` is empty.
- **RV patch round (2026-10-31, on top of `3c03a05`):** applied RV #1, #3, #7a and #7b; the rest is logged in `deferred-work.md`.
  - `dotnet test backend/JobNecto.slnx`: `Passed! - Failed: 0, Passed: 746, Skipped: 0, Total: 746` (739 + 7 new pin tests).
  - `dotnet build backend/JobNecto.slnx --configuration Release --warnaserror --no-incremental`: `Build succeeded.` `0 Warning(s)` `0 Error(s)`. Release tests: `Passed: 746, Failed: 0`.
  - Coverage gate, `python scripts/check_coverage.py ./coverage/backend --threshold 80`: `Coverage: 1980/2052 lines = 96.5% across 102 files (threshold 80%)` and `PASS: all files meet the threshold.` `./coverage` was deleted afterwards.
  - **Mutation check:** removing `DisableVacancySeeding()` from `JobNectoApiFactory` turned `DefaultFactory_KeepsSeedingOff_EvenWhenEnabledEarlierInTheConfigChain` red (`Expected ... Enabled to be False, but found True`). The line was restored, and `git diff` shows only the one added line.
  - **Postgres factories:** `UsersControllerConcurrencyFactory`, `CoverLettersPostgresFactory` and `CoverLetterTemplatesPostgresFactory` all subclass `WebApplicationFactory<Program>`, build a real Test-environment web host and register users over HTTP, so they publish `UserRegisteredNotification`. All three are now pinned off. While seeding is off, building their host touches no database, so their pin tests run without Postgres.

### Completion Notes List

- **Seam (AC1, AC7).** `JobNecto.Application.JobSources` holds `IJobSource`, `JobPosting`, `IVacancyIngestionService` / `VacancyIngestionService`, `VacancySeedOptions` and `SeedVacanciesOnUserRegisteredHandler`; there are no EF types in Application. `JobNecto.Infrastructure.JobSources` holds `StaticJobSource`, the embedded `SeedData/demo-vacancies.json` (explicit `LogicalName`), `VacancySeedBackfillService` and `AddJobSources`. Swapping in a real source = replacing `AddSingleton<IJobSource, StaticJobSource>()`. No read-path file changed.
- **Ingestion.** Ingestion fetches the postings, then loads the user's URLs with `GetJobSourceUrlsAsync`, which projects the JSON `JobSource` column with `IgnoreQueryFilters` and reads `Url` in memory. It skips known URLs, and a URL the source lists twice is added once. Timestamps come from the injected `TimeProvider` (`CreatedAt = UpdatedAt = now - n × 6h`), and only new postings take a stagger slot. It saves once. **Pinned choice:** when nothing is new, `SaveChangesAsync` is *not* called.
- **Triggers (AC2–AC5).** `CreateUserCommandHandler` publishes `UserRegisteredNotification` after `SaveChangesAsync`. The handler and the backfill both check `Seed:Vacancies:Enabled` and catch every failure except a cancellation the caller requested. The backfill runs one scope per user, catches failures per user and for the whole run, and logs one summary line.
- **Catalogue (AC6).** 40 synthetic postings across 18 invented companies, with websites on `*.example.test`. Coverage: all 3 work-location types, 5 work-time types, 14 locations, 7 currencies (PLN, EUR, CHF, USD, CAD, UAH, GBP), 5 rows without salary (currency also null there), experience levels Junior/Middle/Senior/Lead, and all 9 résumé-seed skills. Descriptions have no emails, no phone-like digit runs and no person names; they mention only cities and tech names. The acceptance lens re-checked them by script.
- **Live check:** throwaway `postgres:16-alpine` on port 55434, migrations applied with `dotnet ef database update --connection`, Debug build copied to a temp folder (no `appsettings.Local.json` was present), run as `ASPNETCORE_ENVIRONMENT=Development` on `http://localhost:5000` with throwaway secrets. **14/14 checks passed**:
  - **Run 1 (flag on), AC2:**
    - Users A and B registered: `201` with the auth cookie.
    - A's page 1: `totalCount 40`, 20 items, `hasNext true`. Page 2: 20 items, `hasNext false`. 40 unique ids.
    - `createdAt` is newest-first and distinct (`2026-10-27T02:47Z` … `2026-10-17T08:47Z`).
    - The owner's `GET` returns `200` for all 40 ids, with job source `Jobnecto Demo Catalogue` / `https://jobs.example.test/…`. B's `GET` of A's id returns `404`.
    - B has their own 40, disjoint from A's. The DB holds 40 rows per user.
    - Backfill log: `checked 0 users: 0 vacancies added`, because it ran before anyone registered.
  - **Run 2 (flag off), AC4:** user C registered `201` with `totalCount 0`. No backfill log line.
  - **Run 3 (flag on), AC3:** `Demo vacancy backfill checked 3 users: 40 vacancies added, 0 users failed.` C now has 40, and A still has 40 (no duplicates).
  - **Run 4 (flag on), AC3 idempotency:** `checked 3 users: 0 vacancies added, 0 users failed.` 120 rows in total.
  - **Observed on Postgres:** A's rows have 40 distinct `CreatedAt` values but **1** distinct `UpdatedAt`. This is the pre-existing `ValueGeneratedOnAddOrUpdate` mapping; it is deferred (Review Findings). The default `createdAt` sort is unaffected.
  - **Cleanup:** API process stopped, container removed (`docker rm -f` exit 0, none left), temp API copy, secrets file and run logs deleted, port 5000 free. No secret or connection string was written to any repo file.
- **AC5** is covered at the HTTP level (`Register_WhenJobSourceThrows_…` → `201` + cookie, with the mutation check above) and at the unit level (ingestion and save failures, timeouts). The warning carries the user id and the exception; nothing logs emails or connection strings.
- **Validation failure → no publish (Task 5):** FluentValidation runs in `ValidationBehavior` *before* the handler, so the handler (and `Publish`) never runs on a 400. The handler-level tests cover the 409 paths for email, login and phone.
- **Frontend untouched:** no file under `frontend/` changed, so the frontend gates (`ng test`) weren't needed.
- **Deviations from the story text (both reviewed):**
  1. The catch filters are `when (ex is not OperationCanceledException || !ct.IsCancellationRequested)` instead of `when (ex is not OperationCanceledException)`. The intent ("don't swallow cancellation") is kept, but a source's own timeout (`TaskCanceledException` with the caller's token not cancelled) is now treated as an ordinary failure, so it can't fail sign-up or startup.
  2. Two NuGet references were added: `Microsoft.Extensions.Options` 10.0.3 to Application (for `IOptions<>` in the handler) and `Microsoft.Extensions.Hosting.Abstractions` 10.0.3 to JobSources (for `IHostedService`). Both are abstractions-only, and neither is Quartz, Redis or `HttpClient`.
- **Test-infra change outside the story's file list:** `CorsFactory` and `OpenApiSchemaFactory` run in `Development`, so seeding was on there. The backfill would then write to whatever database the final configuration names; `appsettings.Local.json` loads last and overrides their dummy connection string. Both now force `Seed:Vacancies:Enabled=false` through `ConfigureAppConfiguration`, which applies after Program.cs's sources. Two tests pin this.

### Review Findings

Self-review: four parallel lenses (adversarial, edge-case hunter, verification gap, acceptance auditor) on `git diff 8d0bbcb` (staged). All four completed and returned reports. Each finding below was verified at the cited code before triage. **This is not the RV** (AGENTS.md item 5).

- [x] [Review][Patch] Development-environment test hosts (`CorsFactory`, `OpenApiSchemaFactory`) ran the startup backfill against whatever DB the final config named. Run from the main checkout, `appsettings.Local.json` would point it at the developer's database and seed every user. **Fixed:** seeding forced off through `ConfigureAppConfiguration`; pinned by `CorsFactory_DevelopmentHost_KeepsSeedingOff` and `OpenApiSchemaFactory_DevelopmentHost_KeepsSeedingOff`. `backend/tests/JobNecto.Tests/API/CorsTests.cs`, `OpenApiSchemaTests.cs`.
- [x] [Review][Patch] The cancellation filters keyed on exception type, so an unrequested `TaskCanceledException` (e.g. an HttpClient timeout in a future real `IJobSource`) would 500 sign-up after commit and fail host startup. **Fixed:** propagate only when the caller's token is cancelled. New tests: `Handle_SourceTimesOutWithoutCallerCancellation_IsTreatedAsFailure`, `StartAsync_SourceTimesOutWithoutStartupAbort_CountsUserAsFailed_AndCompletes`, `StartAsync_ListingUsersTimesOutWithoutStartupAbort_CompletesAndLogsWarning`. The cancellation tests now use a really cancelled token. `SeedVacanciesOnUserRegisteredHandler.cs`, `VacancySeedBackfillService.cs`.
- [x] [Review][Patch] AC3 was proven only with mocks. **Added** end-to-end tests through the real host, repository and UoW: `Backfill_SeedsExistingUserWithoutCatalogue_AndIsIdempotentOnRerun` (40, then still 40) and `Backfill_AfterRegistration_AddsNothing_AndNeverResurrectsADeletedVacancy` (39 visible, 40 including deleted).
- [x] [Review][Patch] AC2 checked `GET 200` on one id only, and the `createdAt` order assertion would pass with all-equal timestamps. **Added** `EverySeededVacancy_IsReadableByItsOwner_WithDistinctCreatedAt`.
- [x] [Review][Patch] `StaticJobSource.ToPosting` was only partly pinned (dropping `SalaryMax`/`CompanyWebsite` would go unnoticed), and the integer-enum ban was untested. **Added** `Catalogue_FirstRow_MapsEveryField` and `FetchAsync_IntegerEnumValue_ThrowsInvalidOperation`.
- [ ] [Review][Defer] On Postgres the staggered `UpdatedAt` is ignored on insert (`ValueGeneratedOnAddOrUpdate` → `BeforeSaveBehavior=Ignore`); confirmed live: 1 distinct `UpdatedAt` across 40 rows. The default `createdAt` sort is correct; `sortBy: updatedAt` falls back to the `Id` tie-break. The root cause is pre-existing model config shared by all entities, and the fix needs a migration. Logged in `deferred-work.md`.
- [ ] [Review][Defer] Check-then-insert de-duplication with no DB uniqueness: two instances backfilling at once could double-seed a user. It can't happen on a single instance (hosted services finish before Kestrel listens). Needs `JobSource.Url` as an indexed column or a lock. Logged.
- [ ] [Review][Defer] A client aborting sign-up after the user commits leaves the user unseeded until the next startup backfill (a retry gets 409). The story specified passing the request token through; kept as specified, with a recommendation logged.
- [ ] [Review][Defer] The startup backfill blocks Kestrel while it runs (sequential, per user). Fine at demo scale; move to `BackgroundService`/`ApplicationStarted` later. Logged.
- [ ] [Review][Defer] The registration-path ingestion shares the request's DbContext, so after a failed save the added rows stay tracked. Not reachable today (nothing saves after `Publish`). Logged.
- [ ] [Review][Defer] The coverage gate doesn't measure `async` bodies (`CompilerGeneratedAttribute` exclusion; pre-existing, codebase-wide). Logged with trial-run numbers.
- [x] [Review][Dismiss] "The handler is constructed (with `IJobSource`) before the `Enabled` check, so a throwing source constructor breaks sign-up." `StaticJobSource`'s constructor only builds a `Lazy<>`. A throwing constructor is a composition error that the Development `ValidateOnBuild` and the DI smoke test (`Container_WiresSeedingHandler_DemoSource_AndBackfill`) surface at startup.
- [x] [Review][Dismiss] "`GetJobSourceUrlsAsync` dereferences `s.Url` on a null `JobSource`." The column is `IsRequired()` (NOT NULL), and the converter requires `Name`, so only hand-corrupted rows could trigger it.
- [x] [Review][Dismiss] "`Seed__Vacancies__Enabled=yes` fails startup." That is the binder's fail-fast on an invalid bool, a configuration error rather than a seeding failure.
- [x] [Review][Dismiss] "A user soft-deleted mid-backfill still gets seeded." Negligible window; the rows are owner-scoped and invisible to anyone else.
- [x] [Review][Dismiss] Untested minor properties: `GetAllIdsAsync` order, `StringComparer.Ordinal` URL matching, token pass-through checked with `It.IsAny`, and sequential vs parallel loop. Low value; the fixes would add assertions on implementation details only.
- [x] [Review][Dismiss] "`GetJobSourceUrlsAsync` / jsonb projection is never run on Npgsql in tests." The live check ran it on Postgres 16: run 3 de-duplicated A and B correctly and added 40 for C only.

### File List

- `backend/src/JobNecto.Application/JobSources/JobPosting.cs` (CREATED)
- `backend/src/JobNecto.Application/JobSources/IJobSource.cs` (CREATED)
- `backend/src/JobNecto.Application/JobSources/IVacancyIngestionService.cs` (CREATED)
- `backend/src/JobNecto.Application/JobSources/VacancyIngestionService.cs` (CREATED)
- `backend/src/JobNecto.Application/JobSources/VacancySeedOptions.cs` (CREATED)
- `backend/src/JobNecto.Application/JobSources/SeedVacanciesOnUserRegisteredHandler.cs` (CREATED)
- `backend/src/JobNecto.Application/Users/UserRegisteredNotification.cs` (CREATED)
- `backend/src/JobNecto.Application/Users/CreateUserCommandHandler.cs` (UPDATED — inject `IPublisher`; publish `UserRegisteredNotification` after save)
- `backend/src/JobNecto.Application/ApplicationCollectionExtensions.cs` (UPDATED — register `TimeProvider.System` and `IVacancyIngestionService`)
- `backend/src/JobNecto.Application/JobNecto.Application.csproj` (UPDATED — `Microsoft.Extensions.Options` 10.0.3)
- `backend/src/JobNecto.Application/Interfaces/IVacancyRepository.cs` (UPDATED — `GetJobSourceUrlsAsync`)
- `backend/src/JobNecto.Application/Interfaces/IUserRepository.cs` (UPDATED — `GetAllIdsAsync`)
- `backend/src/JobNecto.Infrastructure/Repositories/VacancyRepository.cs` (UPDATED — `GetJobSourceUrlsAsync`, in-memory URL projection incl. soft-deleted)
- `backend/src/JobNecto.Infrastructure/Repositories/UserRepository.cs` (UPDATED — `GetAllIdsAsync`)
- `backend/src/JobNecto.Infrastructure.JobSources/JobNecto.Infrastructure.JobSources.csproj` (UPDATED — `EmbeddedResource` with `LogicalName`, `Microsoft.Extensions.Hosting.Abstractions`, `InternalsVisibleTo JobNecto.Tests`)
- `backend/src/JobNecto.Infrastructure.JobSources/SeedData/demo-vacancies.json` (CREATED)
- `backend/src/JobNecto.Infrastructure.JobSources/Static/StaticJobSource.cs` (CREATED)
- `backend/src/JobNecto.Infrastructure.JobSources/Seeding/VacancySeedBackfillService.cs` (CREATED)
- `backend/src/JobNecto.Infrastructure.JobSources/DependencyInjection.cs` (CREATED)
- `backend/src/JobNecto.API/Program.cs` (UPDATED — `AddJobSources` after `AddInfrastructure`)
- `backend/src/JobNecto.API/appsettings.json` (UPDATED — `Seed:Vacancies:Enabled=false`)
- `backend/src/JobNecto.API/appsettings.Development.json` (UPDATED — `Seed:Vacancies:Enabled=true`)
- `backend/tests/JobNecto.Tests/API/Vacancies/VacancySeedingApiTests.cs` (CREATED)
- `backend/tests/JobNecto.Tests/Application/JobSources/VacancyIngestionServiceTests.cs` (CREATED)
- `backend/tests/JobNecto.Tests/Application/JobSources/SeedVacanciesOnUserRegisteredHandlerTests.cs` (CREATED)
- `backend/tests/JobNecto.Tests/Infrastructure/JobSources/StaticJobSourceTests.cs` (CREATED)
- `backend/tests/JobNecto.Tests/Infrastructure/JobSources/VacancySeedBackfillServiceTests.cs` (CREATED)
- `backend/tests/JobNecto.Tests/TestDoubles/RecordingLogger.cs` (CREATED)
- `backend/tests/JobNecto.Tests/TestDoubles/FixedTimeProvider.cs` (CREATED)
- `backend/tests/JobNecto.Tests/Application/Users/CreateUserCommandHandlerTests.cs` (UPDATED — `IPublisher` mock; publish-after-save and no-publish-on-409 tests)
- `backend/tests/JobNecto.Tests/Infrastructure/VacancyRepositoryTests.cs` (UPDATED — `GetJobSourceUrlsAsync` tests)
- `backend/tests/JobNecto.Tests/Infrastructure/UserRepositorytests.cs` (UPDATED — `GetAllIdsAsync` tests)
- `backend/tests/JobNecto.Tests/API/CorsTests.cs` (UPDATED — force seeding off in the Development test host; review patch)
- `backend/tests/JobNecto.Tests/API/OpenApiSchemaTests.cs` (UPDATED — force seeding off in the Development test host; review patch)
- `backend/tests/JobNecto.Tests/API/TestHostConfigurationExtensions.cs` (CREATED — RV #1: shared `DisableVacancySeeding()` / `SetVacancySeeding()` app-configuration pin)
- `backend/tests/JobNecto.Tests/API/JobNectoApiFactory.cs` (UPDATED — RV #1: pin seeding off; seeding-enabled subclasses override it)
- `backend/tests/JobNecto.Tests/API/ExceptionHandlingTests.cs` (UPDATED — RV #1: pin seeding off in the Production host)
- `backend/tests/JobNecto.Tests/API/AuthenticationTests.cs` (UPDATED — RV #1: pin seeding off after the source-clearing callback)
- `backend/tests/JobNecto.Tests/API/UsersControllerConcurrencyTests.cs`, `CoverLetters/CoverLettersUniquenessApiTests.cs`, `CoverLetterTemplates/CoverLetterTemplatesUniquenessApiTests.cs` (UPDATED — RV #1: pin seeding off in the Postgres-backed hosts)
- `backend/src/JobNecto.Infrastructure.JobSources/JobNecto.Infrastructure.JobSources.csproj` (UPDATED again — RV #3: explicit `Microsoft.Extensions.Options.ConfigurationExtensions` 10.0.3)
- `backend/tests/JobNecto.Tests/API/Vacancies/VacancySeedingApiTests.cs` (UPDATED again — RV #1 pin tests for all 7 non-seeding hosts, the early-config-chain test and the override test; RV #7a `HaveCount(40)`; RV #7b failing-source call count)
- `_bmad-output/planning-artifacts/architecture/demo-mvp-architecture-decisions.md` (UPDATED — §3.7 seam paragraph)
- `README.md` (UPDATED — Development demo-catalogue note)
- `_bmad-output/implementation-artifacts/deferred-work.md` (UPDATED — Story 3.1 deferred review items)
- `_bmad-output/implementation-artifacts/sprint-status.yaml` (UPDATED — 3-1 → review)
- `_bmad-output/implementation-artifacts/3-1-seed-mock-vacancies-behind-the-jobsource-seam.md` (UPDATED — status, tasks, Dev Agent Record)

## PR Review (RV)

<!-- Filled by jobnecto-qa / bmad-code-review before merge (AGENTS.md completion checklist item 5). Not a self-review. -->

**Reviewer:** `jobnecto-qa` (RV), an independent agent that did not write the PR. Reviewed 2026-10-31.
**Target:** PR #107, `story/3-1-seed-mock-vacancies` at `c5debb0` (on top of `8d0bbcb`), diffed against master `50c33bc`. `git fetch` found no newer commits.

**Verdict: APPROVE WITH FOLLOW-UPS.** No critical, high or medium finding. All 7 ACs are met and verified against the code and tests. Traps 1–6 are handled. The Decision Record is implemented as written. The two declared deviations (the cancellation filter and the two added packages) are sound. The 7 low findings below are all non-blocking `[Defer]` items.

**Lenses run:** all five ran as parallel subagents and returned reports: correctness, regression, architecture & quality, edge cases & optimization, test coverage. I re-verified every finding at the cited code before triage; the severities below are mine.

### Gates (run by the reviewer)

| Gate | Command | Result |
|---|---|---|
| Debug tests | `dotnet test backend/JobNecto.slnx` | `Passed! - Failed: 0, Passed: 739, Skipped: 0, Total: 739` |
| Release build | `dotnet build backend/JobNecto.slnx --configuration Release --warnaserror` | `Build succeeded. 0 Warning(s) 0 Error(s)` |
| Release tests | `dotnet test backend/JobNecto.slnx --configuration Release --no-build` | `Passed! - Failed: 0, Passed: 739, Skipped: 0, Total: 739` |
| Backend coverage | `dotnet test … --collect:"XPlat Code Coverage" --settings backend/coverlet.runsettings --results-directory ./coverage/backend` then `python scripts/check_coverage.py ./coverage/backend --threshold 80` | `Coverage: 1980/2052 lines = 96.5% across 102 files (threshold 80%)` / `PASS: all files meet the threshold.` `./coverage` deleted afterwards. |
| Migrations | `dotnet ef migrations has-pending-model-changes --project backend/src/JobNecto.Infrastructure --startup-project backend/src/JobNecto.API` | `No changes have been made to the model since the last migration.` No DB needed. The `JobCategories`/`Skills` value-comparer warnings already existed. |
| Frontend | `git diff 50c33bc --stat -- frontend` | Empty, so the frontend gates were skipped (not needed). |
| Secrets | grep of the added lines for `Password=`, `Host=`, `Secret`, `ApiKey`, `Bearer` | No hits in code or config. The only matches are placeholders in story prose (`JwtSettings__SecretKey=<random>`). The `Password=test` dummy connection strings in `CorsTests`/`OpenApiSchemaTests` already existed and are not part of this diff. |

Not run: no live API or DB check (instructed not to start containers or servers). I reviewed the dev's recorded live check (14/14) but did not reproduce it.

### Focus-area verification

- **Clean Architecture holds.**
  - `JobNecto.Application` gains no EF types. Its only new package is `Microsoft.Extensions.Options` 10.0.3, needed for `IOptions<>` in the handler.
  - `JobNecto.Infrastructure.JobSources` references only Application. `StaticJobSource` emits only `JobPosting`; `Vacancy` is built only in `VacancyIngestionService.ToVacancy`.
  - `Microsoft.Extensions.Hosting.Abstractions` 10.0.3 is justified, for `IHostedService`.
  - Namespaces match folders.
- **Seeding cannot run unintentionally.**
  - `appsettings.json` sets `false`, `appsettings.Development.json` sets `true`, and `appsettings.Test.json` has no key.
  - The Development test hosts `CorsFactory` and `OpenApiSchemaFactory` force the flag off through `ConfigureAppConfiguration`. That applies after Program.cs's sources, including `appsettings.Local.json`, and two guard tests pin it.
  - `AuthenticationTestFactory` (also Development) is off only because it clears every config source.
  - The claim "`appsettings.Local.json` can't flip it in tests" holds **only for those Development hosts**; see finding 1.
- **MediatR notification.**
  - `Publish` runs after `SaveChangesAsync` (`CreateUserCommandHandler.cs:71`). It never runs on 409, and never on 400 because `ValidationBehavior` short-circuits first.
  - The catch is inside the handler, as Trap 2 requires.
  - The filter `ex is not OperationCanceledException || !ct.IsCancellationRequested` is correct. Only a cancellation the caller asked for propagates. A source's own timeout is logged and swallowed. Unit tests exercise both branches.
- **Hosted backfill.**
  - Disabled returns before resolving anything.
  - It uses a fresh async scope to list users and a fresh scope per user, so a failed save cannot leak tracked entities into the next user.
  - Failures are caught per user and for the whole run, with the same cancellation filter. It cannot crash startup unless startup itself is aborted.
  - The deferred "blocks Kestrel" item is acceptable for a demo: 40 rows per user, one `SaveChanges` per user. If Postgres is down, startup stalls about one connect timeout, logs, and continues.
- **Idempotency.**
  - Dedupe is per user, keyed on `JobSource.Url` with Ordinal comparison.
  - `GetJobSourceUrlsAsync` uses `IgnoreQueryFilters()`; the only `Vacancy` filter is soft-delete (`AppDbContext.cs:52`), so deleted rows are never re-added.
  - URLs listed twice in the source are absorbed by `HashSet.Add`.
  - The concurrent-instance race (no unique index) is correctly declared and deferred. On a single instance it cannot happen, because hosted services finish before the server accepts requests.
- **AC7, read path unchanged.** The diff has no changes to controllers, `Application/Vacancies`, DTOs, migrations or the model snapshot, and `GetFilteredAsync` is untouched. The has-pending-model-changes check is clean.
- **AC6 catalogue.** I script-checked it:
  - 40 rows from 18 invented companies (e.g. Ashvelt Cloudworks, Brindlemoor Bank, Nimbalo Payments). No real brands, Contoso or Northwind.
  - All websites are on `*.example.test`. No `@`, phone-like digit runs or `http(s)://` in any description. No person names; the capitalised word pairs are company or tech names such as "Spring Boot" and "Entity Framework".
  - Variety: 3/3 work-location types, 5 work-time types, 14 locations, 7 currencies, 5 rows without salary. Every threshold is met.
- **Test quality.**
  - The HTTP tests really go through `POST /api/v1/users` (201 and the `auth-token` cookie are asserted), then `POST /vacancies/filter` and `GET /vacancies/{id}` with that cookie.
  - The seeding factory enables the flag explicitly through `AddInMemoryCollection`.
  - The idempotency tests run with seeding on, so they are not vacuous. The no-duplicate case includes a soft-deleted row.
  - `CreateUserCommandHandlerTests` asserts a real `save → publish` order.
  - Weak spots are in finding 7.
- **`UpdatedAt` stagger deferral and its impact on Story 3.2.** It is harmless for Story 3.2; see finding 2.

### Findings

| # | Severity | risk_score | Finding (evidence) | recommended_fix | Tag |
|---|---|---|---|---|---|
| 1 | low | 3 | **Seeding-off is pinned only in the two Development test hosts.** `Program.cs:10` loads `appsettings.Local.json` last, so it overrides `appsettings.json`/`.Test.json` and `UseSetting` values. These hosts do not pin `Seed:Vacancies:Enabled=false`: `JobNectoApiFactory` (Test, InMemory DB, so only test counts break), `ExceptionHandlingFactory` (Production; `ExceptionHandlingTests.cs:81-85`, where Local.json also overrides the dummy connection string, so the startup backfill would write to the developer's DB), and the three Postgres factories. `AuthenticationTestFactory` (Development) is off only because it clears all sources (`AuthenticationTests.cs:56-74`), not by intent. Precondition: a developer adds `Seed` to Local.json. Trap 3 forbids that, and there is no reason to, since Development already enables it. CI is unaffected. | Pin `["Seed:Vacancies:Enabled"]="false"` through `ConfigureAppConfiguration` in `JobNectoApiFactory` (subclasses that add later still win), `ExceptionHandlingFactory` and `AuthenticationTestFactory`. Alternatively, load `appsettings.Local.json` only when `IsDevelopment()`. | [Defer] |
| 2 | low | 3 | **The staggered `UpdatedAt` is ignored on Postgres** (`VacancyConfiguration.cs:92-94` `ValueGeneratedOnAddOrUpdate` → BeforeSave = Ignore), so all 40 seeded rows share one `UpdatedAt`. **Impact on Story 3.2: harmless.** Story 3.2's ACs browse with the default `createdAt desc` (FR24), and `CreatedAt` is staggered (40 distinct values in the dev's live check). For `sortBy=updatedAt|relevance` (`VacancyRepository.cs:36-46`, `relevance` → `updatedAt` at `:246`), the keyset `(UpdatedAt desc, Id desc)` with the strict `UpdatedAt == x && Id < id` tie-break neither skips nor repeats rows; only the visible order among seeded rows degrades to `Id` order. That would only show if 3.2/3.3 offers a "relevance"/"recently updated" sort toggle. Correction to the deferred note's "fix needs a migration" (unverified): `Property(v => v.UpdatedAt).Metadata.SetBeforeSaveBehavior(PropertySaveBehavior.Save)` is probably snapshot-neutral. | Keep it deferred. If Story 3.2/3.3 exposes the `updatedAt`/`relevance` sort, apply `SetBeforeSaveBehavior(Save)` and confirm with `has-pending-model-changes`, plus a Postgres-backed ordering test. | [Defer] |
| 3 | low | 2 | **A hidden transitive dependency.** `DependencyInjection.cs:24` uses `Configure<T>(IConfiguration)` from `Microsoft.Extensions.Options.ConfigurationExtensions`. That package reaches `JobNecto.Infrastructure.JobSources` only through the "unused" `Microsoft.Extensions.Http` → `Microsoft.Extensions.Diagnostics` (`obj/project.assets.json`). `Hosting.Abstractions` does not bring it. Removing the "unused" Http reference, as Dev Notes invite, would break the build. | Add `<PackageReference Include="Microsoft.Extensions.Options.ConfigurationExtensions" Version="10.0.3" />` to `JobNecto.Infrastructure.JobSources.csproj`. It is a one-line change and can be folded in now. | [Defer] |
| 4 | low | 2 | **Ingestion trusts the adapter output (future adapters only).** `VacancyIngestionService.cs:45` adds `posting.SourceUrl` unchecked, and `VacancyRepository.GetJobSourceUrlsAsync` drops null URLs (`.OfType<string>()`). So a null-URL posting is re-added on every run, and blank URLs collapse into one. `ToVacancy` (`:75-76`, `[.. posting.JobCategories]`) throws an NRE on null collections, which fails that user's whole ingestion. `StaticJobSource` cannot trigger either (`required` + `RespectNullableAnnotations` + `?? []`). | In the loop, skip and log `string.IsNullOrWhiteSpace(posting.SourceUrl)`. Use `?? []` for `JobCategories`/`Skills`. Add unit tests. Do it before the first real `IJobSource`. | [Defer] |
| 5 | low | 2 | **The backfill fetches the source once per user.** `IngestForUserAsync` calls `FetchAsync` (`VacancyIngestionService.cs:33`) inside the per-user loop (`VacancySeedBackfillService.cs` `IngestInOwnScopeAsync`). This is free with the `Lazy`-cached static source. With a real HTTP source it means N external calls per startup, and a broken source logs N identical stack traces. | Fetch once before the loop and pass the postings in (an overload `IngestForUserAsync(userId, postings, ct)`). If the fetch fails, log once and return. | [Defer] |
| 6 | low | 2 | **Composition coupling.** `AddApplication` registers `IVacancyIngestionService`, and MediatR auto-registers the handler, but `IJobSource` comes only from `AddJobSources`. MediatR resolves handlers before `Handle`, so a host that calls `AddApplication` without `AddJobSources` would fail `Publish` after the user commits, even with seeding disabled. Today there is a single composition root (`Program.cs`), covered by `Container_WiresSeedingHandler_DemoSource_AndBackfill` and Development `ValidateOnBuild`. | Document in `AddApplication`'s XML doc that `AddJobSources` is required, or add a null-object `TryAddSingleton<IJobSource, EmptyJobSource>()`. | [Defer] |
| 7 | low | 2 | **Test-quality gaps (no vacuous blocker).** (a) `EverySeededVacancy_IsReadableByItsOwner_WithDistinctCreatedAt` (`VacancySeedingApiTests.cs:142-156`) would pass on an empty page; other tests assert 40, so this is a self-containment gap. (b) `Register_WhenJobSourceThrows_…` (`:130-139`) does not prove the failing source was called. It still fails on a missing catch (the dev's mutation check: 500), so it is weak, not vacuous. (c) AC5's "DB unavailable mid-ingest" registration path has no HTTP test; ingestion shares the request `DbContext` (already deferred). (d) The end-to-end backfill covers one user and no soft-deleted user; unit and repository tests cover those parts. | (a) Add `page.Items.Should().HaveCount(40)` before the loop. (b) Count calls in `FailingJobSource` and assert 1. (c) Add a `SaveChangesInterceptor` that throws on added `Vacancy` entries, then assert 201 + cookie, `GET /users/me` 200 and 0 vacancies. (d) A three-user backfill test: no catalogue → 40, soft-deleted → 0, already seeded → 40. | [Defer] |

**Dismissed:**
- `IgnoreQueryFilters()` also bypasses future non-soft-delete filters. Only one filter exists today. Revisit with EF 10 named filters if a second one is added.
- `GetAllIdsAsync(CancellationToken ct)` lacks `= default`. Requiring the token is arguably better.
- `IJobSource.Name` is unused. It is harmless contract surface.
- Mixed `Microsoft.*` 10.0.0/10.0.3 patch versions. This already existed, and NuGet resolves to the highest.
- A client abort during seeding leaves the user unseeded until the next backfill. This is already deferred, and the story specified passing the request token.

**Re-verified existing deferrals as accurate:** the check-then-insert race across instances, the backfill blocking Kestrel, the shared request `DbContext`, request-token cancellation, and the coverage gate's `async`-body blind spot.

**Must fix before merge:** nothing. **Still to add to `deferred-work.md`** (this RV did not edit it): findings 1, 3, 4, 5, 6 and 7, plus the finding-2 note correcting "needs a migration" and the Story 3.2/3.3 trigger.

## Change Log

- 2026-10-27: Story created (ready-for-dev). User decision: ingest on registration + startup backfill, gated by `Seed:Vacancies:Enabled`.
- 2026-10-27: Implemented (`jobnecto-dev`). Built the JobSource seam, per-user ingestion, registration notification, startup backfill and the 40-posting synthetic catalogue. The self-review applied 5 patches (Development test hosts force seeding off; token-based cancellation filters; end-to-end backfill/idempotency, all-ids and mapping tests) and deferred 6 items to `deferred-work.md`. Tests 670 → 739; Release build 0 warnings; coverage gate PASS (96.5%); live check 14/14 on a throwaway Postgres. Status → review; awaiting the independent PR Review (RV).
- 2026-10-31: RV patch round (`jobnecto-dev`), applied after the RV "approve with follow-ups". Test-only changes, plus one build-only package reference:
  - RV #1: seeding is pinned off in every non-seeding test host (`JobNectoApiFactory`, `ExceptionHandlingFactory`, `AuthenticationTestFactory`, the three Postgres factories, and Cors/OpenApi now through the shared helper), with a pin test per host, an early-config-chain test and an override test.
  - RV #3: explicit `Microsoft.Extensions.Options.ConfigurationExtensions` reference.
  - RV #7a/#7b: tighter seeding API tests.
  - RV #2 (amended), #4, #5, #6, #7c and #7d are logged in `deferred-work.md`.
  - Tests 739 → 746; Release build 0 warnings; coverage gate PASS (96.5%). Status unchanged (review).
