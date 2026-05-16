using FluentAssertions;
using MongoDB.Bson;
using SocialApp.Infrastructure.CosmosMongo.Documents;
using SocialApp.Infrastructure.CosmosMongo.Gateways;
using SocialApp.Post.Controllers;
using SocialApp.Post.Entities;
using SocialApp.Post.Gateways;
using SocialApp.Post.Presenters;
using SocialApp.Post.UseCases;
using SocialApp.User.Entities;
using SocialApp.User.Gateways;
using Xunit;

namespace SocialApp.Infrastructure.CosmosMongo.Tests;

public sealed class CosmosMongoMappingTests
{
    private static readonly ICredentialsGateway Credentials = new Pbkdf2CredentialsGateway();

    [Fact]
    public void Documents_with_guid_fields_can_be_serialized_by_mongo_driver()
    {
        var user = CosmosMongoMappers.ToDocument(UserAccount.CreateWithCredentials("Ada Lovelace", "@ada", "ada@example.com", Credentials.Create("Correct9!")));
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
        var user = UserAccount.CreateWithCredentials("Ada Lovelace", "@ada", "ada@example.com", Credentials.Create("Correct9!"));

        var document = CosmosMongoMappers.ToDocument(user);
        var entity = CosmosMongoMappers.ToEntity(document);

        document.PasswordHash.Should().NotBe("Correct9!");
        document.PasswordHash.Should().StartWith("PBKDF2$");
        entity.Id.Should().Be(user.Id);
        entity.DisplayName.Should().Be("Ada Lovelace");
        entity.Handle.Should().Be("ada");
        entity.Email.Should().Be("ada@example.com");
        Credentials.Matches("Correct9!", entity.Credentials).Should().BeTrue();
    }

    [Fact]
    public void User_documents_round_trip_profile_image_metadata()
    {
        var uploadedAt = DateTimeOffset.Parse("2026-05-11T10:15:30+00:00");
        var profileImage = new ProfileImage(
            Guid.NewGuid(),
            "profile-images/ada/avatar.jpg",
            "image/jpeg",
            2048,
            320,
            240,
            uploadedAt);
        var user = UserAccount.Rehydrate(Guid.NewGuid(), "Ada Lovelace", "@ada", "ada@example.com", "credentials", profileImage);

        var document = CosmosMongoMappers.ToDocument(user);
        var entity = CosmosMongoMappers.ToEntity(document);

        document.ProfileImage.Should().NotBeNull();
        document.ProfileImage!.AssetId.Should().Be(profileImage.AssetId);
        document.ProfileImage.StorageKey.Should().Be("profile-images/ada/avatar.jpg");
        document.ProfileImage.ContentType.Should().Be("image/jpeg");
        document.ProfileImage.ByteLength.Should().Be(2048);
        document.ProfileImage.Width.Should().Be(320);
        document.ProfileImage.Height.Should().Be(240);
        document.ProfileImage.UploadedAt.Should().Be(uploadedAt);
        entity.ProfileImage.Should().NotBeNull();
        entity.ProfileImage!.AssetId.Should().Be(profileImage.AssetId);
        entity.ProfileImage.StorageKey.Should().Be("profile-images/ada/avatar.jpg");
        entity.ProfileImage.ContentType.Should().Be("image/jpeg");
        entity.ProfileImage.ByteLength.Should().Be(2048);
        entity.ProfileImage.Width.Should().Be(320);
        entity.ProfileImage.Height.Should().Be(240);
        entity.ProfileImage.UploadedAt.Should().Be(uploadedAt);
    }

    [Fact]
    public void User_documents_without_profile_image_map_to_entities_without_profile_image()
    {
        var document = new UserDocument
        {
            Id = Guid.NewGuid(),
            DisplayName = "Ada Lovelace",
            Handle = "@ada",
            Email = "ada@example.com",
            PasswordHash = "hash"
        };

        var entity = CosmosMongoMappers.ToEntity(document);

        entity.ProfileImage.Should().BeNull();
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
        entity.AuthorHandle.Should().Be("ada");
        entity.Content.Should().Be("Hello from Cosmos");
        entity.CreatedAt.Should().Be(post.CreatedAt);
        entity.IsDeleted.Should().BeTrue();
        entity.LikedBy.Should().ContainSingle("@grace");
    }

    [Fact]
    public void Post_documents_round_trip_media_only_posts_to_entities()
    {
        var media = new PostMediaItem(
            Guid.NewGuid(),
            PostMediaKind.Image,
            "post-media/ada/image.jpg",
            "image/jpeg",
            1234,
            640,
            480,
            null,
            0,
            null,
            "Ada diagram");
        var post = SocialPost.Create("@ada", "", new[] { media });

        var document = CosmosMongoMappers.ToDocument(post);
        var entity = CosmosMongoMappers.ToEntity(document);

        entity.Content.Should().BeEmpty();
        entity.Media.Should().ContainSingle(item =>
            item.AssetId == media.AssetId &&
            item.Kind == PostMediaKind.Image &&
            item.ContentType == "image/jpeg" &&
            item.AltText == "Ada diagram");
    }

    [Fact]
    public void Post_documents_round_trip_all_media_metadata()
    {
        var media = new PostMediaItem(
            Guid.NewGuid(),
            PostMediaKind.Video,
            "post-media/ada/video.mp4",
            "video/mp4",
            4096,
            1920,
            1080,
            12_000,
            1,
            "post-media/ada/video-thumb.jpg",
            "Ada demo video");
        var post = SocialPost.Create("@ada", "Video demo", new[] { media });

        var document = CosmosMongoMappers.ToDocument(post);
        var entity = CosmosMongoMappers.ToEntity(document);

        document.Media.Should().ContainSingle();
        document.Media[0].AssetId.Should().Be(media.AssetId);
        document.Media[0].Kind.Should().Be("Video");
        document.Media[0].StorageKey.Should().Be("post-media/ada/video.mp4");
        document.Media[0].ContentType.Should().Be("video/mp4");
        document.Media[0].ByteLength.Should().Be(4096);
        document.Media[0].Width.Should().Be(1920);
        document.Media[0].Height.Should().Be(1080);
        document.Media[0].DurationMs.Should().Be(12_000);
        document.Media[0].SortOrder.Should().Be(1);
        document.Media[0].ThumbnailKey.Should().Be("post-media/ada/video-thumb.jpg");
        document.Media[0].AltText.Should().Be("Ada demo video");
        entity.Media.Should().ContainSingle(item =>
            item.AssetId == media.AssetId &&
            item.Kind == PostMediaKind.Video &&
            item.StorageKey == "post-media/ada/video.mp4" &&
            item.ContentType == "video/mp4" &&
            item.ByteLength == 4096 &&
            item.Width == 1920 &&
            item.Height == 1080 &&
            item.DurationMs == 12_000 &&
            item.SortOrder == 1 &&
            item.ThumbnailKey == "post-media/ada/video-thumb.jpg" &&
            item.AltText == "Ada demo video");
    }

    [Fact]
    public void Post_documents_with_null_media_map_to_entities_with_empty_media()
    {
        var document = new PostDocument
        {
            Id = Guid.NewGuid(),
            AuthorHandle = "@ada",
            Content = "Text-only post",
            CreatedAt = DateTimeOffset.UtcNow,
            LikedBy = Array.Empty<string>(),
            Media = null!
        };

        var entity = CosmosMongoMappers.ToEntity(document);

        entity.Media.Should().BeEmpty();
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

        public int CountReplies(Guid parentPostId) =>
            _posts.Values
                .Select(CosmosMongoMappers.ToEntity)
                .Count(post => !post.IsDeleted && post.ParentPostId == parentPostId);

        public IReadOnlyList<SocialPost> RecentReplies(Guid parentPostId, int limit) =>
            _posts.Values
                .Select(CosmosMongoMappers.ToEntity)
                .Where(post => !post.IsDeleted && post.ParentPostId == parentPostId)
                .OrderByDescending(post => post.CreatedAt)
                .Take(limit)
                .ToArray();

        public IReadOnlyList<SocialPost> ScrollFor(string readerHandle, int limit) =>
            _posts.Values
                .Select(CosmosMongoMappers.ToEntity)
                .Where(post => !post.IsDeleted)
                .OrderByDescending(post => post.CreatedAt)
                .Take(limit)
                .ToArray();

        public IReadOnlyList<SocialPost> RecentByAuthor(string authorHandle, int limit) =>
            _posts.Values
                .Select(CosmosMongoMappers.ToEntity)
                .Where(post => !post.IsDeleted)
                .Where(post => string.Equals(post.AuthorHandle, authorHandle, StringComparison.OrdinalIgnoreCase))
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


