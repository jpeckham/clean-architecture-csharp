using SocialApp.Post.RequestModels;
using SocialApp.Post.ResponseModels;

namespace SocialApp.Post.UseCases;

public interface ICreatePostInputBoundary { void Handle(CreatePostRequest request); }
public interface ICreatePostOutputBoundary { void Present(CreatePostResponse response); }
public interface IScrollPostsInputBoundary { void Handle(ScrollPostsRequest request); }
public interface IScrollPostsOutputBoundary { void Present(ScrollPostsResponse response); }
public interface ISearchPostsInputBoundary { void Handle(SearchPostsRequest request); }
public interface ISearchPostsOutputBoundary { void Present(SearchPostsResponse response); }
public interface IFollowUserPostsInputBoundary { void Handle(FollowUserPostsRequest request); }
public interface IFollowUserPostsOutputBoundary { void Present(FollowUserPostsResponse response); }
public interface IBlockUserPostsInputBoundary { void Handle(BlockUserPostsRequest request); }
public interface IBlockUserPostsOutputBoundary { void Present(BlockUserPostsResponse response); }
public interface IAddLikeToPostInputBoundary { void Handle(AddLikeToPostRequest request); }
public interface IAddLikeToPostOutputBoundary { void Present(AddLikeToPostResponse response); }
public interface IDeleteLikeFromPostInputBoundary { void Handle(DeleteLikeFromPostRequest request); }
public interface IDeleteLikeFromPostOutputBoundary { void Present(DeleteLikeFromPostResponse response); }
public interface IReplyToPostInputBoundary { void Handle(ReplyToPostRequest request); }
public interface IReplyToPostOutputBoundary { void Present(ReplyToPostResponse response); }
public interface IRepostInputBoundary { void Handle(RepostRequest request); }
public interface IRepostOutputBoundary { void Present(RepostResponse response); }
public interface IDeletePostInputBoundary { void Handle(DeletePostRequest request); }
public interface IDeletePostOutputBoundary { void Present(DeletePostResponse response); }
public interface IBeginPostMediaUploadInputBoundary { void Handle(BeginPostMediaUploadRequest request); }
public interface IBeginPostMediaUploadOutputBoundary { void Present(BeginPostMediaUploadResponse response); }
public interface ICompletePostMediaUploadInputBoundary { void Handle(CompletePostMediaUploadRequest request); }
public interface ICompletePostMediaUploadOutputBoundary { void Present(CompletePostMediaUploadResponse response); }
