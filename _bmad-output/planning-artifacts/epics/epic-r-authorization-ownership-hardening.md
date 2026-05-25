# Epic R: Authorization & Ownership Enforcement Hardening (Brownfield)

Close remaining Phase C security gaps by hardening ownership enforcement, authorization behavior consistency, and regression safety across all authenticated write flows.

## Scope Context

- This is a brownfield hardening epic applied to already-shipped APIs.
- Core CRUD features are already merged; this epic focuses on policy consistency and missing edge cases.
- Existing contracts must remain backward compatible unless explicitly documented in story acceptance criteria.

## Story R.2: Endpoint Ownership Policy Audit and Gap Closure

As a **platform maintainer**,
I want all authenticated mutation and sensitive read endpoints to apply consistent ownership checks,
So that no cross-user access path remains in production.

**Acceptance Criteria:**

**Given** all user-scoped endpoints in API/Application layers
**When** ownership logic is reviewed against FR27/FR28
**Then** every endpoint has explicit, testable ownership behavior (`404` or `403` per endpoint contract) and no undocumented variance remains.

**Given** an endpoint currently relies on implicit filtering without explicit ownership guard where required
**When** the gap is found
**Then** handler/repository logic is updated to enforce ownership explicitly with no contract drift.

**Given** ownership behavior changes are needed
**When** implementation is complete
**Then** OpenAPI and story docs are updated for affected status codes.

---

## Story R.3: Authorization Regression Integration Suite

As a **backend engineer**,
I want integration tests that exercise cross-user access attempts across all protected resources,
So that authorization regressions are blocked in CI.

**Acceptance Criteria:**

**Given** protected resources (`users/me` mutations, resumes, educations, templates, cover letters, vacancies)
**When** user A attempts to access user B data
**Then** API returns contract-correct authorization result and never leaks foreign data.

**Given** delete and update flows on soft-deletable entities
**When** ownership is invalid
**Then** tests verify expected status and unchanged target entity state.

**Given** CI runs on PR and merge
**When** tests execute
**Then** the authorization regression suite is included in default `dotnet test backend/JobNecto.slnx` execution.

---

## Story R.4: Consistent Forbidden vs NotFound Contract Matrix

As a **API consumer**,
I want predictable `403` vs `404` behavior for unauthorized or missing resources,
So that client logic and error handling are stable.

**Acceptance Criteria:**

**Given** all user-scoped detail/update/delete endpoints
**When** contract matrix is defined
**Then** each endpoint documents one canonical behavior for: not found, soft-deleted, cross-user.

**Given** implementation and middleware exception mapping
**When** endpoint tests run
**Then** returned status codes match the matrix exactly.

**Given** any endpoint currently deviates from matrix without intent
**When** discovered
**Then** code and tests are corrected in this epic.

---

## Story R.5: Authorization Hardening Completion Gate

As a **team lead**,
I want a formal done gate for Phase C,
So that Phase D starts only after security baseline is verified.

**Acceptance Criteria:**

**Given** stories R.2-R.4 are complete
**When** completion gate runs
**Then** all of the following pass:

- `dotnet build backend/JobNecto.slnx --configuration Release --warnaserror`
- `dotnet test backend/JobNecto.slnx --configuration Release --warnaserror`

**Given** epic completion is validated
**When** documentation is finalized
**Then** roadmap, sprint status, and requirements trace notes are updated to mark Phase C as done.

