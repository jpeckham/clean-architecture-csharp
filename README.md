# SocialApp Clean Architecture Reference

This repository is a C# reference implementation of Clean Architecture using component-first decomposition. It uses a small microblogging-style domain because the behavior is familiar: users create accounts, log in, find other users, create posts, scroll feeds, like, reply, repost, and delete.

The top-level architecture screams the business capabilities:

```text
src/
  SocialApp.User/
  SocialApp.Post/
  SocialApp.Infrastructure.CosmosMongo/
  SocialApp.Infrastructure.AcsEmail/
  SocialApp.Api/
  SocialApp.Web/

tests/
  SocialApp.User.Tests/
  SocialApp.Post.Tests/
  SocialApp.Infrastructure.CosmosMongo.Tests/
  SocialApp.Api.Tests/
  SocialApp.Architecture.Tests/
```

The original business components remain host-free. The added host projects are outer details: the API and Blazor WebAssembly SPA depend inward, while the business components do not depend on ASP.NET, Blazor, MongoDB, Cosmos DB, or Terraform.

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
- Components do not reference the API, web frontend, or infrastructure projects.
- Entities do not depend on controllers, presenters, gateways, or interactors.
- Interactors depend on abstractions and entities, not frameworks.
- Controllers translate input and invoke input boundaries.
- Presenters implement output boundaries and build view models.
- `SocialApp.Web` calls the API over HTTP and does not reference business components directly.
- `SocialApp.Infrastructure.CosmosMongo` implements component-owned gateway interfaces using the MongoDB driver for Cosmos DB.
- `SocialApp.Infrastructure.AcsEmail` implements component-owned email delivery using Azure Communication Services Email.

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

## Running The Vertical Slice

The default local workflow is the Visual Studio Docker Compose project. Set `docker-compose` as the startup project in Visual Studio and run it. Visual Studio builds and starts:

- `socialapp.api` on `http://localhost:8080`
- `socialapp.web` on `http://localhost:8081`
- `mongo` on `localhost:27017`

The MongoDB container stores data in the named Docker volume `socialapp-mongo-data`, so local test data survives container restarts.

You can run the same setup from the command line:

```powershell
docker compose up --build
```

To reset the persisted local MongoDB data:

```powershell
docker compose down -v
```

The API container receives `CosmosMongo__ConnectionString=mongodb://mongo:27017` and `CosmosMongo__DatabaseName=socialapp`, so it uses `SocialApp.Infrastructure.CosmosMongo` by default in Docker.

For a lightweight non-Docker run, start the API:

```powershell
dotnet run --project src/SocialApp.Api/SocialApp.Api.csproj --launch-profile https
```

Start the Blazor WebAssembly SPA:

```powershell
dotnet run --project src/SocialApp.Web/SocialApp.Web.csproj --launch-profile https
```

The SPA reads `ApiBaseAddress` from `src/SocialApp.Web/wwwroot/appsettings.json`. By default it points at the API HTTPS launch profile.

Without `CosmosMongo:ConnectionString`, the API still uses in-memory gateways. Set `CosmosMongo__ConnectionString` and `CosmosMongo__DatabaseName` manually only when you are not using the Docker Compose project.

Without `AcsEmail:ConnectionString`, the API uses an in-memory email gateway. Set these values to send real out-of-band emails through Azure Communication Services:

```powershell
$env:AcsEmail__ConnectionString="<acs-connection-string>"
$env:AcsEmail__SenderAddress="donotreply@<verified-domain>"
$env:Web__PasswordResetBaseUrl="https://localhost:7278/reset-password"
```

## Auth Flow

The hosted slice uses out-of-band email flows:

- account creation stores a pending registration and sends an email verification code
- registration verification creates the user account
- login sends an email OTP when the browser/device is not remembered
- remembered devices can skip email OTP on later logins
- forgot password sends a one-time reset link that expires after 5 minutes
- reset tokens are consumed once and cannot be reused

## Azure Infrastructure

Azure production infrastructure is defined in `infrastructure/bicep` and deployed by `.github/workflows/production-deploy.yml`.

The first infrastructure deployment must be run from an authenticated local Azure CLI session because GitHub cannot create the Azure identity it needs before it can log in:

```powershell
az login --tenant 7f4e14a4-417c-496f-82d9-9fb7940c3d17
az account set --subscription 6d88cea2-aec5-4d58-88c4-4830a867b3cd

./infrastructure/scripts/deploy-infrastructure.ps1
./infrastructure/scripts/get-github-secrets.ps1
```

Store the printed values as GitHub `production` environment secrets:

```text
AZURE_CLIENT_ID=<client id for id-github-cleansocial-prod>
AZURE_TENANT_ID=7f4e14a4-417c-496f-82d9-9fb7940c3d17
AZURE_SUBSCRIPTION_ID=6d88cea2-aec5-4d58-88c4-4830a867b3cd
```

`AZURE_CLIENT_ID` is not a password. It identifies the user-assigned managed identity `id-github-cleansocial-prod`. The deployment is secured by GitHub OpenID Connect federation: Azure accepts a short-lived token from GitHub only when its claims match the federated credentials created by Bicep:

```text
issuer:  https://token.actions.githubusercontent.com
subject: repo:jpeckham/clean-architecture-csharp:ref:refs/heads/main
subject: repo:jpeckham/clean-architecture-csharp:environment:production
audience: api://AzureADTokenExchange
```

Another repository cannot use the same client ID to deploy because its GitHub OIDC token has a different `sub` claim. Any workflow in this repository that targets the `production` environment can request this identity, so the GitHub `production` environment should use required reviewers or deployment branch restrictions for additional release control.

The production workflow re-applies Bicep, builds and pushes the API container to Azure Container Registry, updates Azure Container Apps, builds the Blazor WebAssembly frontend, and deploys it to Azure Static Web Apps.

Build the API container from the repository root with:

```powershell
docker build -f src/SocialApp.Api/Dockerfile -t socialapp-api:local .
```
