# Prompt: Replace Media Upload Provider Probing With Explicit Resolution

## Objective

Refactor media upload storage selection so `SocialAppSliceEndpoints` no longer probes multiple gateways by trying one implementation and catching failures.

Current finding:

- `src/SocialApp.Api/Endpoints/SocialAppSliceEndpoints.cs` mixes endpoint registration, transport handling, auth extraction, exception mapping, multipart parsing, and media upload provider probing.
- Media upload selection should be explicit and configuration-driven.

## Requirements

- Preserve current local filesystem and Azure Blob media storage behavior.
- Keep storage implementations outside the User and Post business rules.
- Avoid exception-based provider selection.
- Keep Minimal APIs if they remain the repository's current API style.
- Keep appsettings and Docker Compose behavior aligned.

## Suggested Scope

- Introduce an explicit media upload gateway resolver, selector, or provider abstraction in the API/infrastructure boundary.
- Select the storage provider from configuration.
- If the target framework supports it cleanly, consider keyed services for provider registration.
- Move upload-provider selection out of `StoreMediaUpload`.
- Keep endpoint handlers thin: parse the HTTP request, call the selected application-facing dependency, map the result.
- Add focused tests for provider selection and failed/missing provider configuration.
- Update appsettings or Docker-related configuration only if needed.

## Verification

- Run focused API and infrastructure tests for media upload.
- Run `dotnet test SocialApp.sln`.
- Run `docker compose config`.
- Run `docker compose build`.
- Because this touches media upload behavior, run `docker compose up -d` and smoke test uploading media through API `http://localhost:8080` and Web `http://localhost:8081`.

