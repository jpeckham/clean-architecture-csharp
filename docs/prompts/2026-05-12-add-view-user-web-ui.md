# Prompt: Add View User Use Case To Web UI

## Objective

Add web UI coverage for the existing `ViewUser` use case.

The domain use case exists in `SocialApp.User`, but it is not currently mapped through the API or web UI:
- `ViewUserInteractor`
- `ViewUserController`
- `ViewUserPresenter`

## Requirements

- Preserve the component-first Clean Architecture structure.
- Add an API endpoint for viewing a user by handle.
- Add web client support in `SocialAppApiClient`.
- Add a profile/details UI that shows handle and display name.
- Handle missing users with a user-visible not-found state.
- Make user handles in search results or posts navigable to this profile UI where practical.

## Suggested Scope

- Add an endpoint like `GET /api/users/{handle}`.
- Add response contracts as needed.
- Add a client method and result records in `SocialAppApiClient`.
- Add a page such as `/users/{handle}`.
- Add API and UI/component tests for found and not-found states.

## Verification

- Run `dotnet test SocialApp.sln`.
- Manually verify a user profile can be opened from a known handle.
