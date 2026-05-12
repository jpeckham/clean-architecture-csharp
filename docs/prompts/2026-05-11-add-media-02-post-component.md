# Prompt: Add Post Media Component Use Cases

## Objective

Add post-owned media use cases so a caller can reserve media uploads, complete uploaded media, and create posts with completed media asset ids.

## Prerequisite

Complete `docs/prompts/2026-05-11-add-media-01-domain-metadata.md`.

## Requirements

- Keep all post media behavior inside `SocialApp.Post`.
- Add component-owned storage/upload gateway ports.
- Do not add real filesystem, S3, or Azure Blob implementation in this phase.
- Use in-memory test doubles in component tests.
- Existing text-only post creation must continue to work.

## Suggested Scope

Modify:
- `src/SocialApp.Post/Gateways/PostGateways.cs`
- `src/SocialApp.Post/RequestModels/PostRequests.cs`
- `src/SocialApp.Post/ResponseModels/PostResponses.cs`
- `src/SocialApp.Post/ViewModels/PostViewModels.cs`
- `src/SocialApp.Post/Presenters/PostPresenters.cs`
- `src/SocialApp.Post/UseCases/PostInteractors.cs`
- `src/SocialApp.Post/UseCases/PostBoundaries.cs`
- `src/SocialApp.Post/Controllers/PostControllers.cs`
- `tests/SocialApp.Post.Tests/PostComponentTests.cs`

## Behavior

- Add a post-owned gateway such as `IPostMediaStorageGateway`.
- Add request/response models for beginning media upload sessions.
- Add request/response models for completing an uploaded post media asset.
- Add `MediaAssetIds` to post creation.
- Validate that media asset ids are completed and owned by the posting user.
- Return media metadata in post summary/detail view models.
- Feed/search responses should return an empty media list for text-only posts.

## Suggested Use Cases

- `BeginPostMediaUpload`
- `CompletePostMediaUpload`
- media-aware `CreatePost`

## Out Of Scope

- HTTP endpoints.
- Persistence mapping.
- Real storage implementation.
- Web UI upload controls.
- Azure deployment.

## Verification

- Run `dotnet test tests/SocialApp.Post.Tests/SocialApp.Post.Tests.csproj --no-restore`.
- Run `dotnet test tests/SocialApp.Architecture.Tests/SocialApp.Architecture.Tests.csproj --no-restore`.

