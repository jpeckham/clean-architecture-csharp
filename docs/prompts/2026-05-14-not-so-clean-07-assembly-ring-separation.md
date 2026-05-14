# Prompt: Split Business Components Into Assembly-Enforced Rings

## Objective

Move from namespace-separated rings inside `SocialApp.User` and `SocialApp.Post` to assembly-enforced rings while preserving component-first organization.

Current finding:

- `SocialApp.User` and `SocialApp.Post` currently package entities, use cases, boundaries, controllers, presenters, view models, gateway interfaces, and in-memory gateway implementations together.
- This keeps the repository readable but prevents strict Clean Architecture enforcement at the assembly level.

## Requirements

- Preserve component-first decomposition around User and Post.
- Do not create layer-first global projects such as `SocialApp.Application`, `SocialApp.Domain`, or `SocialApp.Infrastructure`.
- Do not create a shared core or shared abstractions project.
- Keep dependencies pointing inward.
- Keep API and Web behavior compatible.
- Keep persistence and storage implementations outside inner business-rule assemblies.

## Suggested Target Structure

Use component-scoped ring assemblies, for example:

```text
src/
  SocialApp.User.Domain/
  SocialApp.User.Application/
  SocialApp.User.Adapters/
  SocialApp.Post.Domain/
  SocialApp.Post.Application/
  SocialApp.Post.Adapters/
  SocialApp.Infrastructure.InMemory/
  SocialApp.Infrastructure.CosmosMongo/
  SocialApp.Infrastructure.AcsEmail/
  SocialApp.Infrastructure.LocalStorage/
  SocialApp.Infrastructure.AzureBlobStorage/
  SocialApp.Api/
  SocialApp.Web/
```

Adjust the exact names only if the repository already has a stronger convention.

## Suggested Scope

- Move User and Post entities into their component domain assemblies.
- Move use cases, request/response models, input/output boundaries, and gateway ports into component application assemblies.
- Move controllers, presenters, and view models into component adapter assemblies.
- Move in-memory gateway implementations into `SocialApp.Infrastructure.InMemory` or test projects.
- Update project references so:
  - Domain references no outer project.
  - Application references Domain.
  - Adapters reference Application and Domain only as needed.
  - Infrastructure references component application/domain contracts as needed.
  - API composes adapters and infrastructure.
  - Web remains HTTP-only and does not reference business or infrastructure projects.
- Use `InternalsVisibleTo` only where tests genuinely require it.
- Update namespaces, DI registration, tests, and docs.
- Keep this refactor mechanical where possible; do not change behavior unless required by the move.

## Verification

- Run component, infrastructure, API, Web, and architecture tests.
- Run `dotnet test SocialApp.sln`.
- Run `docker compose config`.
- Run `docker compose build`.
- Because this changes API composition and application wiring, run `docker compose up -d` and smoke test account creation, login, feed, profile viewing, and media upload through API `http://localhost:8080` and Web `http://localhost:8081`.

