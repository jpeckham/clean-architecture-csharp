using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SocialApp.Infrastructure.LocalStorage.Gateways;
using SocialApp.Infrastructure.LocalStorage.Options;
using SocialApp.Post.Gateways;
using SocialApp.User.Gateways;
using Xunit;

namespace SocialApp.Api.Tests;

public sealed class SocialAppApiSliceTests
{
    [Fact]
    public async Task Authenticated_user_can_like_and_unlike_their_own_like()
    {
        await using var factory = CreateInMemoryFactory();
        var client = factory.CreateClient();
        var ada = await CreateAccountAsync(client, "@ada", "ada@example.com");
        var grace = await CreateAccountAsync(client, "@grace", "grace@example.com");
        client.DefaultRequestHeaders.Authorization = new("Bearer", ada.SessionToken);
        var createResponse = await client.PostAsJsonAsync("/api/posts", new { content = "Like through API" });
        var created = await createResponse.Content.ReadFromJsonAsync<CreatePostResult>();

        client.DefaultRequestHeaders.Authorization = new("Bearer", grace.SessionToken);
        var likeResponse = await client.PostAsync($"/api/posts/{created!.Id}/likes", null);

        likeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var graceFeed = await client.GetFromJsonAsync<RecentPostsResult>("/api/posts/recent?readerHandle=@grace&limit=20");
        graceFeed!.Posts.Should().ContainSingle(p =>
            p.Id == created.Id &&
            p.LikeCount == 1 &&
            p.LikedByCurrentReader);
        client.DefaultRequestHeaders.Authorization = new("Bearer", ada.SessionToken);
        var adaFeed = await client.GetFromJsonAsync<RecentPostsResult>("/api/posts/recent?readerHandle=@ada&limit=20");
        adaFeed!.Posts.Should().ContainSingle(p =>
            p.Id == created.Id &&
            p.LikeCount == 1 &&
            !p.LikedByCurrentReader);

        client.DefaultRequestHeaders.Authorization = new("Bearer", grace.SessionToken);
        var unlikeResponse = await client.DeleteAsync($"/api/posts/{created.Id}/likes");

        unlikeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        graceFeed = await client.GetFromJsonAsync<RecentPostsResult>("/api/posts/recent?readerHandle=@grace&limit=20");
        graceFeed!.Posts.Should().ContainSingle(p =>
            p.Id == created.Id &&
            p.LikeCount == 0 &&
            !p.LikedByCurrentReader);
    }

    [Fact]
    public async Task Authenticated_user_can_create_and_delete_quote_repost_through_api()
    {
        await using var factory = CreateInMemoryFactory();
        var client = factory.CreateClient();
        var ada = await CreateAccountAsync(client, "@ada", "ada@example.com");
        var grace = await CreateAccountAsync(client, "@grace", "grace@example.com");
        var linus = await CreateAccountAsync(client, "@linus", "linus@example.com");
        client.DefaultRequestHeaders.Authorization = new("Bearer", ada.SessionToken);
        var createResponse = await client.PostAsJsonAsync("/api/posts", new { content = "Original API post" });
        var original = await createResponse.Content.ReadFromJsonAsync<CreatePostResult>();
        var originalId = original!.Id!.Value;

        client.DefaultRequestHeaders.Authorization = new("Bearer", grace.SessionToken);
        var repostResponse = await client.PostAsJsonAsync($"/api/posts/{originalId}/reposts", new { content = "Grace quote" });

        repostResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var repost = await repostResponse.Content.ReadFromJsonAsync<CreatePostResult>();
        var repostId = repost!.Id!.Value;
        repostId.Should().NotBe(originalId);

        var graceFeed = await client.GetFromJsonAsync<RecentPostsResult>("/api/posts/recent?readerHandle=@grace&limit=20");
        var originalView = graceFeed!.Posts.Single(p => p.Id == originalId);
        originalView.RepostCount.Should().Be(1);
        originalView.RepostedByCurrentReader.Should().BeTrue();

        var repostView = graceFeed.Posts.Single(p => p.Id == repostId);
        repostView.Content.Should().Be("Grace quote");
        repostView.OriginalPostId.Should().Be(originalId);
        repostView.QuotedPost.Should().NotBeNull();
        repostView.QuotedPost!.AuthorHandle.Should().Be("ada");
        repostView.QuotedPost.Content.Should().Be("Original API post");

        var duplicateRepostResponse = await client.PostAsJsonAsync($"/api/posts/{originalId}/reposts", new { content = "Grace duplicate" });
        duplicateRepostResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);

        client.DefaultRequestHeaders.Authorization = new("Bearer", linus.SessionToken);
        var repostOfRepostResponse = await client.PostAsJsonAsync($"/api/posts/{repostId}/reposts", new { content = "Linus quote" });
        repostOfRepostResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var repostOfRepost = await repostOfRepostResponse.Content.ReadFromJsonAsync<CreatePostResult>();
        var linusFeed = await client.GetFromJsonAsync<RecentPostsResult>("/api/posts/recent?readerHandle=@linus&limit=20");
        linusFeed!.Posts.Single(p => p.Id == repostOfRepost!.Id).OriginalPostId.Should().Be(originalId);

        client.DefaultRequestHeaders.Authorization = new("Bearer", grace.SessionToken);
        var deleteRepostResponse = await client.DeleteAsync($"/api/posts/{originalId}/reposts/mine");

        deleteRepostResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        graceFeed = await client.GetFromJsonAsync<RecentPostsResult>("/api/posts/recent?readerHandle=@grace&limit=20");
        graceFeed!.Posts.Should().NotContain(p => p.Id == repostId);
        graceFeed.Posts.Single(p => p.Id == originalId).RepostedByCurrentReader.Should().BeFalse();
        graceFeed.Posts.Single(p => p.Id == originalId).RepostCount.Should().Be(1);

        var secondRepostResponse = await client.PostAsJsonAsync($"/api/posts/{originalId}/reposts", new { content = "Grace again" });
        secondRepostResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var secondGraceRepost = await secondRepostResponse.Content.ReadFromJsonAsync<CreatePostResult>();
        secondGraceRepost!.Id.Should().NotBe(repostId);
    }

    [Fact]
    public async Task Post_reads_include_current_profile_image_for_originals_reposts_quotes_and_replies()
    {
        await using var factory = CreateInMemoryFactory();
        var client = factory.CreateClient();
        var ada = await CreateAccountAsync(client, "@ada", "ada@example.com");
        var grace = await CreateAccountAsync(client, "@grace", "grace@example.com");
        var linus = await CreateAccountAsync(client, "@linus", "linus@example.com");

        client.DefaultRequestHeaders.Authorization = new("Bearer", ada.SessionToken);
        var originalResponse = await client.PostAsJsonAsync("/api/posts", new { content = "Original before avatar" });
        var original = await originalResponse.Content.ReadFromJsonAsync<CreatePostResult>();

        client.DefaultRequestHeaders.Authorization = new("Bearer", grace.SessionToken);
        var replyResponse = await client.PostAsJsonAsync($"/api/posts/{original!.Id}/replies", new { content = "Reply before avatar" });
        var reply = await replyResponse.Content.ReadFromJsonAsync<CreatePostResult>();

        client.DefaultRequestHeaders.Authorization = new("Bearer", linus.SessionToken);
        var repostResponse = await client.PostAsJsonAsync($"/api/posts/{original.Id}/reposts", new { content = "Quote before avatar" });
        var repost = await repostResponse.Content.ReadFromJsonAsync<CreatePostResult>();

        var adaImageUrl = await UploadProfileImageAsync(client, ada.SessionToken!);
        var graceImageUrl = await UploadProfileImageAsync(client, grace.SessionToken!);
        var linusImageUrl = await UploadProfileImageAsync(client, linus.SessionToken!);

        client.DefaultRequestHeaders.Authorization = new("Bearer", ada.SessionToken);
        var feed = await client.GetFromJsonAsync<RecentPostsResult>("/api/posts/recent?limit=20");

        var originalView = feed!.Posts.Single(p => p.Id == original.Id);
        originalView.AuthorProfileImageUrl.Should().Be(adaImageUrl);
        originalView.RecentReplies.Should().ContainSingle(p =>
            p.Id == reply!.Id &&
            p.AuthorProfileImageUrl == graceImageUrl);

        var repostView = feed.Posts.Single(p => p.Id == repost!.Id);
        repostView.AuthorProfileImageUrl.Should().Be(linusImageUrl);
        repostView.QuotedPost.Should().NotBeNull();
        repostView.QuotedPost!.AuthorProfileImageUrl.Should().Be(adaImageUrl);
    }

    [Fact]
    public async Task Like_endpoints_require_valid_bearer_token()
    {
        await using var factory = CreateInMemoryFactory();
        var client = factory.CreateClient();

        var missingLikeTokenResponse = await client.PostAsync($"/api/posts/{Guid.NewGuid()}/likes", null);
        var missingUnlikeTokenResponse = await client.DeleteAsync($"/api/posts/{Guid.NewGuid()}/likes");
        client.DefaultRequestHeaders.Authorization = new("Bearer", "missing-token");
        var invalidLikeTokenResponse = await client.PostAsync($"/api/posts/{Guid.NewGuid()}/likes", null);
        var invalidUnlikeTokenResponse = await client.DeleteAsync($"/api/posts/{Guid.NewGuid()}/likes");

        missingLikeTokenResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        missingUnlikeTokenResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        invalidLikeTokenResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        invalidUnlikeTokenResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task User_cannot_delete_another_users_like()
    {
        await using var factory = CreateInMemoryFactory();
        var client = factory.CreateClient();
        var ada = await CreateAccountAsync(client, "@ada", "ada@example.com");
        var grace = await CreateAccountAsync(client, "@grace", "grace@example.com");
        client.DefaultRequestHeaders.Authorization = new("Bearer", ada.SessionToken);
        var createResponse = await client.PostAsJsonAsync("/api/posts", new { content = "Grace likes this" });
        var created = await createResponse.Content.ReadFromJsonAsync<CreatePostResult>();

        client.DefaultRequestHeaders.Authorization = new("Bearer", grace.SessionToken);
        var likeResponse = await client.PostAsync($"/api/posts/{created!.Id}/likes", null);
        likeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        client.DefaultRequestHeaders.Authorization = new("Bearer", ada.SessionToken);
        var unlikeResponse = await client.DeleteAsync($"/api/posts/{created.Id}/likes");

        unlikeResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        client.DefaultRequestHeaders.Authorization = new("Bearer", grace.SessionToken);
        var graceFeed = await client.GetFromJsonAsync<RecentPostsResult>("/api/posts/recent?readerHandle=@grace&limit=20");
        graceFeed!.Posts.Should().ContainSingle(p =>
            p.Id == created.Id &&
            p.LikeCount == 1 &&
            p.LikedByCurrentReader);
    }

    [Fact]
    public async Task Creator_can_delete_post_through_api_and_remove_it_from_recent_posts()
    {
        await using var factory = CreateInMemoryFactory();
        var client = factory.CreateClient();
        var session = await CreateAccountAsync(client, "@ada", "ada@example.com");
        client.DefaultRequestHeaders.Authorization = new("Bearer", session.SessionToken);
        var createResponse = await client.PostAsJsonAsync("/api/posts", new { content = "Delete through API" });
        var created = await createResponse.Content.ReadFromJsonAsync<CreatePostResult>();

        var deleteResponse = await client.DeleteAsync($"/api/posts/{created!.Id}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await deleteResponse.Content.ReadFromJsonAsync<SimpleResult>();
        result!.Succeeded.Should().BeTrue();
        var recentPosts = await client.GetFromJsonAsync<RecentPostsResult>("/api/posts/recent?readerHandle=@ada&limit=20");
        recentPosts!.Posts.Should().NotContain(p => p.Id == created.Id);
    }

    [Fact]
    public async Task Search_posts_returns_matching_content_with_reader_state()
    {
        await using var factory = CreateInMemoryFactory();
        var client = factory.CreateClient();
        var ada = await CreateAccountAsync(client, "@ada", "ada@example.com");
        var grace = await CreateAccountAsync(client, "@grace", "grace@example.com");
        client.DefaultRequestHeaders.Authorization = new("Bearer", ada.SessionToken);
        var compilerResponse = await client.PostAsJsonAsync("/api/posts", new { content = "Compiler notes from Ada" });
        await client.PostAsJsonAsync("/api/posts", new { content = "Math notes from Ada" });
        var compilerPost = await compilerResponse.Content.ReadFromJsonAsync<CreatePostResult>();
        client.DefaultRequestHeaders.Authorization = new("Bearer", grace.SessionToken);
        await client.PostAsync($"/api/posts/{compilerPost!.Id}/likes", null);

        var result = await client.GetFromJsonAsync<RecentPostsResult>("/api/posts/search?readerHandle=@ada&query=compiler");

        result!.Posts.Should().ContainSingle(p =>
            p.Id == compilerPost.Id &&
            p.Content == "Compiler notes from Ada" &&
            p.LikeCount == 1 &&
            p.LikedByCurrentReader);
        result.Posts.Should().NotContain(p => p.Content.Contains("Math", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Recent_posts_include_hashtag_segment_metadata()
    {
        await using var factory = CreateInMemoryFactory();
        var client = factory.CreateClient();
        var ada = await CreateAccountAsync(client, "@ada", "ada@example.com");
        client.DefaultRequestHeaders.Authorization = new("Bearer", ada.SessionToken);
        var createResponse = await client.PostAsJsonAsync("/api/posts", new { content = "Hello #amazing today" });
        var created = await createResponse.Content.ReadFromJsonAsync<CreatePostResult>();

        var result = await client.GetFromJsonAsync<RecentPostsResult>("/api/posts/recent?readerHandle=@ada&limit=20");

        var post = result!.Posts.Single(p => p.Id == created!.Id);
        post.ContentSegments.Should().ContainSingle(s =>
            s.Text == "#amazing" &&
            s.HashtagText == "amazing" &&
            s.MentionHandle == null);
    }

    [Fact]
    public async Task Individual_post_endpoint_returns_one_post_with_reader_state()
    {
        await using var factory = CreateInMemoryFactory();
        var client = factory.CreateClient();
        var ada = await CreateAccountAsync(client, "@ada", "ada@example.com");
        var grace = await CreateAccountAsync(client, "@grace", "grace@example.com");
        client.DefaultRequestHeaders.Authorization = new("Bearer", ada.SessionToken);
        var createResponse = await client.PostAsJsonAsync("/api/posts", new { content = "Deep link target #share" });
        var created = await createResponse.Content.ReadFromJsonAsync<CreatePostResult>();
        var postId = created!.Id!.Value;
        client.DefaultRequestHeaders.Authorization = new("Bearer", grace.SessionToken);
        await client.PostAsync($"/api/posts/{postId}/likes", null);

        var result = await client.GetFromJsonAsync<IndividualPostResult>($"/api/posts/{postId}");

        result!.Succeeded.Should().BeTrue();
        result.Post.Should().NotBeNull();
        result.Post!.Id.Should().Be(postId);
        result.Post.Content.Should().Be("Deep link target #share");
        result.Post.LikeCount.Should().Be(1);
        result.Post.LikedByCurrentReader.Should().BeTrue();
        result.Post.ContentSegments.Should().ContainSingle(s => s.Text == "#share" && s.HashtagText == "share");
    }

    [Fact]
    public async Task Individual_post_endpoint_returns_not_found_for_missing_or_deleted_posts()
    {
        await using var factory = CreateInMemoryFactory();
        var client = factory.CreateClient();
        var ada = await CreateAccountAsync(client, "@ada", "ada@example.com");
        client.DefaultRequestHeaders.Authorization = new("Bearer", ada.SessionToken);
        var createResponse = await client.PostAsJsonAsync("/api/posts", new { content = "Delete from deep link" });
        var created = await createResponse.Content.ReadFromJsonAsync<CreatePostResult>();

        var missingResponse = await client.GetAsync($"/api/posts/{Guid.NewGuid()}");
        await client.DeleteAsync($"/api/posts/{created!.Id}");
        var deletedResponse = await client.GetAsync($"/api/posts/{created.Id}");

        missingResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        deletedResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Individual_post_endpoint_requires_valid_bearer_token()
    {
        await using var factory = CreateInMemoryFactory();
        var client = factory.CreateClient();

        var missingTokenResponse = await client.GetAsync($"/api/posts/{Guid.NewGuid()}");
        client.DefaultRequestHeaders.Authorization = new("Bearer", "missing-token");
        var invalidTokenResponse = await client.GetAsync($"/api/posts/{Guid.NewGuid()}");

        missingTokenResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        invalidTokenResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task View_user_posts_include_hashtag_segment_metadata()
    {
        await using var factory = CreateInMemoryFactory();
        var client = factory.CreateClient();
        var ada = await CreateAccountAsync(client, "@ada", "ada@example.com");
        client.DefaultRequestHeaders.Authorization = new("Bearer", ada.SessionToken);
        var createResponse = await client.PostAsJsonAsync("/api/posts", new { content = "Profile #amazing today" });
        var created = await createResponse.Content.ReadFromJsonAsync<CreatePostResult>();

        var result = await client.GetFromJsonAsync<UserProfileResult>("/api/users/%40ada");

        var post = result!.Posts.Single(p => p.Id == created!.Id);
        post.ContentSegments.Should().ContainSingle(s =>
            s.Text == "#amazing" &&
            s.HashtagText == "amazing" &&
            s.MentionHandle == null);
    }

    [Fact]
    public async Task View_user_returns_display_name_handle_and_recent_posts_for_authenticated_reader()
    {
        await using var factory = CreateInMemoryFactory();
        var client = factory.CreateClient();
        var ada = await CreateAccountAsync(client, "@ada", "ada@example.com", "Ada Lovelace");
        var grace = await CreateAccountAsync(client, "@grace", "grace@example.com", "Grace Hopper");
        client.DefaultRequestHeaders.Authorization = new("Bearer", ada.SessionToken);
        var adaPostResponse = await client.PostAsJsonAsync("/api/posts", new { content = "Ada profile post" });
        var adaPost = await adaPostResponse.Content.ReadFromJsonAsync<CreatePostResult>();
        await client.PostAsJsonAsync("/api/posts", new { content = "Ada second profile post" });
        client.DefaultRequestHeaders.Authorization = new("Bearer", grace.SessionToken);
        await client.PostAsJsonAsync("/api/posts", new { content = "Grace profile post" });
        await client.PostAsync($"/api/posts/{adaPost!.Id}/likes", null);
        var repostResponse = await client.PostAsJsonAsync($"/api/posts/{adaPost.Id}/reposts", new { content = "Grace profile quote" });

        var result = await client.GetFromJsonAsync<UserProfileResult>("/api/users/%40ada");
        var missingResponse = await client.GetAsync("/api/users/%40missing");
        client.DefaultRequestHeaders.Authorization = null;
        var unauthenticatedResponse = await client.GetAsync("/api/users/%40ada");

        result!.Succeeded.Should().BeTrue();
        result.DisplayName.Should().Be("Ada Lovelace");
        result.Handle.Should().Be("ada");
        result.Posts.Should().HaveCount(2);
        result.Posts.Should().OnlyContain(p => p.AuthorHandle == "ada");
        result.Posts.Should().Contain(p =>
            p.Id == adaPost.Id &&
            p.Content == "Ada profile post" &&
            p.LikeCount == 1 &&
            p.LikedByCurrentReader &&
            p.RepostCount == 1 &&
            p.RepostedByCurrentReader);
        repostResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        missingResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        unauthenticatedResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Post_media_upload_session_requires_authentication()
    {
        await using var factory = CreateInMemoryFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/posts/media/upload-sessions", new
        {
            kind = "Image",
            contentType = "image/jpeg",
            byteLength = 1234
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Authenticated_user_can_complete_post_media_and_create_media_only_post()
    {
        await using var factory = CreateInMemoryFactory();
        var client = factory.CreateClient();
        var ada = await CreateAccountAsync(client, "@ada", "ada@example.com");
        client.DefaultRequestHeaders.Authorization = new("Bearer", ada.SessionToken);

        var beginResponse = await client.PostAsJsonAsync("/api/posts/media/upload-sessions", new
        {
            kind = "Image",
            contentType = "image/jpeg",
            byteLength = 1234,
            width = 640,
            height = 480,
            altText = "Ada diagram"
        });

        beginResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var upload = await beginResponse.Content.ReadFromJsonAsync<BeginMediaUploadResult>();
        upload!.AssetId.Should().NotBeNull();
        upload.UploadUrl.Should().StartWith("/api/media/uploads/post-media/");

        var uploadContent = new ByteArrayContent(new byte[] { 1, 2, 3, 4 });
        uploadContent.Headers.ContentType = new("image/jpeg");
        var storeResponse = await client.PutAsync(upload.UploadUrl, uploadContent);
        storeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var completeResponse = await client.PostAsJsonAsync($"/api/posts/media/{upload.AssetId}/complete", new { });
        completeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var createResponse = await client.PostAsJsonAsync("/api/posts", new
        {
            content = "",
            mediaAssetIds = new[] { upload.AssetId!.Value }
        });

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<CreatePostResult>();
        var recent = await client.GetFromJsonAsync<RecentPostsResult>("/api/posts/recent?limit=20");
        var post = recent!.Posts.Single(p => p.Id == created!.Id);
        post.Content.Should().BeEmpty();
        post.Media.Should().ContainSingle(m =>
            m.AssetId == upload.AssetId &&
            m.Kind == "Image" &&
            m.ContentType == "image/jpeg" &&
            m.SortOrder == 0 &&
            m.MediaUrl == $"/api/post-media/{upload.AssetId}" &&
            m.AltText == "Ada diagram");

        var mediaResponse = await client.GetAsync($"/api/post-media/{upload.AssetId}");
        mediaResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        mediaResponse.Content.Headers.ContentType!.MediaType.Should().Be("image/jpeg");
        (await mediaResponse.Content.ReadAsByteArrayAsync()).Should().Equal(1, 2, 3, 4);
    }

    [Fact]
    public async Task Profile_image_upload_session_uses_explicit_profile_image_upload_route()
    {
        await using var factory = CreateInMemoryFactory();
        var client = factory.CreateClient();
        var ada = await CreateAccountAsync(client, "@ada", "ada@example.com");
        client.DefaultRequestHeaders.Authorization = new("Bearer", ada.SessionToken);

        var beginResponse = await client.PostAsJsonAsync("/api/users/me/profile-image/upload-sessions", new
        {
            contentType = "image/jpeg",
            byteLength = 1234
        });

        beginResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var upload = await beginResponse.Content.ReadFromJsonAsync<BeginMediaUploadResult>();
        upload!.UploadUrl.Should().StartWith("/api/media/uploads/profile-images/");
    }

    [Fact]
    public async Task Store_media_upload_uses_route_target_without_falling_back_to_another_gateway()
    {
        await using var factory = CreateInMemoryFactory();
        var client = factory.CreateClient();
        var ada = await CreateAccountAsync(client, "@ada", "ada@example.com");
        client.DefaultRequestHeaders.Authorization = new("Bearer", ada.SessionToken);
        var beginResponse = await client.PostAsJsonAsync("/api/users/me/profile-image/upload-sessions", new
        {
            contentType = "image/jpeg",
            byteLength = 1234
        });
        var upload = await beginResponse.Content.ReadFromJsonAsync<BeginMediaUploadResult>();
        var content = new ByteArrayContent(new byte[] { 1, 2, 3, 4 });
        content.Headers.ContentType = new("image/jpeg");

        var response = await client.PutAsync($"/api/media/uploads/post-media/{upload!.AssetId}", content);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_post_rejects_uncompleted_or_foreign_media_asset()
    {
        await using var factory = CreateInMemoryFactory();
        var client = factory.CreateClient();
        var ada = await CreateAccountAsync(client, "@ada", "ada@example.com");
        var grace = await CreateAccountAsync(client, "@grace", "grace@example.com");
        client.DefaultRequestHeaders.Authorization = new("Bearer", ada.SessionToken);
        var beginResponse = await client.PostAsJsonAsync("/api/posts/media/upload-sessions", new
        {
            kind = "Image",
            contentType = "image/png",
            byteLength = 100
        });
        var upload = await beginResponse.Content.ReadFromJsonAsync<BeginMediaUploadResult>();

        var uncompletedResponse = await client.PostAsJsonAsync("/api/posts", new
        {
            content = "Uses pending media",
            mediaAssetIds = new[] { upload!.AssetId!.Value }
        });
        var uploadContent = new ByteArrayContent(new byte[] { 1, 2, 3, 4 });
        uploadContent.Headers.ContentType = new("image/png");
        (await client.PutAsync(upload.UploadUrl, uploadContent)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        await client.PostAsJsonAsync($"/api/posts/media/{upload.AssetId}/complete", new { });

        client.DefaultRequestHeaders.Authorization = new("Bearer", grace.SessionToken);
        var foreignResponse = await client.PostAsJsonAsync("/api/posts", new
        {
            content = "Uses foreign media",
            mediaAssetIds = new[] { upload.AssetId.Value }
        });

        uncompletedResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        foreignResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Complete_post_media_requires_owner()
    {
        await using var factory = CreateInMemoryFactory();
        var client = factory.CreateClient();
        var ada = await CreateAccountAsync(client, "@ada", "ada@example.com");
        var grace = await CreateAccountAsync(client, "@grace", "grace@example.com");
        client.DefaultRequestHeaders.Authorization = new("Bearer", ada.SessionToken);
        var beginResponse = await client.PostAsJsonAsync("/api/posts/media/upload-sessions", new
        {
            kind = "Image",
            contentType = "image/jpeg",
            byteLength = 1234
        });
        var upload = await beginResponse.Content.ReadFromJsonAsync<BeginMediaUploadResult>();

        client.DefaultRequestHeaders.Authorization = new("Bearer", grace.SessionToken);
        var uploadContent = new ByteArrayContent(new byte[] { 1, 2, 3, 4 });
        uploadContent.Headers.ContentType = new("image/jpeg");
        (await client.PutAsync(upload!.UploadUrl, uploadContent)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        var completeResponse = await client.PostAsJsonAsync($"/api/posts/media/{upload!.AssetId}/complete", new { });

        completeResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Profile_image_upload_session_rejects_non_image_content_type()
    {
        await using var factory = CreateInMemoryFactory();
        var client = factory.CreateClient();
        var ada = await CreateAccountAsync(client, "@ada", "ada@example.com");
        client.DefaultRequestHeaders.Authorization = new("Bearer", ada.SessionToken);

        var response = await client.PostAsJsonAsync("/api/users/me/profile-image/upload-sessions", new
        {
            contentType = "video/mp4",
            byteLength = 1234
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Profile_image_upload_session_complete_and_delete_updates_view_user()
    {
        await using var factory = CreateInMemoryFactory();
        var client = factory.CreateClient();
        var ada = await CreateAccountAsync(client, "@ada", "ada@example.com", "Ada Lovelace");
        client.DefaultRequestHeaders.Authorization = new("Bearer", ada.SessionToken);

        var beginResponse = await client.PostAsJsonAsync("/api/users/me/profile-image/upload-sessions", new
        {
            contentType = "image/jpeg",
            byteLength = 1234
        });
        beginResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var upload = await beginResponse.Content.ReadFromJsonAsync<BeginMediaUploadResult>();
        var uploadContent = new ByteArrayContent(new byte[] { 1, 2, 3, 4 });
        uploadContent.Headers.ContentType = new("image/jpeg");
        (await client.PutAsync(upload!.UploadUrl, uploadContent)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var completeResponse = await client.PostAsJsonAsync("/api/users/me/profile-image/complete", new
        {
            assetId = upload.AssetId!.Value,
            width = 640,
            height = 480
        });

        completeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await client.GetFromJsonAsync<UserProfileResult>("/api/users/%40ada");
        profile!.ProfileImage.Should().NotBeNull();
        profile.ProfileImage!.AssetId.Should().Be(upload.AssetId.Value);
        profile.ProfileImage.Width.Should().Be(640);

        var deleteResponse = await client.DeleteAsync("/api/users/me/profile-image");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        profile = await client.GetFromJsonAsync<UserProfileResult>("/api/users/%40ada");
        profile!.ProfileImage.Should().BeNull();
    }

    [Fact]
    public async Task Authenticated_user_can_upload_and_remove_their_own_profile_image()
    {
        await using var factory = CreateInMemoryFactory();
        var client = factory.CreateClient();
        var ada = await CreateAccountAsync(client, "@ada", "ada@example.com", "Ada Lovelace");
        client.DefaultRequestHeaders.Authorization = new("Bearer", ada.SessionToken);
        using var imageContent = new MultipartFormDataContent();
        imageContent.Add(new ByteArrayContent(new byte[] { 1, 2, 3, 4 }), "image", "avatar.jpg");
        imageContent.Last().Headers.ContentType = new("image/jpeg");

        var uploadResponse = await client.PostAsync("/api/users/me/profile-image", imageContent);

        uploadResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var uploadResult = await uploadResponse.Content.ReadFromJsonAsync<ProfileImageResult>();
        uploadResult!.Succeeded.Should().BeTrue();
        uploadResult.ProfileImage.Should().NotBeNull();
        uploadResult.ProfileImage!.ContentType.Should().Be("image/jpeg");

        var profile = await client.GetFromJsonAsync<UserProfileResult>("/api/users/%40ada");
        profile!.ProfileImage.Should().NotBeNull();
        profile.ProfileImage!.ImageUrl.Should().Be($"/api/profile-images/{uploadResult.ProfileImage.AssetId}");

        var imageResponse = await client.GetAsync(profile.ProfileImage.ImageUrl);
        imageResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        imageResponse.Content.Headers.ContentType!.MediaType.Should().Be("image/jpeg");

        var removeResponse = await client.DeleteAsync("/api/users/me/profile-image");
        removeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        profile = await client.GetFromJsonAsync<UserProfileResult>("/api/users/%40ada");
        profile!.ProfileImage.Should().BeNull();
    }

    [Fact]
    public async Task Delete_post_requires_valid_bearer_token()
    {
        await using var factory = CreateInMemoryFactory();
        var client = factory.CreateClient();

        var missingTokenResponse = await client.DeleteAsync($"/api/posts/{Guid.NewGuid()}");
        client.DefaultRequestHeaders.Authorization = new("Bearer", "missing-token");
        var invalidTokenResponse = await client.DeleteAsync($"/api/posts/{Guid.NewGuid()}");

        missingTokenResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        invalidTokenResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Non_author_cannot_delete_post_through_api()
    {
        await using var factory = CreateInMemoryFactory();
        var client = factory.CreateClient();
        var ada = await CreateAccountAsync(client, "@ada", "ada@example.com");
        var grace = await CreateAccountAsync(client, "@grace", "grace@example.com");
        client.DefaultRequestHeaders.Authorization = new("Bearer", ada.SessionToken);
        var createResponse = await client.PostAsJsonAsync("/api/posts", new { content = "Ada owns this" });
        var created = await createResponse.Content.ReadFromJsonAsync<CreatePostResult>();

        client.DefaultRequestHeaders.Authorization = new("Bearer", grace.SessionToken);
        var deleteResponse = await client.DeleteAsync($"/api/posts/{created!.Id}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var recentPosts = await client.GetFromJsonAsync<RecentPostsResult>("/api/posts/recent?readerHandle=@ada&limit=20");
        recentPosts!.Posts.Should().ContainSingle(p => p.Id == created.Id);
    }

    [Fact]
    public async Task Account_session_and_post_slice_runs_through_api()
    {
        await using var factory = CreateInMemoryFactory();

        var client = factory.CreateClient();

        var accountResponse = await client.PostAsJsonAsync("/api/accounts", new
        {
            displayName = "Ada Lovelace",
            handle = "@ada",
            email = "ada@example.com",
            password = "Correct9!"
        });

        accountResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var account = await accountResponse.Content.ReadFromJsonAsync<AuthResult>();
        account!.Succeeded.Should().BeTrue();
        account.SessionToken.Should().NotBeNullOrWhiteSpace();

        var sessionResponse = await client.PostAsJsonAsync("/api/sessions", new
        {
            email = "ada@example.com",
            password = "Correct9!"
        });

        sessionResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var session = await sessionResponse.Content.ReadFromJsonAsync<AuthResult>();
        session!.Succeeded.Should().BeTrue();

        var createPostResponse = await client.PostAsJsonAsync("/api/posts", new
        {
            content = "First post from the API"
        });

        createPostResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        client.DefaultRequestHeaders.Authorization = new("Bearer", session.SessionToken);
        createPostResponse = await client.PostAsJsonAsync("/api/posts", new
        {
            content = "First post from the API",
            authorHandle = "@spoofed"
        });

        createPostResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var recentPosts = await client.GetFromJsonAsync<RecentPostsResult>("/api/posts/recent?readerHandle=@ada&limit=20");
        recentPosts!.Posts.Should().ContainSingle(p => p.AuthorHandle == "ada" && p.Content == "First post from the API");
        recentPosts.Posts.Should().NotContain(p => p.AuthorHandle == "@spoofed");
    }

    [Fact]
    public async Task Development_email_outbox_exposes_in_memory_emails()
    {
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            {
                services.RemoveAll<IUserGateway>();
                services.AddSingleton<IUserGateway, InMemoryUserGateway>();
            }));

        var client = factory.CreateClient();

        var resetResponse = await client.PostAsJsonAsync("/api/password-reset-requests", new
        {
            email = "missing@example.com"
        });
        resetResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var registerResponse = await client.PostAsJsonAsync("/api/registrations", new
        {
            displayName = "Ada Lovelace",
            handle = "@ada-dev",
            email = "ada-dev@example.com",
            password = "Correct9!"
        });
        registerResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var emails = await client.GetFromJsonAsync<DevEmailsResult>("/dev/emails");

        emails!.Emails.Should().ContainSingle(email =>
            email.To == "ada-dev@example.com" &&
            email.Subject.Contains("Verify", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Registration_accepts_plain_handles_and_pasted_email_codes()
    {
        await using var factory = CreateInMemoryFactory();
        var client = factory.CreateClient();

        var registerResponse = await client.PostAsJsonAsync("/api/registrations", new
        {
            displayName = "Ada Lovelace",
            handle = "ada-plain",
            email = "ada-plain@example.com",
            password = "Correct9!"
        });
        registerResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var emails = await client.GetFromJsonAsync<DevEmailsResult>("/dev/emails");
        var code = Regex.Match(emails!.Emails.Single(email => email.To == "ada-plain@example.com").Body, @"\d{6}").Value;

        var verifyResponse = await client.PostAsJsonAsync("/api/registrations/verify", new
        {
            email = "ada-plain@example.com",
            code = $"{code}."
        });

        verifyResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginResponse = await client.PostAsJsonAsync("/api/sessions", new
        {
            email = "ada-plain@example.com",
            password = "Correct9!"
        });
        var login = await loginResponse.Content.ReadFromJsonAsync<AuthResult>();

        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        login!.Handle.Should().Be("ada-plain");
    }

    [Fact]
    public void Api_startup_rejects_unsupported_media_provider()
    {
        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseSetting("Media:Provider", "Unsupported"));

        var act = () => factory.CreateClient();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Unsupported media provider 'Unsupported'.");
    }

    private static WebApplicationFactory<Program> CreateInMemoryFactory() =>
        new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            {
                services.RemoveAll<IUserGateway>();
                services.RemoveAll<ISessionGateway>();
                services.RemoveAll<IPostGateway>();
                services.RemoveAll<IPostSearchGateway>();
                services.RemoveAll<IPostMediaStorageGateway>();
                services.RemoveAll<IProfileImageStorageGateway>();
                services.AddSingleton<IUserGateway, InMemoryUserGateway>();
                services.AddSingleton<ISessionGateway, InMemorySessionGateway>();
                services.AddSingleton<InMemoryPostGateway>();
                services.AddSingleton<IPostGateway>(sp => sp.GetRequiredService<InMemoryPostGateway>());
                services.AddSingleton<IPostSearchGateway, InMemoryPostSearchGateway>();
                var rootPath = Path.Combine(Path.GetTempPath(), "socialapp-api-tests", Guid.NewGuid().ToString("N"));
                services.AddSingleton(Microsoft.Extensions.Options.Options.Create(new LocalMediaStorageOptions { RootPath = rootPath }));
                services.AddSingleton<IPostMediaStorageGateway, FileSystemPostMediaStorageGateway>();
                services.AddSingleton<IProfileImageStorageGateway, FileSystemProfileImageStorageGateway>();
            }));

    private static async Task<AuthResult> CreateAccountAsync(HttpClient client, string handle, string email, string? displayName = null)
    {
        var response = await client.PostAsJsonAsync("/api/accounts", new
        {
            displayName = displayName ?? handle.TrimStart('@'),
            handle,
            email,
            password = "Correct9!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<AuthResult>())!;
    }

    private static async Task<string> UploadProfileImageAsync(HttpClient client, string sessionToken)
    {
        client.DefaultRequestHeaders.Authorization = new("Bearer", sessionToken);
        using var imageContent = new MultipartFormDataContent();
        imageContent.Add(new ByteArrayContent(new byte[] { 1, 2, 3, 4 }), "image", "avatar.jpg");
        imageContent.Last().Headers.ContentType = new("image/jpeg");

        var uploadResponse = await client.PostAsync("/api/users/me/profile-image", imageContent);
        uploadResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await uploadResponse.Content.ReadFromJsonAsync<ProfileImageResult>();
        result!.ProfileImage.Should().NotBeNull();
        return result.ProfileImage!.ImageUrl;
    }

    private sealed record AuthResult(bool Succeeded, string Message, string? Handle, string? SessionToken);
    private sealed record CreatePostResult(bool Succeeded, string Message, Guid? Id, string? AuthorHandle);
    private sealed record SimpleResult(bool Succeeded, string Message);
    private sealed record DevEmailsResult(IReadOnlyList<DevEmailResult> Emails);
    private sealed record DevEmailResult(string To, string Subject, string Body);
    private sealed record BeginMediaUploadResult(bool Succeeded, string Message, Guid? AssetId, string? UploadUrl);
    private sealed record ProfileImageResult(bool Succeeded, string Message, ProfileImageSummaryResult? ProfileImage);
    private sealed record ProfileImageSummaryResult(Guid AssetId, string ContentType, long ByteLength, int? Width, int? Height, string ImageUrl);
    private sealed record UserProfileResult(bool Succeeded, string Message, string? Handle, string? DisplayName, ProfileImageSummaryResult? ProfileImage, IReadOnlyList<PostSummaryResult> Posts);
    private sealed record RecentPostsResult(IReadOnlyList<PostSummaryResult> Posts);
    private sealed record IndividualPostResult(bool Succeeded, string Message, PostSummaryResult? Post);
    private sealed record QuotedPostSummaryResult(Guid Id, string AuthorHandle, string Content, string? AuthorProfileImageUrl = null);
    private sealed record PostMediaSummaryResult(Guid AssetId, string Kind, string ContentType, long ByteLength, int? Width, int? Height, long? DurationMs, int SortOrder, string? ThumbnailKey, string? AltText, string? MediaUrl);
    private sealed record PostContentSegmentResult(int Sequence, string Text, string? MentionHandle, string? HashtagText = null);
    private sealed record PostSummaryResult(
        Guid Id,
        string AuthorHandle,
        IReadOnlyList<PostContentSegmentResult> ContentSegments,
        Guid? ParentPostId,
        Guid? OriginalPostId,
        int LikeCount,
        bool LikedByCurrentReader,
        int RepostCount,
        bool RepostedByCurrentReader,
        QuotedPostSummaryResult? QuotedPost,
        IReadOnlyList<PostMediaSummaryResult> Media,
        string? AuthorProfileImageUrl = null,
        int ReplyCount = 0,
        IReadOnlyList<PostSummaryResult>? RecentReplies = null,
        QuotedPostSummaryResult? ReplyTarget = null)
    {
        public string Content => string.Concat(ContentSegments.Select(s => s.Text));
    }
}
