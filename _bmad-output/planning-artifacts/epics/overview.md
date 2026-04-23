# Overview

This document provides the complete epic and story breakdown for Jobnecto Phase B, decomposing the requirements from the PRD and Architecture decisions into implementable stories.

## Readiness Adjustments After Epic 1 Retrospective

- Shared auth policy: browser authentication flows issue JWTs via HTTP-only cookies; any non-browser client flow must explicitly document `Authorization: Bearer` transport and token renewal behavior before implementation.
- Password handling: passwords are never stored in plaintext; hashing and any required migration/backfill work must be planned before later epics depend on authenticated production-grade user accounts.
- Uniqueness and concurrency: any business rule that returns `409 Conflict` must be enforced by a database constraint and mapped through global exception handling, with concurrent create/update integration coverage where races are plausible.
- Secrets policy: planning artifacts may reference configuration keys and local config files, but must not embed live credentials or secrets.
