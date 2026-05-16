# Hashtag Feature Design

**Date:** 2026-05-16

## Overview

When a user types `#word` in post content, every contiguous non-space sequence following the `#` is stored as a hashtag on the post and rendered as a hyperlink to a search of posts with that tag. No validation against existing posts is performed — a brand-new post is itself the first result of that search.

Content may contain multiple hashtags. Hashtags are extracted, stored, and segmented in the same pipeline as `@mentions`.

---

## Section 1: Entity — `SocialPost`

`SocialPost` gains:

- `_hashtags` — `HashSet<string>` (OrdinalIgnoreCase), private
- `Hashtags` — `IReadOnlyCollection<string>`, public read-only property

The factory methods `Create`, `ReplyTo`, `Repost`, and `Rehydrate` each gain an optional `IEnumerable<string>? hashtags` parameter (defaults to empty).

`SocialPost` also gains a static extraction method:

```csharp
private static readonly Regex HashtagPattern = new(@"#(\S+)", RegexOptions.Compiled, TimeSpan.FromSeconds(1));

public static IEnumerable<string> ExtractHashtags(string content) =>
    HashtagPattern.Matches(content).Select(m => m.Groups[1].Value);
```

The entity owns the knowledge of what a hashtag token looks like in its own content. No validation logic lives here — it stores exactly what it is given.

---

## Section 2: Interactors

`CreatePostInteractor`, `ReplyToPostInteractor`, and `RepostInteractor` each call `SocialPost.ExtractHashtags(content)` after content is known and pass the result directly into the factory method. No gateway lookup — every extracted hashtag is valid by definition.

The `RepostInteractor` applies this to the optional comment content, since a repost is its own `SocialPost` entity.

---

## Section 3: Response Models, View Models, Presenter

### `PostContentSegment`

The existing flat record gains a fourth field:

```csharp
public sealed record PostContentSegment(int Sequence, string Text, string? MentionHandle, string? HashtagText);
```

A segment has at most one of `MentionHandle` or `HashtagText` set — never both. Plain text segments have both null.

### `PostSummaryResponse`

Gains `IReadOnlyList<string>? Hashtags`, mirroring the `Mentions` field.

### Presenter — `SegmentContent`

The `SegmentContent` method is extended to match both `@mention` and `#hashtag` patterns in a single ordered pass over the content string, using a combined regex. Each match emits a segment with the appropriate nullable field populated. The interleaved ordering is preserved by sequence number.

`Hashtags` is not surfaced on `PostSummaryViewModel` — it is consumed by the Presenter to produce `ContentSegments` and does not need to travel further.

`QuotedPostSummaryResponse` and `QuotedPostSummaryViewModel` do **not** gain `Hashtags` or hashtag segments — the quoted post is display-only and rendering hashtag links inside a quoted post preview is out of scope.

---

## Section 4: Persistence

`PostDocument` gains a `Hashtags` field (`List<string>`). `CosmosMongoMappers` passes it through `Rehydrate` so persisted posts round-trip correctly.

`PostSummaryProjection.ToSummary` passes `post.Hashtags` through to the response model.

---

## Section 5: Web Layer

`PostSummaryResult` in `SocialApp.Web.Services.SocialAppApiClient` already carries `ContentSegments` (added for mentions). The new `HashtagText` field on each segment deserializes automatically from JSON.

The web rendering component checks each segment in order:
- `MentionHandle` set → render as link to user profile
- `HashtagText` set → render as link to `/search?tag={HashtagText}`
- Neither set → render as plain text
