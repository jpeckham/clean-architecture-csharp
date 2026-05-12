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
        PasswordHash = user.PasswordHash,
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
        LikedBy = post.LikedBy.ToArray()
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
            document.LikedBy);

}
