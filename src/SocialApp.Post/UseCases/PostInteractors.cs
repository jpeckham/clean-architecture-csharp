using SocialApp.Post.Entities;
using SocialApp.Post.Gateways;
using SocialApp.Post.RequestModels;
using SocialApp.Post.ResponseModels;

namespace SocialApp.Post.UseCases;

public sealed class CreatePostInteractor(IPostGateway posts, ICreatePostOutputBoundary output) : ICreatePostInputBoundary
{
    public void Handle(CreatePostRequest request) => output.Present(new(true, PostMessageKeys.PostCreated, ToSummary(posts.Save(SocialPost.Create(request.AuthorHandle, request.Content)))));
    public static PostSummaryResponse ToSummary(SocialPost post, string? readerHandle = null) =>
        new(
            post.Id,
            post.AuthorHandle,
            post.Content,
            post.ParentPostId,
            post.OriginalPostId,
            post.LikedBy.Count,
            readerHandle is not null && post.LikedBy.Contains(readerHandle));
}

public sealed class ScrollPostsInteractor(IPostGateway posts, IScrollPostsOutputBoundary output) : IScrollPostsInputBoundary
{
    public void Handle(ScrollPostsRequest request) => output.Present(new(posts.ScrollFor(request.ReaderHandle, request.Limit).Select(post => CreatePostInteractor.ToSummary(post, request.ReaderHandle)).ToArray()));
}

public sealed class SearchPostsInteractor(IPostSearchGateway search, ISearchPostsOutputBoundary output) : ISearchPostsInputBoundary
{
    public void Handle(SearchPostsRequest request) => output.Present(new(search.Search(request.Query).Select(post => CreatePostInteractor.ToSummary(post)).ToArray()));
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
        if (posts.FindById(request.OriginalPostId) is null) { output.Present(new(false, PostMessageKeys.OriginalPostNotFound, null)); return; }
        var repost = posts.Save(SocialPost.Repost(request.OriginalPostId, request.AuthorHandle));
        output.Present(new(true, PostMessageKeys.RepostCreated, CreatePostInteractor.ToSummary(repost)));
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
