using FluentAssertions;
using SocialApp.Post.Controllers;
using SocialApp.Post.Entities;
using SocialApp.Post.Gateways;
using SocialApp.Post.Presenters;
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
    public void Scroll_search_follow_and_block_posts_filter_feed()
    {
        var posts = new InMemoryPostGateway();
        var search = new InMemoryPostSearchGateway(posts);
        var ada = posts.Save(SocialPost.Create("@ada", "math notes"));
        posts.Save(SocialPost.Create("@grace", "compiler notes"));

        new FollowUserPostsController(new FollowUserPostsInteractor(posts, new FollowUserPostsPresenter()))
            .Follow("@reader", "@ada");
        var feedPresenter = new ScrollPostsPresenter();
        new ScrollPostsController(new ScrollPostsInteractor(posts, feedPresenter)).Scroll("@reader", 10);
        feedPresenter.ViewModel!.Posts.Should().ContainSingle(p => p.Id == ada.Id);

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
        var posts = new InMemoryPostGateway();
        var original = posts.Save(SocialPost.Create("@ada", "root"));

        new AddLikeToPostController(new AddLikeToPostInteractor(posts, new AddLikeToPostPresenter()))
            .AddLike(original.Id, "@grace");
        posts.FindById(original.Id)!.LikedBy.Should().Contain("@grace");

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
}
