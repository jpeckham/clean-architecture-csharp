using SocialApp.Post.Entities;
using SocialApp.User.Entities;

namespace SocialApp.Infrastructure.CosmosMongo.Documents;

public static class CosmosMongoMappers
{
    public static UserDocument ToDocument(UserAccount user) => new()
    {
        Id = user.Id,
        DisplayName = user.DisplayName,
        Handle = user.Handle,
        Email = user.Email,
        PasswordHash = user.Credentials,
        ProfileImage = user.ProfileImage is null
            ? null
            : new ProfileImageDocument
            {
                AssetId = user.ProfileImage.AssetId,
                StorageKey = user.ProfileImage.StorageKey,
                ContentType = user.ProfileImage.ContentType,
                ByteLength = user.ProfileImage.ByteLength,
                Width = user.ProfileImage.Width,
                Height = user.ProfileImage.Height,
                UploadedAt = user.ProfileImage.UploadedAt
            }
    };

    public static UserAccount ToEntity(UserDocument document) =>
        UserAccount.Rehydrate(
            document.Id,
            document.DisplayName,
            document.Handle,
            document.Email,
            document.PasswordHash,
            document.ProfileImage is null
                ? null
                : new ProfileImage(
                    document.ProfileImage.AssetId,
                    document.ProfileImage.StorageKey,
                    document.ProfileImage.ContentType,
                    document.ProfileImage.ByteLength,
                    document.ProfileImage.Width,
                    document.ProfileImage.Height,
                    document.ProfileImage.UploadedAt));

    public static PostDocument ToDocument(SocialPost post) => new()
    {
        Id = post.Id,
        AuthorHandle = post.AuthorHandle,
        Content = post.Content,
        ParentPostId = post.ParentPostId,
        OriginalPostId = post.OriginalPostId,
        CreatedAt = post.CreatedAt,
        IsDeleted = post.IsDeleted,
        LikedBy = post.LikedBy.ToArray(),
        Media = post.Media.Select(item => new PostMediaDocument
        {
            AssetId = item.AssetId,
            Kind = item.Kind.ToString(),
            StorageKey = item.StorageKey,
            ContentType = item.ContentType,
            ByteLength = item.ByteLength,
            Width = item.Width,
            Height = item.Height,
            DurationMs = item.DurationMs,
            SortOrder = item.SortOrder,
            ThumbnailKey = item.ThumbnailKey,
            AltText = item.AltText
        }).ToArray(),
        Mentions = post.Mentions.ToArray(),
        Hashtags = post.Hashtags.ToArray()
    };

    public static SocialPost ToEntity(PostDocument document) =>
        SocialPost.Rehydrate(
            document.Id,
            document.AuthorHandle,
            document.Content,
            document.ParentPostId,
            document.OriginalPostId,
            document.CreatedAt,
            document.IsDeleted,
            document.LikedBy,
            (document.Media ?? Array.Empty<PostMediaDocument>()).Select(ToPostMediaItem).ToArray(),
            document.Mentions,
            document.Hashtags);

    private static PostMediaItem ToPostMediaItem(PostMediaDocument document) =>
        new(
            document.AssetId,
            Enum.Parse<PostMediaKind>(document.Kind, ignoreCase: true),
            document.StorageKey,
            document.ContentType,
            document.ByteLength,
            document.Width,
            document.Height,
            document.DurationMs,
            document.SortOrder,
            document.ThumbnailKey,
            document.AltText);
}
