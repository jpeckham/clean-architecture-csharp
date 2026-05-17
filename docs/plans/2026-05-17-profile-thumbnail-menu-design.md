# Profile Thumbnail Menu Design

## Goal

Replace page-level logout buttons on authenticated screens with a reusable profile thumbnail menu.

## Design

Use the existing `/users/{handle}` profile page as the account destination and keep current UI terminology: `Profile` and `Log Out`. Add a shared Blazor component in `src/SocialApp.Web/Components` that reads the active `AppSession`, fetches the current user's profile through `SocialAppApiClient.GetUserAsync`, renders the profile image when present, falls back to initials otherwise, and exposes a compact menu from the thumbnail button.

The menu will call `Navigation.NavigateTo($"/users/{handle}")` for `Profile` and call `Session.SignOut()` followed by `Navigation.NavigateTo("/")` for `Log Out`. Authenticated pages will use this component in their existing masthead action areas so the behavior is consistent without restructuring `MainLayout`.

## Testing

Add source-level web tests matching this repository's current `SocialApp.Web.Tests` style:

- Verify the shared component renders `Profile`, `Log Out`, uses `GetUserAsync`, and signs out through `Session.SignOut()`.
- Verify `Feed.razor` no longer owns a page-local `Log Out` button or `SignOut` method and uses the shared component.
- Verify `PostDetails.razor` and `UserProfile.razor` also use the shared component.

## Verification

Run `dotnet test`, `docker compose config`, and `docker compose build`. Because this is a user-visible web flow, run Docker Compose and smoke test the web app through `http://localhost:8081` if the build succeeds.
