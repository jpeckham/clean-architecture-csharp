# Repost UI Design

## Goal

Let signed-in users repost other people's posts from the feed, optionally add their own content, see repost counts, and remove their own repost from the original post action.

## Architecture

`SocialApp.Post` remains the owner of repost rules. Reposts stay as `SocialPost` records with `OriginalPostId` set, which lets delete and like behavior reuse the existing post paths. The feed projection is enriched with repost metadata and quoted original post data so delivery layers do not infer domain state.

## Behavior

A user can repost only a post authored by someone else. A user can have only one active repost for a given original post. Reposting a repost targets the repost's original post, preventing nested quote chains.

The repost may include optional content. Feed cards for reposts render the quoted original post with the original author's handle first, then render the repost author's content after the quote when provided.

The repost action uses a two-arrow/recycle-style icon. Each post shows its active repost count to the right of the icon. The icon is highlighted when the signed-in user has reposted that post. Clicking a highlighted repost button asks for confirmation and deletes the current user's repost of that post.

## API And UI

Add `POST /api/posts/{postId}/reposts` with optional content and `DELETE /api/posts/{postId}/reposts/mine`. The Blazor feed calls these endpoints from each post card, refreshes the feed afterward, and leaves normal delete/like behavior operating on the actual displayed post id.

## Testing

Add component tests for self-repost rejection, duplicate active repost rejection, reposting a repost targeting the root original, count/current-user projection, and quote projection. Add API slice coverage for create/delete repost behavior. Build the UI after implementation.
