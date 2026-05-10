# Blazor API Cosmos Terraform Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Build the smallest end-to-end SocialApp slice with a Blazor WebAssembly SPA, ASP.NET Core Web API, MongoDB-compatible Cosmos persistence, and Terraform-managed Azure infrastructure.

**Architecture:** Keep `SocialApp.User` and `SocialApp.Post` as framework-free business components. Add outer delivery and infrastructure projects that depend inward, with architecture tests preventing framework and database leakage into business components.

**Tech Stack:** .NET 10, ASP.NET Core Minimal APIs, Blazor WebAssembly, MongoDB.Driver, Azure Static Web Apps, Azure Container Apps, Azure Cosmos DB for MongoDB API, Terraform AzureRM provider.

---

### Task 1: Protect Dependency Direction

**Files:**
- Modify: `tests/SocialApp.Architecture.Tests/ArchitectureRulesTests.cs`

**Step 1: Write failing architecture tests**

Add tests that expect:

- business components do not reference `SocialApp.Api`, `SocialApp.Web`, or `SocialApp.Infrastructure.CosmosMongo`
- business components do not reference `MongoDB.Driver`, `Microsoft.AspNetCore`, or Blazor assemblies
- outer projects may reference business components, but business components remain independent

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/SocialApp.Architecture.Tests/SocialApp.Architecture.Tests.csproj`

Expected: fail while new project assemblies do not exist or are not loadable from tests.

**Step 3: Add project loading defensively**

Update the tests so absent outer assemblies do not fail the current baseline, then tighten them after projects are added.

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/SocialApp.Architecture.Tests/SocialApp.Architecture.Tests.csproj`

Expected: pass.

### Task 2: Add Infrastructure Project

**Files:**
- Create: `src/SocialApp.Infrastructure.CosmosMongo/SocialApp.Infrastructure.CosmosMongo.csproj`
- Create: `src/SocialApp.Infrastructure.CosmosMongo/Options/CosmosMongoOptions.cs`
- Create: `src/SocialApp.Infrastructure.CosmosMongo/Documents/*.cs`
- Create: `src/SocialApp.Infrastructure.CosmosMongo/Gateways/*.cs`
- Create: `src/SocialApp.Infrastructure.CosmosMongo/DependencyInjection.cs`
- Create: `tests/SocialApp.Infrastructure.CosmosMongo.Tests/SocialApp.Infrastructure.CosmosMongo.Tests.csproj`
- Create: `tests/SocialApp.Infrastructure.CosmosMongo.Tests/CosmosMongoMappingTests.cs`

**Step 1: Write failing mapping tests**

Test that user and post documents round-trip through infrastructure mappers without losing handle, email, content, ids, likes, deleted state, or timestamps needed by the slice.

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/SocialApp.Infrastructure.CosmosMongo.Tests/SocialApp.Infrastructure.CosmosMongo.Tests.csproj`

Expected: fail because the project and mappers do not exist.

**Step 3: Implement infrastructure adapters**

Use `MongoDB.Driver`. Implement `IUserGateway`, `ISessionGateway`, and `IPostGateway`. Keep persistence documents internal to infrastructure. Use reflection only where existing entities do not expose persistence constructors.

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/SocialApp.Infrastructure.CosmosMongo.Tests/SocialApp.Infrastructure.CosmosMongo.Tests.csproj`

Expected: pass.

### Task 3: Add Web API Host

**Files:**
- Create: `src/SocialApp.Api/SocialApp.Api.csproj`
- Create: `src/SocialApp.Api/Program.cs`
- Create: `src/SocialApp.Api/Endpoints/*.cs`
- Create: `src/SocialApp.Api/Contracts/*.cs`
- Create: `tests/SocialApp.Api.Tests/SocialApp.Api.Tests.csproj`
- Create: `tests/SocialApp.Api.Tests/SocialAppApiSliceTests.cs`

**Step 1: Write failing API slice tests**

Use `WebApplicationFactory` with in-memory gateways to test:

- `POST /api/accounts` creates an account and returns a session token
- `POST /api/sessions` logs in
- `POST /api/posts` creates a post
- `GET /api/posts/recent` returns created posts

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/SocialApp.Api.Tests/SocialApp.Api.Tests.csproj`

Expected: fail because API project does not exist.

**Step 3: Implement endpoints**

Compose component controllers, interactors, presenters, and gateways in the API edge. Keep route DTOs in `SocialApp.Api`.

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/SocialApp.Api.Tests/SocialApp.Api.Tests.csproj`

Expected: pass.

### Task 4: Add Blazor WebAssembly SPA

**Files:**
- Create: `src/SocialApp.Web/SocialApp.Web.csproj`
- Create: `src/SocialApp.Web/Program.cs`
- Create: `src/SocialApp.Web/App.razor`
- Create: `src/SocialApp.Web/Routes.razor`
- Create: `src/SocialApp.Web/Layout/MainLayout.razor`
- Create: `src/SocialApp.Web/Pages/Home.razor`
- Create: `src/SocialApp.Web/Services/SocialAppApiClient.cs`
- Create: `src/SocialApp.Web/wwwroot/index.html`
- Create: `src/SocialApp.Web/wwwroot/css/app.css`

**Step 1: Create UI client contracts**

Define typed request and response records for the four API calls.

**Step 2: Implement the SPA**

Build one usable screen for account creation, login, post creation, and recent posts.

**Step 3: Build the SPA**

Run: `dotnet build src/SocialApp.Web/SocialApp.Web.csproj`

Expected: pass.

### Task 5: Add Terraform Infrastructure

**Files:**
- Create: `infra/terraform/main.tf`
- Create: `infra/terraform/variables.tf`
- Create: `infra/terraform/outputs.tf`
- Create: `infra/terraform/versions.tf`
- Create: `infra/terraform/README.md`

**Step 1: Add Terraform stack**

Provision resource group, Log Analytics, Container Apps environment, Container App, Static Web App, Cosmos DB Mongo account, database, and collections.

**Step 2: Validate formatting**

Run: `terraform -chdir=infra/terraform fmt -check`

Expected: pass when Terraform is installed.

### Task 6: Wire Solution And Documentation

**Files:**
- Modify: `SocialApp.sln`
- Modify: `README.md`
- Modify: `docs/Architecture.md`

**Step 1: Add projects to solution**

Run `dotnet sln add` for new projects.

**Step 2: Document run commands**

Add local API, SPA, test, and Terraform notes.

**Step 3: Verify all tests**

Run: `dotnet test SocialApp.sln`

Expected: pass.
