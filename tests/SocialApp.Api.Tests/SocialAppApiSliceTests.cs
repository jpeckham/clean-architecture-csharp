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
    public async Task Account_session_and_post_slice_runs_through_api()
    {
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            {
                services.RemoveAll<IUserGateway>();
                services.RemoveAll<ISessionGateway>();
                services.RemoveAll<IPostGateway>();
                services.AddSingleton<IUserGateway, InMemoryUserGateway>();
                services.AddSingleton<ISessionGateway, InMemorySessionGateway>();
                services.AddSingleton<IPostGateway, InMemoryPostGateway>();
            }));

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
            handle = "@ada",
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

    private sealed record AuthResult(bool Succeeded, string Message, string? Handle, string? SessionToken);
    private sealed record DevEmailsResult(IReadOnlyList<DevEmailResult> Emails);
    private sealed record DevEmailResult(string To, string Subject, string Body);
    private sealed record RecentPostsResult(IReadOnlyList<PostSummaryResult> Posts);
    private sealed record PostSummaryResult(Guid Id, string AuthorHandle, string Content, Guid? ParentPostId, Guid? OriginalPostId, int LikeCount);
}
