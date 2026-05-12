# Prompt: Add Block User Posts Use Case To Web UI

## Objective

Add web UI coverage for the existing `BlockUserPosts` use case.

The domain use case exists in `SocialApp.Post`, but it is not currently mapped through the API or web UI:
- `BlockUserPostsInteractor`
- `BlockUserPostsController`
- `BlockUserPostsPresenter`

## Requirements

- Preserve the component-first Clean Architecture structure.
- Add an authenticated API endpoint for blocking another user's posts.
- Add web client support in `SocialAppApiClient`.
- Add a UI action to block a user from a profile, search result, or post author affordance.
- Use the logged-in session handle as the reader/blocking user server-side.
- Show success and failure messages in the UI.
- Do not allow the UI to block as an arbitrary handle supplied by the browser.

## Suggested Scope

- Add an endpoint like `POST /api/users/{handle}/block` or `POST /api/post-blocks`.
- Resolve the current user from the bearer token, matching existing post mutation endpoints.
- Add client methods in `SocialAppApiClient`.
- Add block buttons in the most natural existing or newly added user UI.
- Add API tests proving the session user is used as `ReaderHandle`.

## Verification

- Run `dotnet test SocialApp.sln`.
- Manually verify a logged-in user can block another user and sees a success message.
