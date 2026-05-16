# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```powershell
# Build
dotnet build SocialApp.sln

# Run all tests
dotnet test SocialApp.sln

# Run a single test project
dotnet test tests/SocialApp.User.Tests/SocialApp.User.Tests.csproj

# Run a single test by name
dotnet test SocialApp.sln --filter "FullyQualifiedName~TestMethodName"

# Run the full stack locally (preferred for integration testing)
docker compose up --build

# Reset local MongoDB data
docker compose down -v
```

**Testing rule:** Always run tests against the docker-compose local stack when any test touches MongoDB or infrastructure. Start `docker compose up --build` before running `SocialApp.Infrastructure.CosmosMongo.Tests` or any test suite that requires a live database. The API and component unit tests use in-memory gateways and do not need Docker.

## Design imperative

This repository is a strict reference implementation of Robert C. Martin's *Clean Architecture*. Every decision must adhere minimally and faithfully to the book. Do not introduce patterns, abstractions, or terminology from outside it — no CQRS, no Mediator, no Domain Events, no Value Objects, no Aggregates (DDD), no Result types, no specification pattern, no event sourcing, no repository pattern by that name. Even if a pattern is widely considered "good", adding it here is wrong because it obscures the Clean Architecture signal. When in doubt, do less and use the book's own words.

Use only the terminology from the book: **Entity**, **Use Case**, **Interface Adapter** (Controllers, Presenters, Gateways), **Framework & Driver** (the outermost ring). The folder names and class suffixes in this repo (`Interactor`, `InputBoundary`, `OutputBoundary`, `Controller`, `Presenter`, `Gateway`, `RequestModel`, `ResponseModel`, `ViewModel`) are deliberate mappings to that terminology — do not rename or restructure them.

## Architecture

This is a component-first Clean Architecture. The top-level split is by **business component**, not by technical layer.

### Dependency direction

```
SocialApp.Web  ──HTTP──>  SocialApp.Api
                               │
                SocialApp.Infrastructure.CosmosMongo
                SocialApp.Infrastructure.AcsEmail
                SocialApp.Infrastructure.AzureBlobStorage
                SocialApp.Infrastructure.LocalStorage
                               │
                    SocialApp.User   SocialApp.Post
```

Business components (`SocialApp.User`, `SocialApp.Post`) sit at the center. They have **no** references to ASP.NET Core, MongoDB driver, Azure SDKs, MediatR, EF, or each other. `SocialApp.Web` calls the API over HTTP only and has no reference to any `SocialApp.*` business or infrastructure project.

These rules are mechanically enforced by `tests/SocialApp.Architecture.Tests/ArchitectureRulesTests.cs`.

### Inside each business component

Each component owns the full Clean Architecture stack:

```
Controllers → InputBoundary → Interactor → Gateways (interfaces)
                                   │
                           OutputBoundary → Presenter → ViewModels
```

Folders: `Entities`, `UseCases`, `Gateways`, `Controllers`, `Presenters`, `RequestModels`, `ResponseModels`, `ViewModels`.

Gateway **interfaces** live inside the owning component. Implementations live in `SocialApp.Infrastructure.*`.

### No shared kernel

There is intentionally no `SharedKernel`, `Core`, `Common`, or `Abstractions` project. Small duplication inside each component is preferred over coupling through a shared library.

### API composition root

`SocialApp.Api` is the composition root. It wires component controllers, interactors, presenters, and infrastructure gateway implementations together via ASP.NET Core DI. HTTP concerns (status codes, CORS, auth middleware, request validation at the transport edge) belong here, not in business components.

### Infrastructure

`SocialApp.Infrastructure.CosmosMongo` implements all component gateway interfaces using the MongoDB driver. Document types are internal to the infrastructure project; `CosmosMongoMappers` converts between documents and component entities.

Local docker-compose uses `mongo:7` on `localhost:27017`. The API container receives `CosmosMongo__ConnectionString=mongodb://mongo:27017`. Without that env var, the API falls back to in-memory gateways.

## Adding a new use case

1. Add request model and response model inside the component.
2. Add input and output boundary interfaces in `UseCases/`.
3. Implement an interactor that coordinates entities and gateway interfaces.
4. Add a controller that builds the request model and calls the input boundary.
5. Add a presenter that implements the output boundary and builds a view model.
6. Wire the interactor and presenter in `SocialApp.Api/Program.cs` or the relevant endpoint file.
7. Implement persistence in `SocialApp.Infrastructure.CosmosMongo` if the use case needs a new gateway.
8. Add behavior tests that drive the full request flow. Architecture tests catch boundary violations automatically.

## Adding a new component

1. Create `src/SocialApp.<ComponentName>/`.
2. Add folders: `Entities`, `UseCases`, `Gateways`, `Controllers`, `Presenters`, `RequestModels`, `ResponseModels`, `ViewModels`.
3. Create `tests/SocialApp.<ComponentName>.Tests/`.
4. Add architecture test assertions to `ArchitectureRulesTests.cs` covering forbidden references for the new component.
5. Register the component's gateway bindings in `SocialApp.Api/Program.cs`.
