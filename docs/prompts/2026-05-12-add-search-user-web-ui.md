# Prompt: Add Search User Use Case To Web UI

## Objective

Add web UI coverage for the existing `SearchUser` use case.

The domain use case exists in `SocialApp.User`, but it is not currently mapped through the API or web UI:
- `SearchUserInteractor`
- `SearchUserController`
- `SearchUserPresenter`

## Requirements

- Preserve the component-first Clean Architecture structure.
- Add an API endpoint for searching users by query.
- Add web client support in `SocialAppApiClient`.
- Add a search UI that returns handle and display name.
- Keep the UI compact and useful from the feed or a dedicated people/search page.
- Empty results should render a clear empty state.
- Do not introduce cross-component dependencies from `User` to `Post`.

## Suggested Scope

- Add a user search HTTP endpoint, for example `GET /api/users/search?query=...`.
- Add response contracts as needed.
- Add a client method and result records in `SocialAppApiClient`.
- Add a Blazor page or feed-level search area.
- Add tests covering query matching and empty results.

## Verification

- Run `dotnet test SocialApp.sln`.
- Manually verify searching by handle/display name returns expected users.
