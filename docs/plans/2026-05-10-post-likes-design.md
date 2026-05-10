# Post Likes Design

## Goal

Users can add their own like to any post and remove only their own like from a post they have liked. The feed displays a heart next to each post, filled when the current reader has liked it and unfilled otherwise, with the total like count directly to the right.

## Approach

Likes are authenticated post subresources. The API will expose `POST /api/posts/{postId}/likes` to add the current session user's like and `DELETE /api/posts/{postId}/likes` to remove the current session user's like. Neither endpoint accepts a user handle from the caller; the handle comes from the bearer token session.

Post summaries will include both the total like count and whether the current reader has liked the post. The post scroll use case already receives `readerHandle`, so it can compute this without coupling the post component to the user component.

## Rules

- Any authenticated user can like any existing post.
- A user cannot like a post on behalf of another user.
- A user cannot remove another user's like.
- Removing a like succeeds only if the current user has already liked the post.
- Re-liking a post already liked by the same user remains idempotent because likes are stored as a case-insensitive set of handles.
- Missing or invalid bearer tokens return `401`.
- Missing posts return `404`.
- Removing a non-existent current-user like returns `400`.

## Implementation Areas

- `SocialApp.Post`: persist add-like changes, reject delete-like when the current handle has not liked the post, and include `LikedByCurrentReader` in post summaries.
- `SocialApp.Api`: add authenticated like and unlike endpoints.
- `SocialApp.Web`: add API client methods and render/toggle the heart in the feed.
- Tests: add API and component coverage before production changes.
