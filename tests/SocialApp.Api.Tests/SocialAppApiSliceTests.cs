using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
        var adaFeed = await client.GetFromJsonAsync<RecentPostsResult>("/api/posts/recent?readerHandle=@ada&limit=20");
        adaFeed!.Posts.Should().ContainSingle(p =>
            p.Id == created.Id &&
            p.LikeCount == 1 &&
            !p.LikedByCurrentReader);

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
        repostView.QuotedPost!.AuthorHandle.Should().Be("@ada");
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

        var result = await client.GetFromJsonAsync<RecentPostsResult>("/api/posts/search?readerHandle=@grace&query=compiler");

        result!.Posts.Should().ContainSingle(p =>
            p.Id == compilerPost.Id &&
            p.Content == "Compiler notes from Ada" &&
            p.LikeCount == 1 &&
            p.LikedByCurrentReader);
        result.Posts.Should().NotContain(p => p.Content.Contains("Math", StringComparison.OrdinalIgnoreCase));
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
        recentPosts!.Posts.Should().ContainSingle(p => p.AuthorHandle == "@ada" && p.Content == "First post from the API");
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

    private static WebApplicationFactory<Program> CreateInMemoryFactory() =>
        new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            {
                services.RemoveAll<IUserGateway>();
                services.RemoveAll<ISessionGateway>();
                services.RemoveAll<IPostGateway>();
                services.RemoveAll<IPostSearchGateway>();
                services.AddSingleton<IUserGateway, InMemoryUserGateway>();
                services.AddSingleton<ISessionGateway, InMemorySessionGateway>();
                services.AddSingleton<IPostGateway, InMemoryPostGateway>();
                services.AddSingleton<IPostSearchGateway, InMemoryPostSearchGateway>();
            }));

    private static async Task<AuthResult> CreateAccountAsync(HttpClient client, string handle, string email)
    {
        var response = await client.PostAsJsonAsync("/api/accounts", new
        {
            displayName = handle.TrimStart('@'),
            handle,
            email,
            password = "Correct9!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<AuthResult>())!;
    }

    private sealed record AuthResult(bool Succeeded, string Message, string? Handle, string? SessionToken);
    private sealed record CreatePostResult(bool Succeeded, string Message, Guid? Id, string? AuthorHandle);
    private sealed record SimpleResult(bool Succeeded, string Message);
    private sealed record DevEmailsResult(IReadOnlyList<DevEmailResult> Emails);
    private sealed record DevEmailResult(string To, string Subject, string Body);
    private sealed record RecentPostsResult(IReadOnlyList<PostSummaryResult> Posts);
    private sealed record QuotedPostSummaryResult(Guid Id, string AuthorHandle, string Content);
    private sealed record PostSummaryResult(
        Guid Id,
        string AuthorHandle,
        string Content,
        Guid? ParentPostId,
        Guid? OriginalPostId,
        int LikeCount,
        bool LikedByCurrentReader,
        int RepostCount,
        bool RepostedByCurrentReader,
        QuotedPostSummaryResult? QuotedPost);
}
