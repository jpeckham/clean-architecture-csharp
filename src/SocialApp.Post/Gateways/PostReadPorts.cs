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
    public static PostSummaryResponse ToSummary(SocialPost post, string? readerHandle = null) =>
        ToSummary(post, null, readerHandle);

    public static PostSummaryResponse ToSummary(SocialPost post, IPostGateway? posts, string? readerHandle = null)
    {
        var normalizedReaderHandle = readerHandle is null ? null : SocialPost.NormalizeHandle(readerHandle);
        var originalPost = post.OriginalPostId is { } originalPostId && posts is not null
            ? posts.FindById(originalPostId)
            : null;
        var repostTargetId = post.OriginalPostId ?? post.Id;

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
            originalPost is null ? null : new(originalPost.Id, originalPost.AuthorHandle, originalPost.Content, originalPost.CreatedAt, originalPost.Media.Select(ToMediaSummary).ToArray()),
            post.Media.Select(ToMediaSummary).ToArray(),
            post.Mentions.ToArray());
    }

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
