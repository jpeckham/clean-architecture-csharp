# Prompt: Add Search Posts Use Case To Web UI

## Objective

Add web UI coverage for the existing `SearchPosts` use case.

The domain use case exists in `SocialApp.Post`, but it is not currently mapped through the API or web UI:
- `SearchPostsInteractor`
- `SearchPostsController`
- `SearchPostsPresenter`
- `IPostSearchGateway`

## Requirements

- Preserve the component-first Clean Architecture structure.
- Add an API endpoint for searching posts by text query.
- Add web client support in `SocialAppApiClient`.
- Add a web UI search experience that renders post summaries consistently with the feed.
- Preserve like/repost/delete behavior where feasible for returned search results.
- Empty results should render a clear empty state.

## Suggested Scope

- Add an endpoint like `GET /api/posts/search?query=...&readerHandle=...`.
- Reuse the existing post summary response shape where possible.
- Add a client method in `SocialAppApiClient`.
- Add a search box to `/feed` or create a dedicated post search page.
- Add tests for API mapping and search result rendering.

## Verification

- Run `dotnet test SocialApp.sln`.
- Manually verify searching post content returns matching posts.
