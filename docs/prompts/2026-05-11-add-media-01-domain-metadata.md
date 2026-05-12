# Prompt: Add Media Domain Metadata

## Objective

Add domain-only media metadata to the owning components without adding storage, API endpoints, or persistence changes yet.

## Requirements

- Preserve component-first Clean Architecture.
- Add profile image metadata to `SocialApp.User`.
- Add post media item metadata to `SocialApp.Post`.
- Do not introduce a shared `Media` project or cross-component domain dependency.
- Store only metadata and storage references, never bytes.
- Keep existing post behavior working.

## Suggested Scope

Modify:
- `src/SocialApp.User/Entities/UserAccount.cs`
- `src/SocialApp.Post/Entities/SocialPost.cs`

Create if useful:
- `src/SocialApp.User/Entities/ProfileImage.cs`
- `src/SocialApp.Post/Entities/PostMediaItem.cs`

Add tests in:
- `tests/SocialApp.User.Tests/UserComponentTests.cs`
- `tests/SocialApp.Post.Tests/PostComponentTests.cs`

## Domain Rules

- `UserAccount` has optional `ProfileImage`.
- `UserAccount.SetProfileImage(...)` replaces the current image.
- `UserAccount.RemoveProfileImage()` clears the current image.
- `ProfileImage` contains `AssetId`, `StorageKey`, `ContentType`, `ByteLength`, optional dimensions, and `UploadedAt`.
- `SocialPost` exposes a read-only media collection.
- `PostMediaItem` contains `AssetId`, `Kind`, `StorageKey`, `ContentType`, `ByteLength`, optional dimensions, optional duration, `SortOrder`, optional `ThumbnailKey`, and optional `AltText`.
- A post is valid when it has non-blank text or at least one media item.
- Starter media policy: up to 4 images or 1 video; do not allow mixing video and images.
- Preserve the existing 280-character text limit when text is present.

## Out Of Scope

- Upload sessions.
- HTTP contracts.
- Mongo/Cosmos documents.
- File or blob storage.
- Blazor UI.

## Verification

- Run `dotnet test tests/SocialApp.User.Tests/SocialApp.User.Tests.csproj --no-restore`.
- Run `dotnet test tests/SocialApp.Post.Tests/SocialApp.Post.Tests.csproj --no-restore`.
- Run `dotnet test tests/SocialApp.Architecture.Tests/SocialApp.Architecture.Tests.csproj --no-restore`.

