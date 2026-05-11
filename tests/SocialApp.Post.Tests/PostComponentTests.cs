using FluentAssertions;
using SocialApp.Post.Controllers;
using SocialApp.Post.Entities;
using SocialApp.Post.Gateways;
using SocialApp.Post.Presenters;
using SocialApp.Post.ResponseModels;
using SocialApp.Post.UseCases;
using Xunit;

namespace SocialApp.Post.Tests;

public sealed class PostComponentTests
{
    [Fact]
    public void Post_entity_enforces_content_and_like_rules()
    {
        var post = SocialPost.Create("@ada", "Hello component architecture");

        post.AddLike("@grace");
        post.LikedBy.Should().Contain("@grace");
        post.DeleteLike("@grace");
        post.LikedBy.Should().BeEmpty();
        Action blank = () => SocialPost.Create("@ada", " ");
        blank.Should().Throw<ArgumentException>().WithMessage("*content*");
    }

    [Fact]
    public void Post_can_be_rehydrated_from_persistence()
    {
        var id = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow.AddMinutes(-5);

        var post = SocialPost.Rehydrate(id, "@ada", "persisted post", null, null, createdAt, true, new[] { "@grace" });

        post.Id.Should().Be(id);
        post.CreatedAt.Should().Be(createdAt);
        post.IsDeleted.Should().BeTrue();
        post.LikedBy.Should().ContainSingle("@grace");
    }

    [Fact]
    public void Create_post_flow_runs_controller_interactor_gateway_presenter()
    {
        var posts = new InMemoryPostGateway();
        var presenter = new CreatePostPresenter();
        var controller = new CreatePostController(new CreatePostInteractor(posts, presenter));

        controller.Create("@ada", "First post");

        presenter.ViewModel.Should().NotBeNull();
        presenter.ViewModel!.Succeeded.Should().BeTrue();
        posts.AllPosts.Should().ContainSingle(p => p.AuthorHandle == "@ada");
    }

    [Fact]
    public void Create_post_interactor_returns_message_key_not_presentation_copy()
    {
        var posts = new InMemoryPostGateway();
        var output = new CapturingCreatePostOutput();
        var interactor = new CreatePostInteractor(posts, output);

        interactor.Handle(new("@ada", "First post"));

        output.Response.Should().NotBeNull();
        output.Response!.MessageKey.Should().Be(PostMessageKeys.PostCreated);
        output.Response.MessageKey.Should().NotContain(" ");
        output.Response.MessageKey.Should().NotEndWith(".");
    }

    [Fact]
    public void Create_post_presenter_translates_message_key_for_view_model()
    {
        var presenter = new CreatePostPresenter();
        var post = SocialPost.Create("@ada", "First post");

        presenter.Present(new(true, PostMessageKeys.PostCreated, CreatePostInteractor.ToSummary(post)));

        presenter.ViewModel!.Message.Should().Be("Post created.");
    }

    [Fact]
    public void In_memory_post_gateway_save_updates_existing_post_by_id()
    {
        var posts = new InMemoryPostGateway();
        var post = posts.Save(SocialPost.Create("@ada", "root"));
        post.DeleteBy("@ada");

        posts.Save(post);

        posts.AllPosts.Should().ContainSingle(p => p.Id == post.Id);
        posts.FindById(post.Id)!.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public void Scroll_search_follow_and_block_posts_filter_feed()
    {
        var posts = new InMemoryPostGateway();
        var search = new InMemoryPostSearchGateway(posts);
        var ada = posts.Save(SocialPost.Create("@ada", "math notes"));
        posts.Save(SocialPost.Create("@grace", "compiler notes"));

        new FollowUserPostsController(new FollowUserPostsInteractor(posts, new FollowUserPostsPresenter()))
            .Follow("@reader", "@ada");
        ada.AddLike("@reader");
        posts.Save(ada);

        var feedPresenter = new ScrollPostsPresenter();
        new ScrollPostsController(new ScrollPostsInteractor(posts, feedPresenter)).Scroll("@reader", 10);
        feedPresenter.ViewModel!.Posts.Should().ContainSingle(p => p.Id == ada.Id);
        feedPresenter.ViewModel!.Posts.Single(p => p.Id == ada.Id).LikedByCurrentReader.Should().BeTrue();

        new BlockUserPostsController(new BlockUserPostsInteractor(posts, new BlockUserPostsPresenter()))
            .Block("@reader", "@ada");
        new ScrollPostsController(new ScrollPostsInteractor(posts, feedPresenter)).Scroll("@reader", 10);
        feedPresenter.ViewModel!.Posts.Should().BeEmpty();

        var searchPresenter = new SearchPostsPresenter();
        new SearchPostsController(new SearchPostsInteractor(search, searchPresenter)).Search("compiler");
        searchPresenter.ViewModel!.Posts.Should().ContainSingle(p => p.AuthorHandle == "@grace");
    }

    [Fact]
    public void Like_reply_repost_and_delete_are_separate_use_cases()
    {
        var posts = new CountingPostGateway();
        var original = posts.Save(SocialPost.Create("@ada", "root"));
        posts.ResetSaveCount();

        new AddLikeToPostController(new AddLikeToPostInteractor(posts, new AddLikeToPostPresenter()))
            .AddLike(original.Id, "@grace");
        posts.FindById(original.Id)!.LikedBy.Should().Contain("@grace");
        posts.SaveCount.Should().Be(1);

        var replyPresenter = new ReplyToPostPresenter();
        new ReplyToPostController(new ReplyToPostInteractor(posts, replyPresenter))
            .Reply(original.Id, "@grace", "reply");
        replyPresenter.ViewModel!.ParentPostId.Should().Be(original.Id);

        var repostPresenter = new RepostPresenter();
        new RepostController(new RepostInteractor(posts, repostPresenter))
            .Repost(original.Id, "@linus");
        repostPresenter.ViewModel!.OriginalPostId.Should().Be(original.Id);

        new DeleteLikeFromPostController(new DeleteLikeFromPostInteractor(posts, new DeleteLikeFromPostPresenter()))
            .DeleteLike(original.Id, "@grace");
        posts.FindById(original.Id)!.LikedBy.Should().BeEmpty();

        var deletePresenter = new DeletePostPresenter();
        new DeletePostController(new DeletePostInteractor(posts, deletePresenter))
            .Delete(original.Id, "@ada");
        deletePresenter.ViewModel!.Succeeded.Should().BeTrue();
        posts.FindById(original.Id)!.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public void Repost_rejects_self_repost_and_duplicate_active_repost()
    {
        var posts = new InMemoryPostGateway();
        var original = posts.Save(SocialPost.Create("@ada", "root"));
        var controller = new RepostController(new RepostInteractor(posts, new RepostPresenter()));

        Action selfRepost = () => controller.Repost(original.Id, "@ada", "quote");
        selfRepost.Should().Throw<InvalidOperationException>().WithMessage("Users cannot repost their own posts.");

        controller.Repost(original.Id, "@grace", "first quote");
        Action duplicate = () => controller.Repost(original.Id, "@grace", "second quote");
        duplicate.Should().Throw<InvalidOperationException>().WithMessage("Users can repost a post only once.");
    }

    [Fact]
    public void Reposting_a_repost_targets_the_quoted_original_post()
    {
        var posts = new InMemoryPostGateway();
        var original = posts.Save(SocialPost.Create("@ada", "root"));
        var firstPresenter = new RepostPresenter();
        var firstController = new RepostController(new RepostInteractor(posts, firstPresenter));
        firstController.Repost(original.Id, "@grace", "first quote");

        var secondPresenter = new RepostPresenter();
        new RepostController(new RepostInteractor(posts, secondPresenter))
            .Repost(firstPresenter.ViewModel!.Id!.Value, "@linus", "second quote");

        secondPresenter.ViewModel!.OriginalPostId.Should().Be(original.Id);
        posts.FindById(secondPresenter.ViewModel.Id!.Value)!.OriginalPostId.Should().Be(original.Id);
    }

    [Fact]
    public void User_can_repost_original_again_after_deleting_their_active_repost()
    {
        var posts = new InMemoryPostGateway();
        var original = posts.Save(SocialPost.Create("@ada", "root"));
        var firstPresenter = new RepostPresenter();
        var repostController = new RepostController(new RepostInteractor(posts, firstPresenter));
        repostController.Repost(original.Id, "@grace", "first quote");
        var firstRepostId = firstPresenter.ViewModel!.Id!.Value;

        new DeletePostController(new DeletePostInteractor(posts, new DeletePostPresenter()))
            .Delete(firstRepostId, "@grace");

        var secondPresenter = new RepostPresenter();
        new RepostController(new RepostInteractor(posts, secondPresenter))
            .Repost(original.Id, "@grace", "second quote");

        secondPresenter.ViewModel!.Succeeded.Should().BeTrue();
        secondPresenter.ViewModel.OriginalPostId.Should().Be(original.Id);
        secondPresenter.ViewModel.Id.Should().NotBe(firstRepostId);
        posts.CountActiveReposts(original.Id).Should().Be(1);
    }

    [Fact]
    public void Feed_projection_includes_repost_state_counts_and_quoted_original()
    {
        var posts = new InMemoryPostGateway();
        var original = posts.Save(SocialPost.Create("@ada", "root"));
        new RepostController(new RepostInteractor(posts, new RepostPresenter()))
            .Repost(original.Id, "@grace", "Grace take");
        new RepostController(new RepostInteractor(posts, new RepostPresenter()))
            .Repost(original.Id, "@linus", string.Empty);

        var presenter = new ScrollPostsPresenter();
        new ScrollPostsController(new ScrollPostsInteractor(posts, presenter)).Scroll("@grace", 10);

        var originalView = presenter.ViewModel!.Posts.Single(p => p.Id == original.Id);
        originalView.RepostCount.Should().Be(2);
        originalView.RepostedByCurrentReader.Should().BeTrue();

        var repostView = presenter.ViewModel.Posts.Single(p => p.AuthorHandle == "@grace");
        repostView.Content.Should().Be("Grace take");
        repostView.OriginalPostId.Should().Be(original.Id);
        repostView.QuotedPost.Should().NotBeNull();
        repostView.QuotedPost!.AuthorHandle.Should().Be("@ada");
        repostView.QuotedPost.Content.Should().Be("root");
        repostView.RepostCount.Should().Be(2);
        repostView.RepostedByCurrentReader.Should().BeTrue();
    }

    [Fact]
    public void Delete_like_rejects_handles_that_have_not_liked_the_post()
    {
        var posts = new InMemoryPostGateway();
        var original = posts.Save(SocialPost.Create("@ada", "root"));
        var controller = new DeleteLikeFromPostController(new DeleteLikeFromPostInteractor(posts, new DeleteLikeFromPostPresenter()));

        Action delete = () => controller.DeleteLike(original.Id, "@grace");

        delete.Should().Throw<InvalidOperationException>().WithMessage("Cannot delete a like that does not exist.");
        posts.FindById(original.Id)!.LikedBy.Should().BeEmpty();
    }

    [Fact]
    public void Delete_post_rejects_non_author_and_keeps_post_visible()
    {
        var posts = new InMemoryPostGateway();
        var original = posts.Save(SocialPost.Create("@ada", "root"));
        var controller = new DeletePostController(new DeletePostInteractor(posts, new DeletePostPresenter()));

        Action delete = () => controller.Delete(original.Id, "@grace");

        delete.Should().Throw<InvalidOperationException>().WithMessage("Only the author can delete the post.");
        posts.FindById(original.Id)!.IsDeleted.Should().BeFalse();
    }

    private sealed class CountingPostGateway : IPostGateway
    {
        private readonly InMemoryPostGateway inner = new();

        public int SaveCount { get; private set; }

        public SocialPost Save(SocialPost post)
        {
            SaveCount++;
            return inner.Save(post);
        }

        public SocialPost? FindById(Guid id) => inner.FindById(id);

        public IReadOnlyList<SocialPost> ScrollFor(string readerHandle, int limit) =>
            inner.ScrollFor(readerHandle, limit);

        public void Follow(string readerHandle, string followedHandle) =>
            inner.Follow(readerHandle, followedHandle);

        public void Block(string readerHandle, string blockedHandle) =>
            inner.Block(readerHandle, blockedHandle);

        public SocialPost? FindActiveRepost(Guid originalPostId, string authorHandle) =>
            inner.FindActiveRepost(originalPostId, authorHandle);

        public int CountActiveReposts(Guid originalPostId) =>
            inner.CountActiveReposts(originalPostId);

        public void ResetSaveCount() => SaveCount = 0;
    }

    private sealed class CapturingCreatePostOutput : ICreatePostOutputBoundary
    {
        public CreatePostResponse? Response { get; private set; }

        public void Present(CreatePostResponse response) => Response = response;
    }
}
