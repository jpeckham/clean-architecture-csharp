using FluentAssertions;
using SocialApp.Infrastructure.CosmosMongo.Documents;
using SocialApp.Post.Controllers;
using SocialApp.Post.Entities;
using SocialApp.Post.Gateways;
using SocialApp.Post.Presenters;
using SocialApp.Post.UseCases;
using SocialApp.User.Entities;
using Xunit;

namespace SocialApp.Infrastructure.CosmosMongo.Tests;

public sealed class CosmosMongoMappingTests
{
    [Fact]
    public void User_documents_round_trip_to_entities()
    {
        var user = UserAccount.Create("Ada Lovelace", "@ada", "ada@example.com", "Correct9!");

        var document = CosmosMongoMappers.ToDocument(user);
        var entity = CosmosMongoMappers.ToEntity(document);

        entity.Id.Should().Be(user.Id);
        entity.DisplayName.Should().Be("Ada Lovelace");
        entity.Handle.Should().Be("@ada");
        entity.Email.Should().Be("ada@example.com");
        entity.CheckPassword("Correct9!").Should().BeTrue();
    }

    [Fact]
    public void Post_documents_round_trip_to_entities()
    {
        var post = SocialPost.Create("@ada", "Hello from Cosmos");
        post.AddLike("@grace");
        post.DeleteBy("@ada");

        var document = CosmosMongoMappers.ToDocument(post);
        var entity = CosmosMongoMappers.ToEntity(document);

        entity.Id.Should().Be(post.Id);
        entity.AuthorHandle.Should().Be("@ada");
        entity.Content.Should().Be("Hello from Cosmos");
        entity.CreatedAt.Should().Be(post.CreatedAt);
        entity.IsDeleted.Should().BeTrue();
        entity.LikedBy.Should().ContainSingle("@grace");
    }

    [Fact]
    public void Delete_post_interactor_persists_deleted_state_through_gateway()
    {
        var posts = new DocumentBackedPostGateway();
        var original = posts.Save(SocialPost.Create("@ada", "delete me"));
        var presenter = new DeletePostPresenter();
        var controller = new DeletePostController(new DeletePostInteractor(posts, presenter));

        controller.Delete(original.Id, "@ada");

        presenter.ViewModel!.Succeeded.Should().BeTrue();
        posts.ScrollFor("@ada", 10).Should().BeEmpty();
    }

    private sealed class DocumentBackedPostGateway : IPostGateway
    {
        private readonly Dictionary<Guid, PostDocument> _posts = new();

        public SocialPost Save(SocialPost post)
        {
            _posts[post.Id] = CosmosMongoMappers.ToDocument(post);
            return post;
        }

        public SocialPost? FindById(Guid id) =>
            _posts.TryGetValue(id, out var post)
                ? CosmosMongoMappers.ToEntity(post)
                : null;

        public IReadOnlyList<SocialPost> ScrollFor(string readerHandle, int limit) =>
            _posts.Values
                .Select(CosmosMongoMappers.ToEntity)
                .Where(post => !post.IsDeleted)
                .OrderByDescending(post => post.CreatedAt)
                .Take(limit)
                .ToArray();

        public void Follow(string readerHandle, string followedHandle)
        {
        }

        public void Block(string readerHandle, string blockedHandle)
        {
        }
    }
}
