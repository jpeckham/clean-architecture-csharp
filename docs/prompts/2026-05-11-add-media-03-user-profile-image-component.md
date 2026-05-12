# Prompt: Add User Profile Image Component Use Cases

## Objective

Add user-owned profile image use cases so a caller can reserve a profile image upload, complete it, remove it, and view profile image metadata with a user profile.

## Prerequisite

Complete `docs/prompts/2026-05-11-add-media-01-domain-metadata.md`.

## Requirements

- Keep profile image behavior inside `SocialApp.User`.
- Add a user-owned storage/upload gateway port.
- Do not add a real storage adapter in this phase.
- Use in-memory test doubles in component tests.
- Existing account/session/view-user behavior must continue to work.

## Suggested Scope

Modify:
- `src/SocialApp.User/Gateways/UserGateways.cs`
- `src/SocialApp.User/RequestModels/UserRequests.cs`
- `src/SocialApp.User/ResponseModels/UserResponses.cs`
- `src/SocialApp.User/ViewModels/UserViewModels.cs`
- `src/SocialApp.User/Presenters/UserPresenters.cs`
- `src/SocialApp.User/UseCases/UserInteractors.cs`
- `src/SocialApp.User/UseCases/UserBoundaries.cs`
- `src/SocialApp.User/Controllers/UserControllers.cs`
- `tests/SocialApp.User.Tests/UserComponentTests.cs`

## Behavior

- Add a user-owned gateway such as `IProfileImageStorageGateway`.
- Add request/response models for beginning a profile image upload session.
- Add request/response models for completing a profile image upload.
- Add a remove profile image use case.
- Enforce image-only content types for profile images.
- Enforce ownership: only the current user can complete/remove their own profile image.
- Enrich user view models/responses with nullable profile image metadata.

## Out Of Scope

- HTTP endpoints.
- Persistence mapping.
- Real storage implementation.
- Blazor UI profile picker.
- Azure deployment.

## Verification

- Run `dotnet test tests/SocialApp.User.Tests/SocialApp.User.Tests.csproj --no-restore`.
- Run `dotnet test tests/SocialApp.Architecture.Tests/SocialApp.Architecture.Tests.csproj --no-restore`.

