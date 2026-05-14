using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using NetArchTest.Rules;
using Xunit;

namespace SocialApp.Architecture.Tests;

public sealed class ArchitectureRulesTests
{
    private static readonly Assembly UserAssembly = typeof(SocialApp.User.Entities.UserAccount).Assembly;
    private static readonly Assembly PostAssembly = typeof(SocialApp.Post.Entities.SocialPost).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(SocialApp.Infrastructure.CosmosMongo.DependencyInjection).Assembly;
    private static readonly Assembly AcsEmailAssembly = typeof(SocialApp.Infrastructure.AcsEmail.DependencyInjection).Assembly;
    private static readonly Assembly ApiAssembly = typeof(Program).Assembly;
    private static readonly Assembly WebAssembly = typeof(SocialApp.Web.App).Assembly;

    [Fact]
    public void Components_do_not_reference_each_other()
    {
        UserAssembly.GetReferencedAssemblies().Select(a => a.Name).Should().NotContain("SocialApp.Post");
        PostAssembly.GetReferencedAssemblies().Select(a => a.Name).Should().NotContain("SocialApp.User");
    }

    [Fact]
    public void Components_do_not_reference_framework_or_database_packages()
    {
        var forbidden = new[] { "Microsoft.AspNetCore", "Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore.Components", "MongoDB.Driver", "Azure.Communication.Email", "MediatR" };

        UserAssembly.GetReferencedAssemblies().Select(a => a.Name).Should().NotIntersectWith(forbidden);
        PostAssembly.GetReferencedAssemblies().Select(a => a.Name).Should().NotIntersectWith(forbidden);
    }

    [Fact]
    public void Business_components_do_not_reference_outer_details()
    {
        var forbidden = new[] { "SocialApp.Api", "SocialApp.Web", "SocialApp.Infrastructure.CosmosMongo", "SocialApp.Infrastructure.AcsEmail" };

        UserAssembly.GetReferencedAssemblies().Select(a => a.Name).Should().NotIntersectWith(forbidden);
        PostAssembly.GetReferencedAssemblies().Select(a => a.Name).Should().NotIntersectWith(forbidden);
    }

    [Fact]
    public void User_profile_post_adapter_does_not_depend_on_post_use_cases()
    {
        Types.InAssembly(ApiAssembly)
            .That().HaveName("UserProfilePostGatewayAdapter")
            .ShouldNot().HaveDependencyOn("SocialApp.Post.UseCases")
            .GetResult().IsSuccessful.Should().BeTrue();
    }

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
    public void Web_does_not_reference_business_or_infrastructure_projects()
    {
        WebAssembly.GetReferencedAssemblies().Select(a => a.Name)
            .Where(n => n is not null && n.StartsWith("SocialApp."))
            .Should().BeEmpty();
    }

    [Fact]
    public void Infrastructure_references_business_components_but_not_api_or_web()
    {
        var references = InfrastructureAssembly.GetReferencedAssemblies().Select(a => a.Name).ToArray();

        references.Should().Contain(new[] { "SocialApp.User", "SocialApp.Post" });
        references.Should().NotContain(new[] { "SocialApp.Api", "SocialApp.Web" });

        var acsReferences = AcsEmailAssembly.GetReferencedAssemblies().Select(a => a.Name).ToArray();
        acsReferences.Should().Contain("SocialApp.User");
        acsReferences.Should().NotContain(new[] { "SocialApp.Api", "SocialApp.Web", "SocialApp.Post" });
    }

    [Fact]
    public void Entities_are_isolated_from_boundaries_and_adapters()
    {
        Types.InAssemblies(new[] { UserAssembly, PostAssembly })
            .That().ResideInNamespaceEndingWith(".Entities")
            .ShouldNot().HaveDependencyOnAny("Controllers", "Presenters", "Gateways", "UseCases")
            .GetResult().IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Interactors_do_not_depend_on_controllers_or_presenters()
    {
        Types.InAssemblies(new[] { UserAssembly, PostAssembly })
            .That().HaveNameEndingWith("Interactor")
            .ShouldNot().HaveDependencyOnAny("Controllers", "Presenters")
            .GetResult().IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Presenters_implement_output_boundaries()
    {
        var presenters = UserAssembly.GetTypes().Concat(PostAssembly.GetTypes())
            .Where(t => t.Name.EndsWith("Presenter", StringComparison.Ordinal) && t.IsClass);

        presenters.Should().OnlyContain(t => t.GetInterfaces().Any(i => i.Name.EndsWith("OutputBoundary", StringComparison.Ordinal)));
    }

    [Fact]
    public void Controllers_depend_on_input_boundaries()
    {
        var controllers = UserAssembly.GetTypes().Concat(PostAssembly.GetTypes())
            .Where(t => t.Name.EndsWith("Controller", StringComparison.Ordinal) && t.IsClass);

        controllers.Should().OnlyContain(t => t.GetConstructors()
            .SelectMany(c => c.GetParameters())
            .Any(p => p.ParameterType.Name.EndsWith("InputBoundary", StringComparison.Ordinal)));
    }

    [Fact]
    public void Component_dependency_graph_is_acyclic()
    {
        var componentReferences = new Dictionary<string, string[]>
        {
            ["SocialApp.User"] = UserAssembly.GetReferencedAssemblies().Select(a => a.Name!).Where(n => n.StartsWith("SocialApp.")).ToArray(),
            ["SocialApp.Post"] = PostAssembly.GetReferencedAssemblies().Select(a => a.Name!).Where(n => n.StartsWith("SocialApp.")).ToArray()
        };

        componentReferences["SocialApp.User"].Should().BeEmpty();
        componentReferences["SocialApp.Post"].Should().BeEmpty();
    }

    [Fact]
    public void Feed_has_separate_recent_and_search_routes()
    {
        typeof(SocialApp.Web.Pages.Feed)
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
