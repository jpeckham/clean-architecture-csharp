using SocialApp.Post.Gateways;
using SocialApp.User.Gateways;

namespace SocialApp.Api.Endpoints;

public sealed class UserProfilePostGatewayAdapter(IProfilePostSummaryReadPort posts) : IUserProfilePostGateway
{
    public IReadOnlyList<UserProfilePostSummary> RecentPostsByAuthor(string authorHandle, string readerHandle, int limit) =>
        posts.RecentByAuthor(authorHandle, readerHandle, limit)
            .Select(ToUserProfilePost)
            .ToArray();

    private static UserProfilePostSummary ToUserProfilePost(SocialApp.Post.ResponseModels.PostSummaryResponse post) =>
        new(
            post.Id,
            post.AuthorHandle,
            post.Content,
            post.ParentPostId,
            post.OriginalPostId,
            post.CreatedAt,
            post.LikeCount,
            post.LikedByCurrentReader,
            post.RepostCount,
            post.RepostedByCurrentReader,
            post.QuotedPost is null
                ? null
                : new UserProfileQuotedPostSummary(post.QuotedPost.Id, post.QuotedPost.AuthorHandle, post.QuotedPost.Content, post.QuotedPost.CreatedAt, post.QuotedPost.Media?.Select(m => new UserProfilePostMediaSummary(m.AssetId, m.Kind, m.ContentType, m.ByteLength, m.Width, m.Height, m.DurationMs, m.SortOrder, m.ThumbnailKey, m.AltText)).ToArray()),
            post.Media?.Select(m => new UserProfilePostMediaSummary(
                m.AssetId,
                m.Kind,
                m.ContentType,
                m.ByteLength,
                m.Width,
                m.Height,
                m.DurationMs,
                m.SortOrder,
                m.ThumbnailKey,
                m.AltText)).ToArray() ?? Array.Empty<UserProfilePostMediaSummary>(),
            post.Mentions?.ToArray(),
            post.Hashtags?.ToArray());
}
