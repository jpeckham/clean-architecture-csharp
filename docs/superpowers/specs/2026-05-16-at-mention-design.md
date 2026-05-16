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

`PostSummaryViewModel` gains `IReadOnlyList<PostContentSegment> ContentSegments` in place of a raw `Content` string (see Section 5). `Mentions` is not surfaced on the ViewModel — it is consumed by the Presenter to produce `ContentSegments` and does not need to travel further.

`PostSummaryProjection.ToSummary` passes `post.Mentions` through to the response model.

`PostSummaryResult` in `SocialApp.Web.Services.SocialAppApiClient` gains `IReadOnlyList<PostContentSegmentResult> ContentSegments` (web-local DTO types that mirror the server ViewModel, deserialized from JSON — see Section 5). `Mentions` is not needed on the web DTO.

`QuotedPostSummaryResponse` and `QuotedPostSummaryViewModel` do **not** gain `Mentions` or `ContentSegments` — the quoted post is display-only and rendering mention links inside a quoted post preview is out of scope.

### Persistence

`PostDocument` gains `string[] Mentions` (default `Array.Empty<string>()`).

`CosmosMongoMappers` maps `Mentions` in both directions (entity → document, document → entity via `Rehydrate`).

No migration is required — existing documents without the field deserialize to an empty array.

---

## Section 5: Presentation — Segmented Content

The segmentation of content into typed segments is a Presenter concern (Interface Adapters). Placing it in the server-side Presenter means it is tested as a server-side business concern and is portable to any view (web, mobile, etc.). Pushing it into the web layer would remove it from the testable surface.

`SocialApp.Web` has no project reference to `SocialApp.Post`. The solution is not to move the logic into the web layer — it is to serialize the ViewModel's `ContentSegments` as JSON with a type discriminator and define parallel DTO types in `SocialApp.Web` for deserialization. The logic and its tests remain on the server.

### Server-side segment types (`SocialApp.Post.ViewModels`)

```csharp
public abstract record PostContentSegment;
public sealed record TextSegment(string Text) : PostContentSegment;
public sealed record MentionSegment(string Handle) : PostContentSegment;
```

`PostSummaryViewModel` gains `IReadOnlyList<PostContentSegment> ContentSegments`.

### Presenter (`PostPresenters.cs`)

The Presenter segments content when building the ViewModel — consistent with the Humble Object pattern. It calls `SocialPost.ExtractMentionHandles(response.Content)` to identify token positions, checks each handle against `response.Mentions`, and emits typed segments. The resulting `ContentSegments` list fully represents the renderable content; no further parsing is needed by any consumer.

### JSON serialization

`PostContentSegment` uses `[JsonDerivedType]` attributes (System.Text.Json) to serialize with a `$type` discriminator. The API endpoint serializes the ViewModel including `ContentSegments` as a typed JSON array.

### Web-local DTO types (`SocialApp.Web`)

```csharp
public sealed record PostContentSegmentResult(string Type, string? Text, string? Handle);
```

`PostSummaryResult` gains `IReadOnlyList<PostContentSegmentResult> ContentSegments`. The JSON deserializer maps the server's polymorphic array into these flat DTOs via the `$type` field.

### Razor component

A new `PostContent.razor` component in `SocialApp.Web` accepts:

- `ContentSegments` — `IReadOnlyList<PostContentSegmentResult>`

It iterates the segments and renders based on `Type`:
- `"text"` → inline text node (`segment.Text`)
- `"mention"` → `<a href="/users/{handle}">@handle</a>` (`segment.Handle`)

The component contains no parsing logic. `Feed.razor` and `UserProfile.razor` replace their bare `<p>@item.Content</p>` with:

```razor
<PostContent ContentSegments="@item.ContentSegments" />
```

---

## Out of Scope

- Searching posts by mentioned handle
- Mention notifications
- Mention links inside quoted post previews
- Autocomplete suggestions while typing `@`
