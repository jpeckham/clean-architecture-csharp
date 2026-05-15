using System.Reflection;
using FluentAssertions;
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
    public void Entities_do_not_depend_on_boundary_adapter_or_framework_details()
    {
        var forbidden = new[]
        {
            "SocialApp.Api",
            "SocialApp.Web",
            "SocialApp.Infrastructure",
            "Microsoft.AspNetCore",
            "Microsoft.Extensions",
            "MongoDB",
            "Azure",
            "System.Net.Http",
            "System.Text.Json",
            "Controllers",
            "Presenters",
            "Gateways",
            "UseCases",
            "ViewModels",
            "ResponseModels",
            "RequestModels"
        };

        Types.InAssemblies(new[] { UserAssembly, PostAssembly })
            .That().ResideInNamespaceEndingWith(".Entities")
            .ShouldNot().HaveDependencyOnAny(forbidden)
            .GetResult().IsSuccessful.Should().BeTrue("entities must remain pure domain objects; move boundary, adapter, framework, and transport concerns outward");
    }

    [Fact]
    public void Use_cases_do_not_depend_on_outer_adapters_or_framework_details()
    {
        var forbidden = new[]
        {
            "SocialApp.Api",
            "SocialApp.Web",
            "SocialApp.Infrastructure",
            "Microsoft.AspNetCore",
            "Microsoft.Extensions.DependencyInjection",
            "Microsoft.Extensions.Configuration",
            "MongoDB",
            "Azure",
            "System.Net.Http",
            "System.Text.Json"
        };

        Types.InAssemblies(new[] { UserAssembly, PostAssembly })
            .That().ResideInNamespaceEndingWith(".UseCases")
            .ShouldNot().HaveDependencyOnAny(forbidden)
            .GetResult().IsSuccessful.Should().BeTrue("use cases should depend only on component boundaries and domain abstractions, not delivery or persistence details");
    }

    [Fact]
    public void Component_presenters_do_not_contain_http_route_literals()
    {
        var routeLiterals = FindSourceLines(
            new[] { "src/SocialApp.User/Presenters", "src/SocialApp.Post/Presenters" },
            "\"/");

        routeLiterals.Should().BeEmpty("presenters translate responses to view models; HTTP route construction belongs in API or Web adapters");
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

    private static IReadOnlyList<string> FindSourceLines(IEnumerable<string> relativeDirectories, string pattern)
    {
        var root = FindRepositoryRoot();
        return relativeDirectories
            .Select(relative => Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)))
            .Where(Directory.Exists)
            .SelectMany(directory => Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories))
            .SelectMany(file => File.ReadLines(file)
                .Select((line, index) => new { Line = line, LineNumber = index + 1 })
                .Where(item => item.Line.Contains(pattern, StringComparison.Ordinal))
                .Select(item => $"{Path.GetRelativePath(root, file)}:{item.LineNumber}: {item.Line.Trim()}"))
            .ToArray();
    }
}
