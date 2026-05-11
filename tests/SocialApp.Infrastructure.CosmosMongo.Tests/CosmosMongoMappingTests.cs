using FluentAssertions;
using MongoDB.Bson;
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
    public void Documents_with_guid_fields_can_be_serialized_by_mongo_driver()
    {
        var user = CosmosMongoMappers.ToDocument(UserAccount.Create("Ada Lovelace", "@ada", "ada@example.com", "Correct9!"));
        var post = CosmosMongoMappers.ToDocument(SocialPost.Create("@ada", "Hello from Cosmos"));
        var session = new SessionDocument
        {
            Token = "token",
            UserId = user.Id,
            Handle = user.Handle,
            CreatedAt = DateTimeOffset.UtcNow
        };

        Action serialize = () =>
        {
            _ = user.ToBson();
            _ = post.ToBson();
            _ = session.ToBson();
        };

        serialize.Should().NotThrow();
    }

    [Fact]
    public void User_documents_round_trip_to_entities()
    {
        var user = UserAccount.Create("Ada Lovelace", "@ada", "ada@example.com", "Correct9!");

        var document = CosmosMongoMappers.ToDocument(user);
        var entity = CosmosMongoMappers.ToEntity(document);

        document.PasswordHash.Should().NotBe("Correct9!");
        document.PasswordHash.Should().StartWith("PBKDF2$");
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

        public SocialPost? FindActiveRepost(Guid originalPostId, string authorHandle) =>
            _posts.Values
                .Select(CosmosMongoMappers.ToEntity)
                .SingleOrDefault(post =>
                    !post.IsDeleted &&
                    post.OriginalPostId == originalPostId &&
                    string.Equals(post.AuthorHandle, authorHandle, StringComparison.OrdinalIgnoreCase));

        public int CountActiveReposts(Guid originalPostId) =>
            _posts.Values
                .Select(CosmosMongoMappers.ToEntity)
                .Count(post => !post.IsDeleted && post.OriginalPostId == originalPostId);

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
