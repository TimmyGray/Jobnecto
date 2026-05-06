# Jobnecto Frontend Implementation Guide

## 1. Purpose and Audience

This document is the implementation handoff for:

- Frontend developers building the Jobnecto client application.
- Product and UX designers defining interaction and visual behavior.
- QA engineers validating UX, contract fidelity, and accessibility.

Scope in this version:

- Fully aligned with currently implemented backend endpoints:
  - `POST /api/v1/users`
  - `POST /api/v1/users/token/refresh`
  - `GET /api/v1/users/me`
  - `PATCH /api/v1/users/me`
  - `POST /api/v1/users/me/avatar`
  - `PUT /api/v1/users/me/avatar`
  - `DELETE /api/v1/users/me/avatar`
  - `POST /api/v1/resumes`
  - `GET /api/v1/resumes`
  - `GET /api/v1/resumes/{id}`
  - `PATCH /api/v1/resumes/{id}`
  - `DELETE /api/v1/resumes/{id}`
  - `POST /api/v1/educations`
  - `GET /api/v1/educations`
  - `GET /api/v1/educations/{id}`
  - `PATCH /api/v1/educations/{id}`
  - `DELETE /api/v1/educations/{id}`

Out-of-scope but planned (design stubs only): vacancies, cover letter templates, and cover letters.

## 2. Frontend UI Architecture

### 2.1 Application Layers

Use a feature-sliced architecture:

- `app`: app bootstrap, providers, router, global error boundaries.
- `processes`: auth lifecycle, token refresh orchestration.
- `pages`: route-level UI composition.
- `widgets`: composed blocks (profile card, resume table, education timeline).
- `features`: business actions (create resume, update profile, upload avatar).
- `entities`: shared domain models (UserProfile, Resume, Education).
- `shared`: UI kit, utilities, API client, schema validators, constants.

### 2.2 Folder Blueprint

```text
src/
  app/
    providers/
    router/
  processes/
    auth/
  pages/
    auth-sign-up/
    dashboard/
    profile/
    resumes/
    resume-detail/
    educations/
    education-detail/
    settings/
  widgets/
    top-nav/
    profile-summary/
    cursor-pagination/
  features/
    user/
      update-profile/
      avatar-upload/
    resume/
      create-resume/
      update-resume/
      delete-resume/
    education/
      create-education/
      update-education/
      delete-education/
  entities/
    user/
    resume/
    education/
  shared/
    api/
      client.ts
      problem-details.ts
      endpoints/
    ui/
    lib/
    config/
```

### 2.3 State Model

- Server state: query/mutation cache (recommended: TanStack Query).
- Client state: minimal ephemeral UI state (drawer open, active tab, filter chips).
- Form state: dedicated schema-driven form state (recommended: React Hook Form + Zod/Yup).
- Auth state:
  - Primary auth transport is HTTP-only cookie.
  - Refresh endpoint may return `accessToken` in body only when bearer transport is used.
  - Browser-first implementation should treat cookie transport as canonical.

### 2.4 Routing

Recommended routes:

- `/sign-up`
- `/dashboard`
- `/profile`
- `/resumes`
- `/resumes/:id`
- `/educations`
- `/educations/:id`
- `/settings`

Guarded routes:

- All except `/sign-up` should require authenticated session.

## 3. Design System Tokens

Define tokens in a single source (JSON or TS), then export to CSS variables.

### 3.1 Color Tokens

```json
{
  "color": {
    "bg": {
      "canvas": "#F7F9FC",
      "surface": "#FFFFFF",
      "elevated": "#F1F5FA"
    },
    "text": {
      "primary": "#0F172A",
      "secondary": "#334155",
      "muted": "#64748B",
      "inverse": "#FFFFFF"
    },
    "brand": {
      "primary": "#0A66C2",
      "primaryHover": "#004182",
      "accent": "#14B8A6"
    },
    "status": {
      "success": "#15803D",
      "warning": "#D97706",
      "danger": "#B91C1C",
      "info": "#0369A1"
    },
    "border": {
      "default": "#D0D9E5",
      "strong": "#94A3B8",
      "focus": "#0A66C2"
    }
  }
}
```

### 3.2 Typography Tokens

```json
{
  "font": {
    "family": {
      "sans": "Manrope, Segoe UI, sans-serif",
      "mono": "IBM Plex Mono, Consolas, monospace"
    },
    "size": {
      "xs": 12,
      "sm": 14,
      "md": 16,
      "lg": 20,
      "xl": 28,
      "xxl": 36
    },
    "lineHeight": {
      "tight": 1.2,
      "base": 1.5,
      "relaxed": 1.7
    },
    "weight": {
      "regular": 400,
      "medium": 500,
      "semibold": 600,
      "bold": 700
    }
  }
}
```

### 3.3 Spacing, Radius, Shadow, Z-Index

```json
{
  "space": { "1": 4, "2": 8, "3": 12, "4": 16, "5": 20, "6": 24, "8": 32, "10": 40, "12": 48 },
  "radius": { "sm": 6, "md": 10, "lg": 14, "pill": 999 },
  "shadow": {
    "sm": "0 1px 2px rgba(15, 23, 42, 0.06)",
    "md": "0 6px 18px rgba(15, 23, 42, 0.08)",
    "lg": "0 12px 32px rgba(15, 23, 42, 0.12)"
  },
  "z": { "base": 1, "dropdown": 1000, "sticky": 1100, "modal": 1200, "toast": 1300 }
}
```

### 3.4 Component Token Mapping

- Buttons:
  - Primary: `brand.primary` + `text.inverse`
  - Secondary: `bg.surface` + `text.primary` + `border.default`
  - Danger: `status.danger`
- Inputs:
  - Border default: `border.default`
  - Focus ring: `border.focus`
  - Error border: `status.danger`
- Alerts:
  - Success, warning, error, info each mapped to status palette.

## 4. Page-by-Page UX Behavior

## 4.1 Sign Up Page (`/sign-up`)

Purpose: register account and establish authenticated session.

Primary flow:

1. User fills sign-up form.
2. Submit to `POST /api/v1/users`.
3. On 201, backend sets auth cookie and returns user payload.
4. Frontend routes to `/dashboard` and hydrates profile via `GET /api/v1/users/me`.

Required UI states:

- Idle
- Submitting
- Field validation errors
- Conflict error (email/login already used)
- Server error

## 4.2 Dashboard Page (`/dashboard`)

Purpose: high-level orientation.

Blocks:

- Profile completion card (from user profile fields).
- Resume count and quick links.
- Education count and quick links.
- Upcoming section placeholders for vacancies and cover letters.

Behavior:

- Parallel fetch for profile, resumes first page, educations first page.
- Partial failure tolerance: one widget failing must not break others.

## 4.3 Profile Page (`/profile`)

Purpose: view and update current user profile and avatar.

Actions:

- Load current profile (`GET /api/v1/users/me`).
- Partial updates (`PATCH /api/v1/users/me`).
- Avatar upload/update (`POST` or `PUT /api/v1/users/me/avatar`).
- Avatar delete (`DELETE /api/v1/users/me/avatar`).

Behavior rules:

- Save button enabled only when dirty and valid.
- Optimistic preview allowed for avatar, but commit only after server 200.
- On successful update, revalidate profile query.

## 4.4 Resumes List Page (`/resumes`)

Purpose: create, browse, paginate, and delete resumes.

Actions:

- Create resume (`POST /api/v1/resumes`).
- Cursor list (`GET /api/v1/resumes?pageSize=&lastSeenId=&lastSeenUpdatedAt=`).
- Delete resume (`DELETE /api/v1/resumes/{id}`).

Behavior rules:

- Cursor-based pagination only (not offset page index).
- Show load-more pattern, not page numbers.
- After delete, remove row immediately and refresh cursor metadata.

## 4.5 Resume Detail/Edit Page (`/resumes/:id`)

Actions:

- Fetch detail (`GET /api/v1/resumes/{id}`).
- Patch update (`PATCH /api/v1/resumes/{id}`).

Behavior rules:

- If 404, show not-found state with back-to-list CTA.
- Prevent submit when no fields changed.
- Submit only changed fields where possible.

## 4.6 Educations List Page (`/educations`)

Purpose: create, browse, paginate, and delete education records.

Actions:

- Create (`POST /api/v1/educations`).
- Cursor list (`GET /api/v1/educations`).
- Delete (`DELETE /api/v1/educations/{id}`).

Behavior mirrors resumes list.

## 4.7 Education Detail/Edit Page (`/educations/:id`)

Actions:

- Fetch detail (`GET /api/v1/educations/{id}`).
- Patch update (`PATCH /api/v1/educations/{id}`).

Behavior mirrors resume detail/edit, with education-specific validation.

## 4.8 Settings Page (`/settings`)

Purpose:

- Session controls and support diagnostics.
- Trigger refresh token call if needed (`POST /api/v1/users/token/refresh`).

## 5. Component Contracts

Use strongly typed props and explicit events for all reusable components.

## 5.1 Core Form Components

### `TextField`

Props:

- `name: string`
- `label: string`
- `value: string`
- `onChange: (value: string) => void`
- `onBlur?: () => void`
- `required?: boolean`
- `maxLength?: number`
- `error?: string`
- `hint?: string`
- `autoComplete?: string`

### `SelectField<T>`

Props:

- `name: string`
- `label: string`
- `value: T | null`
- `options: Array<{ label: string; value: T }>`
- `onChange: (value: T) => void`
- `placeholder?: string`
- `error?: string`

### `TagInput`

For skills, projects, certifications, excluded words.

Props:

- `values: string[]`
- `onChange: (values: string[]) => void`
- `maxItemLength?: number`
- `maxItems?: number`
- `allowDuplicates?: boolean`
- `error?: string`

## 5.2 Domain Components

### `ProfileForm`

Input model:

- `loginName?: string`
- `email?: string`
- `phone?: string`
- `location?: string`
- `about?: string`
- `avatar?: string`

Events:

- `onSubmit(payload: UpdateCurrentUserPayload)`
- `onCancel()`

### `AvatarUploader`

Props:

- `currentAvatar?: string`
- `maxBytes: number` (must be 5 MB)
- `acceptedMimeTypes: string[]` (jpeg, jpg, png, webp, gif)

Events:

- `onUpload(file: File)`
- `onDelete()`

### `ResumeForm`

Input model (create/update compatible):

- `title?: string`
- `salary?: number`
- `currency?: Currency`
- `skills?: string[]`
- `workLocationType?: WorkLocationType`
- `experience?: Experience`
- `projects?: string[]`
- `certifications?: string[]`
- `languages?: Array<{ language: Language; level: LanguageLevel }>`
- `locations?: Location[]`
- `excludedWords?: string[]`

### `EducationForm`

Input model:

- `title?: string`
- `specialization?: string`
- `degree?: Degree`

## 6. API Integration Mapping

Base URL:

- `http://localhost:5000/api/v1` in development unless overridden.

Auth transport:

- Browser clients rely on server-managed HTTP-only cookie.
- Always send credentials in fetch/XHR where required.

## 6.1 Endpoints by Feature

| Feature | Endpoint | Method | Success | Notes |
| --- | --- | --- | --- | --- |
| Sign up | `/users` | POST | 201 | Sets auth cookie; Location points to `/api/v1/users/me` |
| Get profile | `/users/me` | GET | 200 | Requires auth |
| Update profile | `/users/me` | PATCH | 200 | Partial update |
| Upload avatar | `/users/me/avatar` | POST | 200 | multipart/form-data |
| Replace avatar | `/users/me/avatar` | PUT | 200 | multipart/form-data |
| Delete avatar | `/users/me/avatar` | DELETE | 200 | Returns updated profile |
| Refresh token | `/users/token/refresh` | POST | 200 | Body token only for bearer transport |
| Create resume | `/resumes` | POST | 201 | Returns created resume |
| List resumes | `/resumes` | GET | 200 | Cursor pagination |
| Resume detail | `/resumes/{id}` | GET | 200 | 404 if not found or not owned |
| Update resume | `/resumes/{id}` | PATCH | 200 | Partial update |
| Delete resume | `/resumes/{id}` | DELETE | 204 | Soft delete |
| Create education | `/educations` | POST | 201 | Returns created education |
| List educations | `/educations` | GET | 200 | Cursor pagination |
| Education detail | `/educations/{id}` | GET | 200 | 404 if not found or not owned |
| Update education | `/educations/{id}` | PATCH | 200 | Partial update |
| Delete education | `/educations/{id}` | DELETE | 204 | Soft delete |

## 6.2 Problem Details Error Shape

Backend uses RFC 7807 Problem Details (`application/problem+json`).

Expected fields:

- `status`
- `title`
- `detail`
- `instance`
- `traceId` in `extensions`
- `errors` map for validation failures (400)

Frontend contract:

- Normalize all non-2xx responses into a single `ApiError` model.
- Surface `errors[field]` messages inline for forms.
- Surface generic banner for non-validation failures.

## 6.3 Cursor Pagination Contract

Response fields:

- `items`
- `totalCount`
- `lastSeenId`
- `lastSeenUpdatedAt`
- `pageSize`
- `hasNext`
- `totalPages` (derived server-side from totalCount and pageSize)

Query rules:

- `pageSize < 1` defaults to 20 on server.
- `pageSize > 100` is capped to 100 on server.
- Send both `lastSeenId` and `lastSeenUpdatedAt` from previous response for next-page continuity.

## 7. Form Validation Rules (Client Mirror)

Client-side validation must mirror backend to reduce failed submissions.

## 7.1 User Sign-Up

- `loginName`: required, 3-50 chars, regex `^[A-Za-z0-9_]+$`
- `email`: required, valid email, max 50
- `password`: required, min 8, max 50
- `phone`: optional, max 20, E.164 regex `^\+[1-9]\d{1,14}$`
- `location`: optional, max 50, must be valid `Location` enum value
- `about`: optional, max 5000

## 7.2 Update Profile

- All fields optional, but if provided must pass constraints above.
- `avatar` string field (if sent via patch): max 2048, must be https URL or storage key pattern `^[A-Za-z0-9_./-]+$`.

## 7.3 Avatar Upload

- File required.
- File size <= 5 MB.
- Allowed MIME: `image/jpeg`, `image/jpg`, `image/png`, `image/webp`, `image/gif`.
- Client should pre-check MIME and size before upload.

## 7.4 Create/Update Resume

- `title`: optional, max 500
- `skills`: optional array; if provided each item non-empty, max 30 chars
- `workLocationType`: optional, must match enum (`OnSite`, `Remote`, `Hybrid`)
- `currency`: optional, valid enum
- `experience`: optional, valid enum (`LessThanOneYear`, `OneToThreeYears`, `ThreeToFiveYears`, `MoreThanFiveYears`)
- `salary`: optional, must be >= 0
- Update request must include at least one updatable field.

## 7.5 Create/Update Education

Create:

- `title`: required, non-whitespace, max 100
- `specialization`: required, non-whitespace, max 100
- `degree`: required, enum (`Bachelor`, `Master`, `PhD`, `PostDoc`, `Other`)

Update:

- At least one of `title`, `specialization`, `degree` required.
- If provided, title and specialization must be non-empty and within length limit.
- Degree, if provided, must be valid enum.

## 8. Loading, Error, and Empty State Specifications

## 8.1 Loading States

- Page-level skeleton for initial route load.
- Section-level skeleton for widgets fetched independently.
- Inline submit spinners on action buttons.
- Preserve layout dimensions to avoid cumulative layout shift.

## 8.2 Error States

- 400 validation:
  - Inline field errors + summary banner.
- 401 unauthorized:
  - Redirect to sign-up or auth recovery flow.
  - Preserve intended destination for post-auth return.
- 403 forbidden:
  - Show permission screen with explanation and recovery CTA.
- 404 not found:
  - In detail pages, show entity-not-found empty state and back link.
- 409 conflict:
  - For sign-up/profile unique collisions, show explicit action guidance.
- 500:
  - Show generic recoverable error with retry.

## 8.3 Empty States

- Resumes list empty:
  - Headline: "No resumes yet"
  - CTA: "Create first resume"
- Educations list empty:
  - Headline: "No education records yet"
  - CTA: "Add education"
- Dashboard empty summary:
  - Show onboarding checklist with direct links.

## 9. Accessibility Requirements

Must satisfy WCAG 2.2 AA baseline.

Mandatory requirements:

- Semantic structure:
  - One `h1` per page, logical heading order.
- Keyboard:
  - Full tab/shift+tab navigation, visible focus ring, no keyboard traps.
- Form accessibility:
  - Inputs tied to labels via `for`/`id`.
  - Errors tied with `aria-describedby`.
- Dynamic updates:
  - Toasts and form errors announced via `aria-live` regions.
- Color contrast:
  - Body text >= 4.5:1, large text >= 3:1.
- Hit targets:
  - Interactive target size >= 24x24 px.
- File upload:
  - Dropzone and button flow both keyboard accessible.
- Reduced motion:
  - Honor `prefers-reduced-motion` for all non-essential animation.

## 10. Responsive Breakpoints and Layout Rules

Breakpoint tokens:

- `xs`: 0-479
- `sm`: 480-767
- `md`: 768-1023
- `lg`: 1024-1439
- `xl`: 1440+

Layout behavior:

- `xs/sm`:
  - Single-column pages.
  - Bottom-sheet or full-screen modal for create/edit forms.
- `md`:
  - Two-column details where useful (form + live preview).
- `lg/xl`:
  - Primary content with optional contextual side panel.

Tables/lists:

- On `xs/sm`, card list layout instead of dense table.
- Preserve action affordances with sticky action bar when editing.

## 11. Motion Guidelines

Use motion to communicate state changes, not decoration.

Motion tokens:

- Duration:
  - Fast: 120ms
  - Standard: 180ms
  - Emphasis: 260ms
- Easing:
  - Standard: `cubic-bezier(0.2, 0, 0, 1)`
  - Exit: `cubic-bezier(0.4, 0, 1, 1)`

Patterns:

- Page transitions:
  - Fade + slight y-translation (4px), max 180ms.
- List updates:
  - Add item highlight pulse (single cycle).
  - Remove item collapse animation, max 180ms.
- Validation errors:
  - Do not shake fields; use color/focus and helper text.
- Loading:
  - Skeleton shimmer optional; disable when reduced motion is enabled.

## 12. Enum Sources for Frontend Select Controls

To avoid drift, generate frontend enum options from backend OpenAPI or shared schema.

Current backend enum values:

- `WorkLocationType`: `OnSite`, `Remote`, `Hybrid`
- `Experience`: `LessThanOneYear`, `OneToThreeYears`, `ThreeToFiveYears`, `MoreThanFiveYears`
- `Degree`: `Bachelor`, `Master`, `PhD`, `PostDoc`, `Other`
- `Currency`: `USD`, `EUR`, `GBP`, `CAD`, `AUD`, `CHF`, `JPY`, `CNY`, `INR`, `RUB`, `UAH`, `PLN`, `SEK`, `NOK`, `DKK`, `NZD`, `MXN`, `BRL`
- `Location`: use the full backend enum set; do not hardcode partial list.
- `Language`: use backend enum list from domain contract.
- `LanguageLevel`: `Beginner`, `Intermediate`, `Advanced`, `Native`

## 13. Developer Handoff Acceptance Checklist

Definition of done for frontend implementation handoff:

### 13.1 Architecture and Code Quality

- [ ] Feature-sliced structure implemented.
- [ ] Shared API client handles credentials and Problem Details parsing.
- [ ] Typed models defined for user, resume, education, and pagination envelopes.
- [ ] No duplicated validation logic across forms.

### 13.2 Contract Fidelity

- [ ] Every implemented backend endpoint has matching frontend service function.
- [ ] Request/response typing matches server contracts.
- [ ] Cursor pagination behavior verified end-to-end.
- [ ] 400/401/403/404/409/500 states handled and visually tested.

### 13.3 UX and Accessibility

- [ ] All required loading/error/empty states implemented per page.
- [ ] Keyboard-only flow validated for critical journeys.
- [ ] Form errors are screen-reader announced.
- [ ] Color contrast and focus visibility pass AA checks.

### 13.4 Responsive and Motion

- [ ] Layout verified at xs, sm, md, lg, xl breakpoints.
- [ ] Edit/create flows are fully usable on mobile.
- [ ] Motion tokens applied consistently and reduced-motion respected.

### 13.5 QA and Release Readiness

- [ ] E2E tests cover sign-up, profile update, avatar upload, resume CRUD, education CRUD.
- [ ] API error mocking includes validation and conflict examples.
- [ ] Cross-browser smoke tests pass (Chromium, Firefox, WebKit minimum).
- [ ] Final review completed by frontend developer and designer together.

## 14. Implementation Notes for Next Backend Phases

When vacancies and cover-letter APIs are merged, extend this guide by adding:

- Vacancies list/detail/filter page contracts.
- Cover letter templates CRUD UX.
- Cover letter generation flow (manual first, LLM-assisted later).
- Job application orchestration UI states.

Keep this document as the canonical frontend handoff artifact for Jobnecto client-side development.
