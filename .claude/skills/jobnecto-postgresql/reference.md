# Tables and relationships

| Table | Notes |
|-------|--------|
| `Users` | Root entity; check constraints on `Email`, `Phone`, `Age` |
| `Resumes` | FK `UserId` → `Users` |
| `Educations` | FK `UserId` → `Users`; M:N with `Resumes` via `ResumeEducations` |
| `Vacancies` | FK `UserId` → `Users`; `JobSource` is **jsonb** |
| `CoverLetters` | FKs `UserId`, `VacancyId` |
| `CoverLetterTemplates` | Root entity for generation; FK `UserId` |
| `ResumeEducations` | Join for Resume ↔ Education |

Discover in `psql`: `\d "Users"`, `\d "Vacancies"`, `\d "CoverLetterTemplates"`, etc.

# JSONB Columns

| Table | Columns |
|-------|---------|
| `Users` | `Skills`, `Languages`, `Certificates`, `Projects` |
| `Resumes` | `Skills`, `Projects`, `Certifications`, `Languages`, `Locations`, `ExcludedWords` |
| `Vacancies` | `JobSource`, `JobCategories`, `Skills` |

# Pagination (C# repositories)

`PagedQuery`: `PageSize` (default 20), optional `LastSeenId`, `LastSeenUpdatedAt` for cursor paging.

`PagedResult<T>`: `Items`, `TotalCount`, next-cursor fields, `HasNext`, `TotalPages`.

`BaseRepository` orders by `UpdatedAt` desc, then `Id` desc.

# Minimal SQL examples (dev / test)

Adjust UUIDs and run only against a safe dev database.

**Insert a user** (valid email; omit `Phone` or use `+15551234567` style):

```sql
INSERT INTO "Users" ("Id", "Login", "Password", "Email", "CreatedAt", "UpdatedAt")
VALUES (
  gen_random_uuid(),
  'testuser',
  'changeme',
  'test@example.com',
  NOW(),
  NOW()
);
```

**Insert a vacancy** (requires `UserId` and `JobSource` jsonb):

```sql
INSERT INTO "Vacancies" (
  "Id", "UserId", "Title", "JobSource", "IsChosen", "IsHidden", "CreatedAt", "UpdatedAt"
)
VALUES (
  gen_random_uuid(),
  '<existing-user-guid>'::uuid,
  'Sample job',
  '{"Name": "Manual", "Url": null}'::jsonb,
  false,
  false,
  NOW(),
  NOW()
);
```

**Insert a cover letter** (needs existing `UserId` and `VacancyId`):

```sql
INSERT INTO "CoverLetters" ("Id", "UserId", "VacancyId", "Content", "CreatedAt", "UpdatedAt")
VALUES (
  gen_random_uuid(),
  '<user-guid>'::uuid,
  '<vacancy-guid>'::uuid,
  'Test cover letter body',
  NOW(),
  NOW()
);
```

**Insert a cover letter template** (requires `UserId`):

```sql
INSERT INTO "CoverLetterTemplates" ("Id", "UserId", "Name", "Content", "CreatedAt", "UpdatedAt")
VALUES (
  gen_random_uuid(),
  '<user-guid>'::uuid,
  'Standard Professional',
  'Dear Hiring Manager, I am applying for {{JobTitle}}...',
  NOW(),
  NOW()
);
```

If `gen_random_uuid()` is unavailable, enable `pgcrypto` (`CREATE EXTENSION IF NOT EXISTS pgcrypto;`) or supply explicit UUIDs from the shell.
