# Deep Linking And Share Post Design

## Goal

Add stable URLs for individual posts so users can open one post directly and share a copyable deep link.

## Recommended Approach

Use a backend display-one-post use case and expose it through `GET /api/posts/{postId}`. The endpoint returns the same post summary shape used by recent posts, search, and profile posts, including media, content segments, quoted posts, likes, and repost state for the authenticated reader.

Add a Blazor `/posts/{postId:guid}` page that loads one post from the API and renders the existing post card experience. Feed cards link to that route, and a Share action prompts with the absolute browser URL for the selected post.

## Behavior

Deleted or missing posts return `404 Not Found` from the API and render a not-found state on the web page. Authenticated access follows the existing post read behavior: a valid bearer token is required so reader-specific like and repost state can be projected.

## Components

- `SocialApp.Post`: add display-one request, response, boundary, presenter, controller, and interactor.
- `SocialApp.Api`: map `GET /api/posts/{postId}` and reuse the existing HTTP post summary mapping.
- `SocialApp.Web`: add API client method, `/posts/{postId}` page, and a shared post card component with callbacks for like, repost, delete, and share.
- Tests: cover use case behavior, API routing/status, and route registration.

## Testing

Run focused post/API/web tests first, then the repository test suite. Because this touches the API/Web user flow, verify Docker Compose with `docker compose config`, `docker compose build`, and a compose smoke test through `http://localhost:8080` and `http://localhost:8081`.
