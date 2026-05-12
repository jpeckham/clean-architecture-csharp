# Prompt: Add Local Filesystem Media Storage

## Objective

Add a local development storage adapter that supports upload sessions and media reads using a filesystem-backed root path.

## Prerequisites

Complete:
- `docs/prompts/2026-05-11-add-media-02-post-component.md`
- `docs/prompts/2026-05-11-add-media-03-user-profile-image-component.md`
- `docs/prompts/2026-05-11-add-media-04-api-contracts-and-endpoints.md`

## Requirements

- Keep storage implementation in an infrastructure project.
- Register storage adapters from API composition/DI.
- Use a Docker named volume for local media.
- Avoid Azure Blob SDK work in this phase.
- Keep generated storage keys unguessable and scoped by owner/use.

## Suggested Scope

Create if useful:
- `src/SocialApp.Infrastructure.LocalStorage/`
- `src/SocialApp.Infrastructure.LocalStorage/SocialApp.Infrastructure.LocalStorage.csproj`
- `src/SocialApp.Infrastructure.LocalStorage/DependencyInjection.cs`
- `src/SocialApp.Infrastructure.LocalStorage/Options/LocalMediaStorageOptions.cs`
- `src/SocialApp.Infrastructure.LocalStorage/Gateways/FileSystemProfileImageStorageGateway.cs`
- `src/SocialApp.Infrastructure.LocalStorage/Gateways/FileSystemPostMediaStorageGateway.cs`

Modify:
- `SocialApp.sln`
- `src/SocialApp.Api/SocialApp.Api.csproj`
- `src/SocialApp.Api/Program.cs`
- `src/SocialApp.Api/appsettings.Development.json`
- `docker-compose.yml`
- `docker-compose.override.yml` if needed
- `tests/SocialApp.Api.Tests/SocialAppApiSliceTests.cs` or add infrastructure-specific tests

## Behavior

- `Media__Provider=FileSystem` registers the local adapter.
- Upload session returns an API-local upload URL or instruction that lets local clients PUT bytes to the API.
- Complete upload verifies the file exists and returns metadata to the owning component.
- Read URLs resolve through API endpoints or static file middleware under a controlled media route.
- Docker Compose mounts a named volume such as `socialapp-media-data`.

## Out Of Scope

- Azure Blob Storage.
- Front Door/CDN.
- Thumbnail generation.
- Blazor visual polish beyond what is needed to prove upload APIs work.

## Verification

- Run `dotnet test tests/SocialApp.Api.Tests/SocialApp.Api.Tests.csproj --no-restore`.
- Run `dotnet test SocialApp.sln --no-restore`.
- Run `docker compose config` to verify the compose file is valid.

