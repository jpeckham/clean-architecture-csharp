using System.Reflection;
using FluentAssertions;
using NetArchTest.Rules;
using Xunit;

namespace SocialApp.Architecture.Tests;

public sealed class ArchitectureRulesTests
{
    private static readonly Assembly UserAssembly = typeof(SocialApp.User.Entities.UserAccount).Assembly;
    private static readonly Assembly PostAssembly = typeof(SocialApp.Post.Entities.SocialPost).Assembly;

    [Fact]
    public void Components_do_not_reference_each_other()
    {
        UserAssembly.GetReferencedAssemblies().Select(a => a.Name).Should().NotContain("SocialApp.Post");
        PostAssembly.GetReferencedAssemblies().Select(a => a.Name).Should().NotContain("SocialApp.User");
    }

    [Fact]
    public void Components_do_not_reference_framework_or_database_packages()
    {
        var forbidden = new[] { "Microsoft.AspNetCore", "Microsoft.EntityFrameworkCore", "MediatR" };

        UserAssembly.GetReferencedAssemblies().Select(a => a.Name).Should().NotIntersectWith(forbidden);
        PostAssembly.GetReferencedAssemblies().Select(a => a.Name).Should().NotIntersectWith(forbidden);
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
}
