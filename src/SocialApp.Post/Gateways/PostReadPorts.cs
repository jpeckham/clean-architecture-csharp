using SocialApp.Post.Entities;
using SocialApp.Post.ResponseModels;

namespace SocialApp.Post.Gateways;

public interface IProfilePostSummaryReadPort
{
    IReadOnlyList<PostSummaryResponse> RecentByAuthor(string authorHandle, string readerHandle, int limit);
}

public sealed class ProfilePostSummaryReadPort(IPostGateway posts) : IProfilePostSummaryReadPort
{
    public IReadOnlyList<PostSummaryResponse> RecentByAuthor(string authorHandle, string readerHandle, int limit) =>
        posts.RecentByAuthor(authorHandle, Math.Clamp(limit, 1, 100))
            .Select(post => PostSummaryProjection.ToSummary(post, posts, readerHandle))
            .ToArray();
}

internal static class PostSummaryProjection
{
    private const int RecentReplyLimit = 3;
    private const int DetailReplyLimit = 100;

    public static PostSummaryResponse ToSummary(SocialPost post, string? readerHandle = null) =>
        ToSummary(post, null, readerHandle);

    public static PostSummaryResponse ToSummary(SocialPost post, IPostGateway? posts, string? readerHandle = null, int replyDepth = 1, int replyLimit = RecentReplyLimit)
    {
        var normalizedReaderHandle = readerHandle is null ? null : SocialPost.NormalizeHandle(readerHandle);
        var originalPost = post.OriginalPostId is { } originalPostId && posts is not null
            ? posts.FindById(originalPostId)
            : null;
        var replyTarget = post.ParentPostId is { } parentPostId && posts is not null
            ? posts.FindById(parentPostId)
            : null;
        var repostTargetId = post.OriginalPostId ?? post.Id;
        var recentReplies = replyDepth > 0 && posts is not null
            ? posts.RecentReplies(post.Id, replyLimit)
                .Select(reply => ToSummary(reply, posts, readerHandle, replyDepth - 1, replyLimit))
                .ToArray()
            : Array.Empty<PostSummaryResponse>();

        return new(
            post.Id,
            post.AuthorHandle,
            post.Content,
            post.ParentPostId,
            post.OriginalPostId,
            post.CreatedAt,
            post.LikedBy.Count,
            normalizedReaderHandle is not null && post.LikedBy.Contains(normalizedReaderHandle),
            posts?.CountActiveReposts(repostTargetId) ?? 0,
            normalizedReaderHandle is not null && posts?.FindActiveRepost(repostTargetId, normalizedReaderHandle) is not null,
            posts?.CountReplies(post.Id) ?? 0,
            recentReplies,
            replyTarget is null ? null : ToQuotedSummary(replyTarget),
            originalPost is null ? null : ToQuotedSummary(originalPost),
            post.Media.Select(ToMediaSummary).ToArray(),
            post.Mentions.ToArray(),
            post.Hashtags.ToArray());
    }

    public static PostSummaryResponse ToFocusedThreadSummary(SocialPost post, IPostGateway posts, string readerHandle) =>
        ToSummary(post, posts, readerHandle, replyDepth: 3, replyLimit: DetailReplyLimit);

    private static QuotedPostSummaryResponse ToQuotedSummary(SocialPost post) =>
        new(
            post.Id,
            post.AuthorHandle,
            post.Content,
            post.CreatedAt,
            post.Media.Select(ToMediaSummary).ToArray());

    public static PostMediaSummaryResponse ToMediaSummary(PostMediaItem item) => new(
        item.AssetId,
        item.Kind.ToString(),
        item.ContentType,
        item.ByteLength,
        item.Width,
        item.Height,
        item.DurationMs,
        item.SortOrder,
        item.ThumbnailKey,
        item.AltText);
}
