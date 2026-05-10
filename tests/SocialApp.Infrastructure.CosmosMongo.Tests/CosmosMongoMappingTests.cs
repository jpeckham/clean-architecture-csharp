using FluentAssertions;
using SocialApp.Infrastructure.CosmosMongo.Documents;
using SocialApp.Post.Entities;
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
}
