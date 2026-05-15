# Prompt: Re-evaluate Assembly Ring Separation Against Package-By-Component

## Objective

Revisit whether assembly-enforced rings are appropriate for this repository now that the repository's purpose is clarified:

> This repository exists as a reference architecture that strictly follows Robert C. Martin's Clean Architecture book while still being a full working application. Anything that obscures the reference architecture should get out of the way.

The key architectural concern is that splitting `SocialApp.User` and `SocialApp.Post` into projects such as `SocialApp.User.Domain`, `SocialApp.User.Application`, and `SocialApp.User.Adapters` may combine package-by-feature with package-by-layer/ring. That can undermine the repository's teaching goal if the project structure no longer clearly communicates package-by-component.

## Current Finding

- `SocialApp.User` and `SocialApp.Post` are currently the primary business packages.
- Each component package contains entities, use cases, boundaries, controllers, presenters, view models, gateway interfaces, and in-memory gateway implementations.
- This keeps the repository's package structure aligned with component-first organization.
- It also means ring separation is enforced mostly by folders, namespaces, tests, and discipline rather than by separate assemblies.

## Architectural Constraint

Package-by-component is primary.

Do not replace the component packages with ring/layer packages unless there is a clear, book-faithful reason that improves the repository as a Clean Architecture reference. Compile-time ring enforcement is not enough justification by itself if the resulting project structure teaches the wrong lesson.

## Questions To Answer Before Any Implementation

1. Does Robert C. Martin's Clean Architecture require rings to be separate deployable/package units, or does it require source dependencies to point inward?
2. In this repository, what is the clearest project/package structure for teaching package-by-component?
3. Would component-scoped ring assemblies clarify or confuse that lesson?
4. Can architecture tests enforce the dependency rule strongly enough while preserving `SocialApp.User` and `SocialApp.Post` as the package boundaries?
5. Which current responsibilities are truly outer details that should move out of the business component packages without turning the codebase into package-by-layer?

## Possible Scope

This prompt may result in no implementation change.

If there is useful scope, prefer changes that preserve `SocialApp.User` and `SocialApp.Post` as the primary component packages:

- Keep `src/SocialApp.User` and `src/SocialApp.Post` as the business component assemblies.
- Keep namespaces/folders that make Clean Architecture roles visible inside each component.
- Strengthen architecture tests that prevent:
  - entity dependencies on use cases, gateways, presenters, controllers, view models, transport models, or framework details
  - use-case dependencies on controllers, presenters, API, Web, infrastructure, HTTP, JSON serialization, DI framework APIs, database drivers, or cloud SDKs
  - presenters containing HTTP route literals or transport-specific concerns
  - controllers depending on concrete interactors instead of input boundaries where avoidable
  - cross-component references between User and Post
- Consider moving only concrete infrastructure details out of component packages when they are clearly not part of the component's reference architecture story.
- Keep API and Web behavior compatible.
- Do not create global layer-first projects such as `SocialApp.Application`, `SocialApp.Domain`, or `SocialApp.Infrastructure`.
- Do not create a shared core or shared abstractions project.

## Non-Goals

- Do not split `SocialApp.User` into `SocialApp.User.Domain`, `SocialApp.User.Application`, and `SocialApp.User.Adapters` unless the evaluation proves that this does not harm the package-by-component lesson.
- Do not split `SocialApp.Post` into ring assemblies for compile-time enforcement alone.
- Do not optimize for conventional .NET Clean Architecture templates if they conflict with the book's package-by-component emphasis.
- Do not introduce extra projects, abstractions, or indirection only to satisfy a purist interpretation if they make the reference application harder to understand.

## Deliverable

Produce a short architectural recommendation before touching code:

- State whether there is any implementation scope.
- If no, explain why assembly ring separation should be rejected for this repository.
- If yes, identify the smallest changes that improve strict Clean Architecture adherence while preserving package-by-component.
- Include the specific tests or documentation updates that would prove the intended architecture.

## Verification If Code Changes Are Made

- Run the relevant component, infrastructure, API, Web, and architecture tests.
- Run `dotnet test SocialApp.sln`.
- For changes touching API/Web wiring or user-visible flows, follow the repository Docker Compose verification requirements:
  - `docker compose config`
  - `docker compose build`
  - `docker compose up -d`
  - smoke test through API `http://localhost:8080` and Web `http://localhost:8081`
