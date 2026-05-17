using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Components;

namespace SocialApp.Web.Tests;

public sealed class WebConfigurationTests
{
    [Fact]
    public void Docker_web_app_uses_ipv4_loopback_for_browser_api_base_address()
    {
        var root = FindRepositoryRoot();
        var composeOverride = File.ReadAllText(Path.Combine(root, "docker-compose.override.yml"));
        var webEntrypoint = File.ReadAllText(Path.Combine(root, "src", "SocialApp.Web", "docker-entrypoint.d", "10-write-appsettings.sh"));

        composeOverride.Should().Contain("API_BASE_ADDRESS: http://127.0.0.1:8080");
        webEntrypoint.Should().Contain("API_BASE_ADDRESS:-http://127.0.0.1:8080");
    }

    [Fact]
    public void Feed_has_separate_recent_and_search_routes()
    {
        typeof(Pages.Feed)
            .GetCustomAttributes<RouteAttribute>()
            .Select(attribute => attribute.Template)
            .Should().BeEquivalentTo(new[] { "/feed", "/feed/search" });
    }

    [Fact]
    public void Post_details_page_has_stable_deep_link_route()
    {
        typeof(Pages.PostDetails)
            .GetCustomAttributes<RouteAttribute>()
            .Select(attribute => attribute.Template)
            .Should().ContainSingle("/posts/{PostId:guid}");
    }

    [Fact]
    public void Deep_link_auth_flow_preserves_return_url_after_login()
    {
        var root = FindRepositoryRoot();
        var postDetails = File.ReadAllText(Path.Combine(root, "src", "SocialApp.Web", "Pages", "PostDetails.razor"));
        var login = File.ReadAllText(Path.Combine(root, "src", "SocialApp.Web", "Pages", "Login.razor"));

        postDetails.Should().Contain("returnUrl=");
        login.Should().Contain("[SupplyParameterFromQuery]");
        login.Should().Contain("NavigateAfterSignIn");
        login.Should().Contain("ReturnUrl");
    }

    [Fact]
    public void Web_client_exposes_individual_post_fetch_method()
    {
        typeof(Services.SocialAppApiClient)
            .GetMethod("GetPostAsync", new[] { typeof(string), typeof(Guid) })
            .Should().NotBeNull();
    }

    [Fact]
    public void Web_client_exposes_reply_method()
    {
        typeof(Services.SocialAppApiClient)
            .GetMethod("ReplyToPostAsync", new[] { typeof(string), typeof(Guid), typeof(Services.ReplyPostRequest) })
            .Should().NotBeNull();
    }

    [Fact]
    public void Feed_uses_shared_post_card_component_for_post_actions()
    {
        typeof(Components.PostCard).Should().NotBeNull();

        var root = FindRepositoryRoot();
        var feed = File.ReadAllText(Path.Combine(root, "src", "SocialApp.Web", "Pages", "Feed.razor"));
        feed.Should().Contain("<PostCard");
        feed.Should().Contain("ShowConversationPreview=\"false\"");
    }

    [Fact]
    public void Post_card_renders_join_conversation_action_and_conversation_preview()
    {
        var root = FindRepositoryRoot();
        var postCard = File.ReadAllText(Path.Combine(root, "src", "SocialApp.Web", "Components", "PostCard.razor"));

        postCard.Should().Contain("Join the conversation");
        postCard.Should().Contain("chat-bubble-icon");
        postCard.Should().Contain("ShowConversationPreview");
        postCard.Should().Contain("reply-prefix");
        postCard.Should().Contain("RecentReplies");
    }

    [Fact]
    public void Post_card_makes_quoted_context_posts_navigable()
    {
        var root = FindRepositoryRoot();
        var postCard = File.ReadAllText(Path.Combine(root, "src", "SocialApp.Web", "Components", "PostCard.razor"));

        postCard.Should().Contain("clickable-quoted-post");
        postCard.Should().Contain("OpenQuotedPostAsync(Item.QuotedPost)");
        postCard.Should().Contain("OpenQuotedPostAsync(Item.ReplyTarget)");
        postCard.Should().Contain("HandleQuotedPostKeyDownAsync");
    }

    [Fact]
    public void Conversation_replies_component_renders_nested_replies()
    {
        typeof(Components.ConversationReplies).Should().NotBeNull();

        var root = FindRepositoryRoot();
        var conversationReplies = File.ReadAllText(Path.Combine(root, "src", "SocialApp.Web", "Components", "ConversationReplies.razor"));

        conversationReplies.Should().Contain("<ConversationReplies");
        conversationReplies.Should().Contain("Replies=\"reply.RecentReplies\"");
    }

    [Fact]
    public void Feed_styles_use_dark_professional_dashboard_theme()
    {
        var css = ReadWebStylesheet();

        css.Should().Contain("color-scheme: dark");
        css.Should().Contain("--surface");
        css.Should().Contain("--accent");
    }

    [Fact]
    public void Feed_styles_widen_posts_and_style_cards()
    {
        var css = ReadWebStylesheet();

        css.Should().Contain("minmax(0, 820px)");
        css.Should().Contain(".post {");
        css.Should().Contain("box-shadow:");
    }

    [Fact]
    public void Composer_has_dedicated_dashboard_styling()
    {
        var css = ReadWebStylesheet();

        css.Should().Contain(".composer-header");
        css.Should().Contain(".composer-submit-row");
        css.Should().Contain(".composer textarea");
    }

    private static string ReadWebStylesheet()
    {
        var root = FindRepositoryRoot();
        return File.ReadAllText(Path.Combine(root, "src", "SocialApp.Web", "wwwroot", "css", "app.css"));
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "SocialApp.sln")))
        {
            directory = directory.Parent;
        }

        directory.Should().NotBeNull();
        return directory!.FullName;
    }
}
