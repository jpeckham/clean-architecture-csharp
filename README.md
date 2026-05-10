# SocialApp Clean Architecture Reference

This repository is a C# reference implementation of Clean Architecture using component-first decomposition. It uses a small microblogging-style domain because the behavior is familiar: users create accounts, log in, find other users, create posts, scroll feeds, like, reply, repost, and delete.

The top-level architecture screams the business capabilities:

```text
src/
  SocialApp.User/
  SocialApp.Post/

tests/
  SocialApp.User.Tests/
  SocialApp.Post.Tests/
  SocialApp.Architecture.Tests/
```

There is no application host. Executable behavior is demonstrated through tests.

## Component-First Architecture

The solution is organized by releasable business component, not by technical layer. Each component owns its entities, use cases, boundaries, controllers, presenters, request models, response models, view models, gateway interfaces, and in-memory gateway implementations.

Inside each component, Clean Architecture roles remain visible:

```text
Controllers -> Input Boundaries -> Interactors -> Gateways
                                      |
                                      v
                             Output Boundaries -> Presenters -> View Models
```

This keeps business rules independent from frameworks and makes every boundary explicit.

## Why There Is No Shared Core

There is intentionally no `SharedKernel`, `Core`, `Common`, or `Abstractions` project.

Shared projects often become dumping grounds, hide coupling, and weaken component independence. This repository prefers small duplication inside each component unless a shared concept has overwhelming architectural justification.

## Dependency Rules

- `SocialApp.User` does not reference `SocialApp.Post`.
- `SocialApp.Post` does not reference `SocialApp.User`.
- Components do not reference ASP.NET, Entity Framework, MediatR, ORMs, or DI frameworks.
- Entities do not depend on controllers, presenters, gateways, or interactors.
- Interactors depend on abstractions and entities, not frameworks.
- Controllers translate input and invoke input boundaries.
- Presenters implement output boundaries and build view models.

These rules are enforced in `SocialApp.Architecture.Tests`.

## How To Add A New Component

1. Create `src/SocialApp.<ComponentName>`.
2. Add internal folders: `Entities`, `UseCases`, `Gateways`, `Controllers`, `Presenters`, `RequestModels`, `ResponseModels`, `ViewModels`.
3. Create `tests/SocialApp.<ComponentName>.Tests`.
4. Keep gateway interfaces inside the owning component.
5. Add architecture tests that prevent forbidden references and boundary leakage.
6. Avoid adding a shared library unless the duplication is genuinely harmful and the dependency direction remains stable.

## How To Add A New Use Case

1. Add a request model and response model inside the component.
2. Add input and output boundary interfaces in the component's `UseCases` folder.
3. Implement an interactor that coordinates entities and gateway interfaces.
4. Add a controller that creates the request model and calls the input boundary.
5. Add a presenter that implements the output boundary and builds a view model.
6. Write behavior tests that demonstrate the full request flow.
7. Extend architecture tests if the use case introduces a new boundary rule.

## Running Tests

```powershell
dotnet test SocialApp.sln
```

The architecture test project is not a unit test suite. Its job is to mechanically prevent architectural decay.
