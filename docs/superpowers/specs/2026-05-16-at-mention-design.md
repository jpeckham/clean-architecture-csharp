# @Mention Feature Design

**Date:** 2026-05-16

## Overview

When a user types `@handle` in post content, any handle that maps to a real account is stored as a validated mention on the post and rendered as a hyperlink to that user's profile in the feed. Handles that do not match a real account are left as plain text and excluded from the mentions array.

Mention search (finding all posts that mention a given handle) is explicitly out of scope; the `Mentions` field is stored as groundwork for that future use case.

---

## Section 1: Entity — `SocialPost`

`SocialPost` gains:

- `_mentions` — `HashSet<string>` (OrdinalIgnoreCase), private
- `Mentions` — `IReadOnlyCollection<string>`, public read-only property

The factory methods `Create`, `ReplyTo`, and `Repost` each gain an optional `IEnumerable<string> mentions` parameter (defaults to empty). `Rehydrate` gains a `mentions` parameter so persisted posts round-trip correctly.

`SocialPost` also gains a static method:

```csharp
public static IEnumerable<string> ExtractMentionHandles(string content)
```

This parses `@word` tokens from the content string using a regex (`@[a-zA-Z0-9_]+`) and returns the raw handle strings (stripped of `@`). This is an enterprise business rule — the entity owns the knowledge of what a mention token looks like in its own content.

No validation logic lives in the entity. It stores only what it is given, and exposes the extraction rule.

---

## Section 2: Gateway Interface — `IAccountHandleGateway`

A new interface is added to `SocialApp.Post.Gateways`:

```csharp
public interface IAccountHandleGateway
{
    bool Exists(string handle);
}
```

This follows the identical structure as `IPostGateway` — defined in the business component, implemented in infrastructure.

**In-memory implementation:** `InMemoryAccountHandleGateway` in `PostGateways.cs`, backed by a `HashSet<string>` seeded at registration time. Used by component and unit tests.

**Infrastructure implementation:** `CosmosMongoAccountHandleGateway` queries the User collection for a document with the given handle.

---

## Section 3: Interactors

`CreatePostInteractor`, `ReplyToPostInteractor`, and `RepostInteractor` each receive `IAccountHandleGateway` as an additional constructor parameter.

After the content is known, each interactor:

1. Calls `SocialPost.ExtractMentionHandles(content)` — entity business rule
2. Filters results through `gateway.Exists(handle)` — application business rule (validate against real accounts)
3. Passes the validated handle set into the relevant factory method (`Create`, `ReplyTo`, `Repost`)

The `RepostInteractor` applies this to the optional comment content added by the reposter — the repost is its own `SocialPost` entity and may contain its own mentions independent of the original post.

---

## Section 4: Data Flow — Response Models, View Models, Persistence

### Response and view models

`PostSummaryResponse` gains `IReadOnlyList<string> Mentions`.

`PostSummaryViewModel` gains `IReadOnlyList<string> Mentions`.

`PostSummaryProjection.ToSummary` passes `post.Mentions` through to the response model.

`PostSummaryResult` in `SocialApp.Web.Services.SocialAppApiClient` gains `IReadOnlyList<string> Mentions` so the web layer receives validated mentions from the API.

`QuotedPostSummaryResponse` and `QuotedPostSummaryViewModel` do **not** gain `Mentions` — the quoted post is display-only and rendering mention links inside a quoted post preview is out of scope.

### Persistence

`PostDocument` gains `string[] Mentions` (default `Array.Empty<string>()`).

`CosmosMongoMappers` maps `Mentions` in both directions (entity → document, document → entity via `Rehydrate`).

No migration is required — existing documents without the field deserialize to an empty array.

---

## Section 5: Presentation — Segmented Content

`SocialApp.Web` has no project reference to `SocialApp.Post` (it calls the API over HTTP only). Segment types defined in `SocialApp.Post.ViewModels` cannot be used directly by the web layer. Segmentation therefore happens in the web layer using web-local types, applying the Humble Object pattern within that layer: all logic lives in a dedicated class, the Razor component renders only.

### Web-local segment types

Defined in `SocialApp.Web`:

```csharp
public abstract record PostContentSegment;
public sealed record TextSegment(string Text) : PostContentSegment;
public sealed record MentionSegment(string Handle) : PostContentSegment;
```

### Segmenter

`PostContentSegmenter` — a static class in `SocialApp.Web.Services`:

```csharp
public static IReadOnlyList<PostContentSegment> Segment(string content, IReadOnlyList<string> mentions)
```

It scans `content` for `@word` tokens using the same regex as the entity (`@[a-zA-Z0-9_]+`). For each token whose normalized handle appears in `mentions`, it emits a `MentionSegment`. All surrounding text becomes `TextSegment`. This class is the testable logic layer; it contains no rendering.

### Razor component

A new `PostContent.razor` component in `SocialApp.Web` accepts:

- `ContentSegments` — `IReadOnlyList<PostContentSegment>`

It iterates the segments and renders:
- `TextSegment` → inline text node
- `MentionSegment` → `<a href="/users/{handle}">@handle</a>`

The component contains no parsing logic. `Feed.razor` and `UserProfile.razor` replace their bare `<p>@item.Content</p>` with:

```razor
<PostContent ContentSegments="@PostContentSegmenter.Segment(item.Content, item.Mentions)" />
```

---

## Out of Scope

- Searching posts by mentioned handle
- Mention notifications
- Mention links inside quoted post previews
- Autocomplete suggestions while typing `@`
