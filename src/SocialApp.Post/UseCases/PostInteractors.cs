using SocialApp.Post.Entities;
using SocialApp.Post.Gateways;
using SocialApp.Post.RequestModels;
using SocialApp.Post.ResponseModels;

namespace SocialApp.Post.UseCases;

public sealed class CreatePostInteractor(IPostGateway posts, ICreatePostOutputBoundary output, IPostMediaStorageGateway? mediaStorage = null) : ICreatePostInputBoundary
{
    public void Handle(CreatePostRequest request)
    {
        var media = ResolveMedia(request);
        output.Present(new(true, PostMessageKeys.PostCreated, ToSummary(posts.Save(SocialPost.Create(request.AuthorHandle, request.Content, media)))));
    }

    public static PostSummaryResponse ToSummary(SocialPost post, string? readerHandle = null) =>
        ToSummary(post, null, readerHandle);

    public static PostSummaryResponse ToSummary(SocialPost post, IPostGateway? posts, string? readerHandle = null)
    {
        var originalPost = post.OriginalPostId is { } originalPostId && posts is not null
            ? posts.FindById(originalPostId)
            : null;
        var repostTargetId = post.OriginalPostId ?? post.Id;

        return
        new(
            post.Id,
            post.AuthorHandle,
            post.Content,
            post.ParentPostId,
            post.OriginalPostId,
            post.CreatedAt,
            post.LikedBy.Count,
            readerHandle is not null && post.LikedBy.Contains(readerHandle),
            posts?.CountActiveReposts(repostTargetId) ?? 0,
            readerHandle is not null && posts?.FindActiveRepost(repostTargetId, readerHandle) is not null,
            originalPost is null ? null : new(originalPost.Id, originalPost.AuthorHandle, originalPost.Content, originalPost.CreatedAt),
            post.Media.Select(ToMediaSummary).ToArray());
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

    private IReadOnlyList<PostMediaItem> ResolveMedia(CreatePostRequest request)
    {
        if (request.MediaAssetIds is null || request.MediaAssetIds.Count == 0)
        {
            return Array.Empty<PostMediaItem>();
        }

        if (mediaStorage is null)
        {
            throw new InvalidOperationException("Media asset must be completed and owned by the post author.");
        }

        var media = new List<PostMediaItem>();
        for (var index = 0; index < request.MediaAssetIds.Count; index++)
        {
            var item = mediaStorage.FindCompletedAsset(request.MediaAssetIds[index], request.AuthorHandle);
            if (item is null)
            {
                throw new InvalidOperationException("Media asset must be completed and owned by the post author.");
            }

            media.Add(new PostMediaItem(
                item.AssetId,
                item.Kind,
                item.StorageKey,
                item.ContentType,
                item.ByteLength,
                item.Width,
                item.Height,
                item.DurationMs,
                index,
                item.ThumbnailKey,
                item.AltText));
        }

        return media;
    }
}

public sealed class BeginPostMediaUploadInteractor(IPostMediaStorageGateway media, IBeginPostMediaUploadOutputBoundary output) : IBeginPostMediaUploadInputBoundary
{
    public void Handle(BeginPostMediaUploadRequest request)
    {
        var upload = media.ReserveUpload(new(
            request.OwnerHandle,
            request.Kind,
            request.ContentType,
            request.ByteLength,
            request.Width,
            request.Height,
            request.DurationMs,
            null,
            request.AltText));

        output.Present(new(
            true,
            PostMessageKeys.PostMediaUploadReserved,
            new(upload.AssetId, upload.StorageKey, upload.UploadUrl)));
    }
}

public sealed class CompletePostMediaUploadInteractor(IPostMediaStorageGateway media, ICompletePostMediaUploadOutputBoundary output) : ICompletePostMediaUploadInputBoundary
{
    public void Handle(CompletePostMediaUploadRequest request)
    {
        var item = media.CompleteUpload(new(request.AssetId, request.OwnerHandle));
        output.Present(item is null
            ? new(false, PostMessageKeys.PostMediaAssetNotFound, null)
            : new(true, PostMessageKeys.PostMediaUploadCompleted, CreatePostInteractor.ToMediaSummary(item)));
    }
}

public sealed class ScrollPostsInteractor(IPostGateway posts, IScrollPostsOutputBoundary output) : IScrollPostsInputBoundary
{
    public void Handle(ScrollPostsRequest request) => output.Present(new(posts.ScrollFor(request.ReaderHandle, request.Limit).Select(post => CreatePostInteractor.ToSummary(post, posts, request.ReaderHandle)).ToArray()));
}

public sealed class SearchPostsInteractor(IPostSearchGateway search, ISearchPostsOutputBoundary output, IPostGateway? posts = null) : ISearchPostsInputBoundary
{
    public void Handle(SearchPostsRequest request) => output.Present(new(search.Search(request.Query).Select(post => CreatePostInteractor.ToSummary(post, posts, request.ReaderHandle)).ToArray()));
}

public sealed class FollowUserPostsInteractor(IPostGateway posts, IFollowUserPostsOutputBoundary output) : IFollowUserPostsInputBoundary
{
    public void Handle(FollowUserPostsRequest request)
    {
        posts.Follow(request.ReaderHandle, request.FollowedHandle);
        output.Present(new(true, PostMessageKeys.UserFollowed));
    }
}

public sealed class BlockUserPostsInteractor(IPostGateway posts, IBlockUserPostsOutputBoundary output) : IBlockUserPostsInputBoundary
{
    public void Handle(BlockUserPostsRequest request)
    {
        posts.Block(request.ReaderHandle, request.BlockedHandle);
        output.Present(new(true, PostMessageKeys.UserBlocked));
    }
}

public sealed class AddLikeToPostInteractor(IPostGateway posts, IAddLikeToPostOutputBoundary output) : IAddLikeToPostInputBoundary
{
    public void Handle(AddLikeToPostRequest request)
    {
        var post = posts.FindById(request.PostId);
        if (post is null) { output.Present(new(false, PostMessageKeys.PostNotFound)); return; }
        post.AddLike(request.Handle);
        posts.Save(post);
        output.Present(new(true, PostMessageKeys.LikeAdded));
    }
}

public sealed class DeleteLikeFromPostInteractor(IPostGateway posts, IDeleteLikeFromPostOutputBoundary output) : IDeleteLikeFromPostInputBoundary
{
    public void Handle(DeleteLikeFromPostRequest request)
    {
        var post = posts.FindById(request.PostId);
        if (post is null) { output.Present(new(false, PostMessageKeys.PostNotFound)); return; }
        if (!post.LikedBy.Contains(request.Handle)) { throw new InvalidOperationException("Cannot delete a like that does not exist."); }
        post.DeleteLike(request.Handle);
        posts.Save(post);
        output.Present(new(true, PostMessageKeys.LikeDeleted));
    }
}

public sealed class ReplyToPostInteractor(IPostGateway posts, IReplyToPostOutputBoundary output) : IReplyToPostInputBoundary
{
    public void Handle(ReplyToPostRequest request)
    {
        if (posts.FindById(request.ParentPostId) is null) { output.Present(new(false, PostMessageKeys.ParentPostNotFound, null)); return; }
        var reply = posts.Save(SocialPost.ReplyTo(request.ParentPostId, request.AuthorHandle, request.Content));
        output.Present(new(true, PostMessageKeys.ReplyCreated, CreatePostInteractor.ToSummary(reply)));
    }
}

public sealed class RepostInteractor(IPostGateway posts, IRepostOutputBoundary output) : IRepostInputBoundary
{
    public void Handle(RepostRequest request)
    {
        var requestedPost = posts.FindById(request.OriginalPostId);
        if (requestedPost is null) { output.Present(new(false, PostMessageKeys.OriginalPostNotFound, null)); return; }

        var targetPost = requestedPost.OriginalPostId is { } rootOriginalPostId
            ? posts.FindById(rootOriginalPostId)
            : requestedPost;

        if (targetPost is null) { output.Present(new(false, PostMessageKeys.OriginalPostNotFound, null)); return; }

        if (string.Equals(targetPost.AuthorHandle, request.AuthorHandle, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Users cannot repost their own posts.");
        }

        if (posts.FindActiveRepost(targetPost.Id, request.AuthorHandle) is not null)
        {
            throw new InvalidOperationException("Users can repost a post only once.");
        }

        var repost = posts.Save(SocialPost.Repost(targetPost.Id, request.AuthorHandle, request.Content));
        output.Present(new(true, PostMessageKeys.RepostCreated, CreatePostInteractor.ToSummary(repost, posts, request.AuthorHandle)));
    }
}

public sealed class DeletePostInteractor(IPostGateway posts, IDeletePostOutputBoundary output) : IDeletePostInputBoundary
{
    public void Handle(DeletePostRequest request)
    {
        var post = posts.FindById(request.PostId);
        if (post is null) { output.Present(new(false, PostMessageKeys.PostNotFound)); return; }
        post.DeleteBy(request.RequesterHandle);
        posts.Save(post);
        output.Present(new(true, PostMessageKeys.PostDeleted));
    }
}
