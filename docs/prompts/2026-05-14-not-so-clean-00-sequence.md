# Prompt Sequence: Strict Clean Architecture Remediation

## Objective

Use this sequence to address the findings from `docs/prompts/2026-05-14-not-so-clean-findings.md` in small, independently executable prompts.

Each prompt is scoped so it can be run separately, reviewed separately, and verified with focused tests before moving to the next architectural change.

## Architectural Rules

- Preserve the repository's component-first intent: `SocialApp.User` and `SocialApp.Post` remain the primary business components.
- Do not introduce a generic shared core, shared application, shared infrastructure, or shared abstractions project.
- Dependencies must continue to point inward.
- Prefer component-owned ports and DTOs over cross-component reuse of implementation helpers.
- Keep user behavior in the User component and post behavior in the Post component.
- Keep API, Web, persistence, storage, and framework concerns outside inner business rules.
- Keep changes additive where possible and avoid unrelated behavior changes.

## Recommended Order

1. `docs/prompts/2026-05-14-not-so-clean-01-profile-post-read-port.md`
2. `docs/prompts/2026-05-14-not-so-clean-02-password-service-extraction.md`
3. `docs/prompts/2026-05-14-not-so-clean-03-profile-image-route-generation.md`
4. `docs/prompts/2026-05-14-not-so-clean-04-media-upload-gateway-resolution.md`
5. `docs/prompts/2026-05-14-not-so-clean-05-inmemory-post-search-gateway.md`
6. `docs/prompts/2026-05-14-not-so-clean-06-architecture-test-hardening.md`
7. `docs/prompts/2026-05-14-not-so-clean-07-assembly-ring-separation.md`

## Phase Boundaries

- Phases 1-5 are focused fixes for concrete violations found in the current code.
- Phase 6 strengthens enforcement around the improved boundaries.
- Phase 7 is the larger project-structure refactor and should be done after the smaller violations are cleaned up.

## Verification

After each phase, run the narrow tests named in that phase.

After completing any phase that touches API, Web, infrastructure registration, HTTP contracts, appsettings, persistence mappings, Dockerfiles, or user-visible flows, also run from the repository root:

```powershell
docker compose config
docker compose build
```

When a phase changes a user-facing flow, also run:

```powershell
docker compose up -d
```

Then smoke test the relevant flow through:

- API: `http://localhost:8080`
- Web: `http://localhost:8081`

