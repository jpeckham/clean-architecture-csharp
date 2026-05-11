# Delete Own Posts UI Design

## Goal

Add the hosted path that lets a signed-in user delete posts they authored from the feed UI.

## Architecture

`SocialApp.Post` remains the owner of the business rule. The existing `DeletePost` use case calls `SocialPost.DeleteBy(requesterHandle)`, which permits deletion only when the requester handle matches the post author.

The web UI and API stay as delivery mechanisms. The API authenticates the bearer token, resolves the requester from the session gateway, invokes the Post component controller/interactor, and returns the presenter view model. Cosmos Mongo persists the changed deleted state through the component-owned `IPostGateway`.

## UI Behavior

The feed renders a `Delete` button only for posts whose `AuthorHandle` matches the signed-in session handle, using case-insensitive comparison. Clicking `Delete` opens a browser confirmation prompt. If confirmed, the UI calls the delete endpoint, refreshes the feed, and displays the presenter message.

## API Behavior

Add `DELETE /api/posts/{postId}`. Requests without a valid bearer token return `401 Unauthorized`. Missing posts return a failed result. Attempts to delete another user's post return a failed result without bypassing the entity rule.

## Testing

Add tests before implementation:

- Post component test proves unauthorized deletion fails and leaves the post visible.
- Infrastructure test proves deletion is persisted through the gateway.
- API slice test proves only the creator can delete through the hosted endpoint.
