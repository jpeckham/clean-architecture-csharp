# Visual Studio Docker Compose Mongo Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add a Visual Studio Docker Compose project that builds and runs the API, Blazor WebAssembly frontend, and persisted local MongoDB together.

**Architecture:** Keep persistence as an outer infrastructure detail. The Docker Compose path supplies `CosmosMongo__ConnectionString`, causing the existing API composition root to use `SocialApp.Infrastructure.CosmosMongo`; non-Docker runs can still fall back to in-memory gateways.

**Tech Stack:** Visual Studio Docker Compose `.dcproj`, Docker Compose, MongoDB official image, ASP.NET Core container image, nginx static hosting for Blazor WebAssembly.

---

### Task 1: Add Compose Executable Spec

**Files:**
- Create: `docker-compose.yml`
- Create: `docker-compose.override.yml`
- Create: `docker-compose.dcproj`
- Create: `.dockerignore`
- Modify: `SocialApp.sln`

**Step 1: Verify missing compose project fails**

Run: `docker compose -f docker-compose.yml -f docker-compose.override.yml config`

Expected: FAIL because `docker-compose.yml` does not exist.

**Step 2: Add the compose files**

Create a Visual Studio Docker Compose project with API, web, and Mongo services. Use a named volume for MongoDB data.

**Step 3: Validate compose config**

Run: `docker compose -f docker-compose.yml -f docker-compose.override.yml config`

Expected: PASS and include `socialapp.api`, `socialapp.web`, `mongo`, and `socialapp-mongo-data`.

### Task 2: Add Blazor Web Container

**Files:**
- Create: `src/SocialApp.Web/Dockerfile`
- Create: `src/SocialApp.Web/nginx/default.conf.template`
- Create: `src/SocialApp.Web/docker-entrypoint.d/10-write-appsettings.sh`

**Step 1: Build Web project**

Run: `dotnet build src/SocialApp.Web/SocialApp.Web.csproj`

Expected: PASS before containerization.

**Step 2: Add Dockerfile and nginx runtime config**

Publish the WebAssembly app with the .NET SDK, serve it from nginx, and write `appsettings.json` from `API_BASE_ADDRESS` at container startup.

**Step 3: Build compose services**

Run: `docker compose -f docker-compose.yml -f docker-compose.override.yml build`

Expected: PASS for `socialapp.api` and `socialapp.web`.

### Task 3: Document Default Local Workflow

**Files:**
- Modify: `README.md`

**Step 1: Update local run documentation**

Document Visual Studio startup through the Docker Compose project, host URLs, Mongo data persistence, and how to reset the Docker volume.

**Step 2: Build solution**

Run: `dotnet build SocialApp.sln`

Expected: PASS.

**Step 3: Final verification**

Run:

```powershell
docker compose -f docker-compose.yml -f docker-compose.override.yml config
dotnet build SocialApp.sln
docker compose -f docker-compose.yml -f docker-compose.override.yml build
```

Expected: all commands pass.

