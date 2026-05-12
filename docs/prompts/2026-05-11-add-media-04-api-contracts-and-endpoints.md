# Prompt: Add Media API Contracts And Endpoints

## Objective

Expose the post media and profile image component use cases over the existing Minimal API surface.

## Prerequisites

Complete:
- `docs/prompts/2026-05-11-add-media-02-post-component.md`
- `docs/prompts/2026-05-11-add-media-03-user-profile-image-component.md`

## Requirements

- Keep HTTP contracts in `SocialApp.Api`.
- Keep business validation inside `SocialApp.User` and `SocialApp.Post`.
- Preserve backward compatibility for existing text-only post creation.
- New media fields in responses must be additive.

## Suggested Scope

Modify:
- `src/SocialApp.Api/Contracts/PostContracts.cs`
- `src/SocialApp.Api/Contracts/AccountContracts.cs`
- `src/SocialApp.Api/Endpoints/SocialAppSliceEndpoints.cs`
- `src/SocialApp.Api/Endpoints/UserProfilePostGatewayAdapter.cs` if feed/user projection needs media
- `src/SocialApp.Web/Services/SocialAppApiClient.cs` only for typed client methods and DTOs needed by tests
- `tests/SocialApp.Api.Tests/SocialAppApiSliceTests.cs`

## Endpoint Shape

- `POST /api/posts/media/upload-sessions`
- `POST /api/posts/media/{assetId}/complete`
- Existing `POST /api/posts` accepts optional `mediaAssetIds`.
- Existing feed/search post responses include `media`.
- `POST /api/users/me/profile-image/upload-sessions`
- `POST /api/users/me/profile-image/complete`
- `DELETE /api/users/me/profile-image`
- Existing view-user response includes nullable `profileImage`.

## Test Coverage

- Begin post media upload session requires authentication.
- Complete post media upload requires authentication and ownership.
- Create text-only post still works.
- Create media-only post works after asset completion.
- Create post with uncompleted or foreign asset fails.
- Begin profile image upload rejects non-image content type.
- Complete profile image updates `GET /api/users/{handle}`.
- Delete profile image clears `GET /api/users/{handle}`.

## Out Of Scope

- Mongo/Cosmos persistence.
- Real storage implementation.
- UI rendering.
- Terraform.

## Verification

- Run `dotnet test tests/SocialApp.Api.Tests/SocialApp.Api.Tests.csproj --no-restore`.
- Run `dotnet test SocialApp.sln --no-restore`.

