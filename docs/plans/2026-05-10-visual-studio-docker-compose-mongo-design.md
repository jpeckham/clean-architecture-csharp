# Visual Studio Docker Compose Mongo Design

## Goal

Make persisted local MongoDB the default development path when running the SocialApp vertical slice from Visual Studio.

## Approach

Add a Visual Studio Docker Compose project to the solution. The compose project builds and runs the API, the Blazor WebAssembly frontend, and a local MongoDB container with a named Docker volume. The existing API composition root already switches to `SocialApp.Infrastructure.CosmosMongo` when `CosmosMongo:ConnectionString` is present, so the default Docker path can use Mongo without changing the business components.

## Runtime Shape

- `mongo` uses the official MongoDB image and persists data in `socialapp-mongo-data`.
- `socialapp.api` builds from `src/SocialApp.Api/Dockerfile`, listens on container port `8080`, maps to host port `8080`, and connects to `mongodb://mongo:27017`.
- `socialapp.web` builds from a new `src/SocialApp.Web/Dockerfile`, serves the published WebAssembly app through nginx, maps to host port `8081`, and writes runtime `appsettings.json` so the browser calls `http://localhost:8080`.

Because Blazor WebAssembly HTTP calls execute in the user's browser, the SPA must use the host-mapped API URL instead of Docker's internal service DNS name.

## Boundaries

The in-memory gateways remain available when the API runs without a Mongo connection string. This preserves lightweight component tests and non-Docker startup. Docker Compose becomes the documented default local workflow for persisted longer-running testing.

## Verification

- `docker compose -f docker-compose.yml -f docker-compose.override.yml config`
- `dotnet build SocialApp.sln`
- `docker compose -f docker-compose.yml -f docker-compose.override.yml build`

