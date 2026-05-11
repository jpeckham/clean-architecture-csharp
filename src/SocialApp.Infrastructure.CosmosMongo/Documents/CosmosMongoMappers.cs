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
        PasswordHash = user.PasswordHash
    };

    public static UserAccount ToEntity(UserDocument document) =>
        UserAccount.Rehydrate(document.Id, document.DisplayName, document.Handle, document.Email, document.PasswordHash);

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
