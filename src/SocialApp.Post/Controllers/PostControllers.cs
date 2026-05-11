using SocialApp.Post.UseCases;

namespace SocialApp.Post.Controllers;

public sealed class CreatePostController(ICreatePostInputBoundary input) { public void Create(string authorHandle, string content) => input.Handle(new(authorHandle, content)); }
public sealed class ScrollPostsController(IScrollPostsInputBoundary input) { public void Scroll(string readerHandle, int limit) => input.Handle(new(readerHandle, limit)); }
public sealed class SearchPostsController(ISearchPostsInputBoundary input) { public void Search(string query) => input.Handle(new(query)); }
public sealed class FollowUserPostsController(IFollowUserPostsInputBoundary input) { public void Follow(string readerHandle, string followedHandle) => input.Handle(new(readerHandle, followedHandle)); }
public sealed class BlockUserPostsController(IBlockUserPostsInputBoundary input) { public void Block(string readerHandle, string blockedHandle) => input.Handle(new(readerHandle, blockedHandle)); }
public sealed class AddLikeToPostController(IAddLikeToPostInputBoundary input) { public void AddLike(Guid postId, string handle) => input.Handle(new(postId, handle)); }
public sealed class DeleteLikeFromPostController(IDeleteLikeFromPostInputBoundary input) { public void DeleteLike(Guid postId, string handle) => input.Handle(new(postId, handle)); }
public sealed class ReplyToPostController(IReplyToPostInputBoundary input) { public void Reply(Guid parentPostId, string authorHandle, string content) => input.Handle(new(parentPostId, authorHandle, content)); }
public sealed class RepostController(IRepostInputBoundary input) { public void Repost(Guid originalPostId, string authorHandle, string content = "") => input.Handle(new(originalPostId, authorHandle, content)); }
public sealed class DeletePostController(IDeletePostInputBoundary input) { public void Delete(Guid postId, string requesterHandle) => input.Handle(new(postId, requesterHandle)); }
