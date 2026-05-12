# Prompt: Add Media Upload And Rendering To Blazor Web UI

## Objective

Add Blazor UI support for profile image upload, post media upload, media previews, and rendering media in feed/search/profile views.

## Prerequisites

Complete:
- `docs/prompts/2026-05-11-add-media-04-api-contracts-and-endpoints.md`
- `docs/prompts/2026-05-11-add-media-06-local-filesystem-storage.md`

## Requirements

- Use the existing `SocialAppApiClient` pattern.
- Preserve existing text-only posting flow.
- Make upload progress and failures visible.
- Keep the UI practical and dense, matching the current app rather than adding a marketing-style page.
- Render images and video using actual media URLs returned by the API.

## Suggested Scope

Modify:
- `src/SocialApp.Web/Services/SocialAppApiClient.cs`
- `src/SocialApp.Web/Pages/Feed.razor`
- `src/SocialApp.Web/Pages/UserProfile.razor`
- `src/SocialApp.Web/wwwroot/css/app.css`

Create if useful:
- small Razor components for media preview grids or upload controls under `src/SocialApp.Web/`

## Behavior

- Feed composer allows selecting up to 4 images or 1 video.
- Feed composer can create a media-only post after upload completion.
- Selected media shows a local preview before submit.
- Uploaded media renders in feed/search results.
- Profile page shows the user's profile image when present.
- Current user can upload or remove their own profile image.
- Errors from rejected content type, size, failed upload, or incomplete asset are shown near the relevant control.

## Out Of Scope

- Azure Blob direct browser upload.
- Front Door/CDN.
- Thumbnail generation.
- Advanced video transcoding.

## Verification

- Run `dotnet build SocialApp.sln --no-restore`.
- Run `dotnet test SocialApp.sln --no-restore`.
- Start the app locally and manually verify:
  - text-only post still works
  - image post works
  - media-only post works
  - profile image upload and removal work
  - feed/profile reload still shows persisted media metadata

