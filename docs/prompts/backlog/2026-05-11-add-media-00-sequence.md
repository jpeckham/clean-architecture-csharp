# Prompt Sequence: Add Media Incrementally

## Objective

Use this sequence to implement media support in small, verifiable phases. It decomposes `docs/prompts/2026-05-11-adding-media.md` into prompts that can be run one at a time.

## Architectural Rules

- Preserve component-first Clean Architecture.
- Do not create a shared `Media` domain/application component.
- Keep profile image behavior in `SocialApp.User`.
- Keep post attachment behavior in `SocialApp.Post`.
- Keep binary storage behind component-owned gateway ports.
- Persist metadata only in domain and Mongo/Cosmos documents.
- Keep existing clients working with additive response fields and optional request fields.

## Recommended Order

1. `docs/prompts/2026-05-11-add-media-01-domain-metadata.md`
2. `docs/prompts/2026-05-11-add-media-02-post-component.md`
3. `docs/prompts/2026-05-11-add-media-03-user-profile-image-component.md`
4. `docs/prompts/2026-05-11-add-media-04-api-contracts-and-endpoints.md`
5. `docs/prompts/2026-05-11-add-media-05-cosmos-mongo-persistence.md`
6. `docs/prompts/2026-05-11-add-media-06-local-filesystem-storage.md`
7. `docs/prompts/2026-05-11-add-media-07-web-ui.md`
8. `docs/prompts/2026-05-11-add-media-08-azure-blob-terraform.md`
9. `docs/prompts/2026-05-11-add-media-09-hardening.md`

## Phase Boundaries

- Phases 1-3 should pass component tests without real blob/file storage.
- Phase 4 exposes the behavior over HTTP.
- Phase 5 makes media metadata durable.
- Phase 6 makes local direct upload/download work.
- Phase 7 adds the user-facing Blazor workflow.
- Phase 8 adds Azure infrastructure and cloud storage.
- Phase 9 adds operational polish that is not required for the first usable media slice.

## Verification

After each phase, run the narrow project tests named in that phase. After completing any phase that touches multiple layers, run:

```powershell
dotnet test SocialApp.sln
```

