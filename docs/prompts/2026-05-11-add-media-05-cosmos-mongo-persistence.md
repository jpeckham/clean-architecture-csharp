# Prompt: Persist Media Metadata In Cosmos Mongo

## Objective

Persist profile image and post media metadata in the existing Cosmos Mongo infrastructure without adding new Mongo collections unless staged upload metadata truly requires it.

## Prerequisites

Complete:
- `docs/prompts/2026-05-11-add-media-01-domain-metadata.md`
- `docs/prompts/2026-05-11-add-media-02-post-component.md`
- `docs/prompts/2026-05-11-add-media-03-user-profile-image-component.md`

## Requirements

- Keep persistence in `SocialApp.Infrastructure.CosmosMongo`.
- Treat media metadata as embedded documents owned by `UserDocument` and `PostDocument`.
- Existing documents without media fields must still rehydrate correctly.
- Do not store binary bytes in Mongo/Cosmos.

## Suggested Scope

Modify:
- `src/SocialApp.Infrastructure.CosmosMongo/Documents/UserDocument.cs`
- `src/SocialApp.Infrastructure.CosmosMongo/Documents/PostDocument.cs`
- `src/SocialApp.Infrastructure.CosmosMongo/Documents/CosmosMongoMappers.cs`
- `src/SocialApp.Infrastructure.CosmosMongo/Gateways/CosmosMongoUserGateway.cs`
- `src/SocialApp.Infrastructure.CosmosMongo/Gateways/CosmosMongoPostGateway.cs`
- `tests/SocialApp.Infrastructure.CosmosMongo.Tests/CosmosMongoMappingTests.cs`

## Behavior

- Add nullable embedded `ProfileImage` document to `UserDocument`.
- Add embedded `Media` array to `PostDocument`, defaulting to empty.
- Map all domain metadata fields both directions.
- Confirm older/null document shapes map to no profile image and empty post media.
- Preserve existing persistence behavior for text-only posts and users.

## Out Of Scope

- Upload-session storage implementation.
- Filesystem or Blob adapters.
- API endpoint changes.
- UI changes.

## Verification

- Run `dotnet test tests/SocialApp.Infrastructure.CosmosMongo.Tests/SocialApp.Infrastructure.CosmosMongo.Tests.csproj --no-restore`.
- Run `dotnet test SocialApp.sln --no-restore`.

