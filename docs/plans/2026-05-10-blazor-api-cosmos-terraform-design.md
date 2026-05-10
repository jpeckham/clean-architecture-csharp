# Blazor API Cosmos Terraform Design

## Goal

Add the smallest useful end-to-end product slice around the existing Clean Architecture components: a Blazor WebAssembly SPA calls a C# Web API, the API drives the existing user and post use cases, MongoDB-compatible persistence stores the slice data in Azure Cosmos DB, and Terraform provisions the Azure hosting.

## Architectural Boundary

The existing `SocialApp.User` and `SocialApp.Post` projects remain the business components. They continue to own entities, use cases, boundaries, request models, response models, presenters, controllers, and gateway interfaces. They must not reference ASP.NET Core, Blazor, MongoDB, Cosmos DB, Terraform, or Azure hosting packages.

New outer projects depend inward:

```text
SocialApp.Web                  -> API over HTTP only
SocialApp.Api                  -> SocialApp.User, SocialApp.Post, SocialApp.Infrastructure.CosmosMongo
SocialApp.Infrastructure.CosmosMongo -> SocialApp.User, SocialApp.Post, MongoDB driver
SocialApp.User                 -> no SocialApp.* references
SocialApp.Post                 -> no SocialApp.* references
```

This keeps frameworks, UI, databases, and cloud infrastructure as details outside the use cases.

## Smallest Vertical Slice

The first slice supports:

- create account
- log in
- create post
- view recent posts

The Web API exposes only these workflows. Other existing use cases stay available in the component projects but are not wired into the first host.

## API Design

`SocialApp.Api` is an ASP.NET Core Web API composition root. Its endpoints translate HTTP DTOs into component controller calls, compose interactors with gateway implementations and presenters, then return presenter view models.

Endpoints:

- `POST /api/accounts`
- `POST /api/sessions`
- `POST /api/posts`
- `GET /api/posts/recent?readerHandle=@handle&limit=20`

The API owns HTTP concerns: status codes, request validation at the transport edge, CORS, configuration, health checks, and dependency injection. It does not put web attributes or framework dependencies into business components.

## Web Design

`SocialApp.Web` is a Blazor WebAssembly SPA hosted as static content. It uses typed HTTP client services for the four API operations and keeps UI state local to the slice. The first screen contains the working product flow rather than a landing page.

The browser does not reference business component projects. That avoids moving use-case logic into the client and keeps the server as the system boundary for persistence.

## Persistence Design

`SocialApp.Infrastructure.CosmosMongo` implements only the gateway interfaces required by the slice:

- `IUserGateway`
- `ISessionGateway`
- `IPostGateway`

It uses the MongoDB driver against Azure Cosmos DB for MongoDB API. Documents are persistence models owned by the infrastructure project. Mapping code converts between those documents and component entities.

For the first slice, session tokens can be stored in a sessions collection. Password reset and search-specific gateways are not implemented until their use cases are hosted.

## Azure Infrastructure

Terraform provisions:

- resource group
- Log Analytics workspace
- Azure Container Apps environment
- Azure Container App for `SocialApp.Api`
- Azure Static Web App for `SocialApp.Web`
- Azure Cosmos DB account using the MongoDB API
- Mongo database and collections for users, sessions, posts, follows, and blocks

The container image is an input variable so CI/CD can build and push images without Terraform rebuilding application artifacts. Cosmos connection details are exposed to Container Apps as secrets and environment variables.

## Testing

Tests protect both behavior and architecture:

- API slice tests prove endpoints call the component use cases and return useful responses.
- Infrastructure tests cover document mapping and Mongo gateway behavior where it can be tested without live Azure.
- Architecture tests enforce inward dependency direction and keep business components free of framework/database references.
- Live Cosmos integration tests are optional and configuration-gated.

## Error Handling

Component presenters continue to represent use-case outcomes. The API maps unsuccessful view models to suitable HTTP responses for the slice. Unexpected exceptions are handled by ASP.NET Core error handling and logged by the host.

## Out Of Scope

The first slice does not add authentication middleware, JWT issuing, password reset, search, likes, replies, reposts, deletes, follow/block UI, CI/CD pipelines, or production DNS. Those are later slices.
