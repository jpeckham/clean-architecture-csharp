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
