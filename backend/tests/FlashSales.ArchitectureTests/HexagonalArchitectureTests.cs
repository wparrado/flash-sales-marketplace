using AwesomeAssertions;
using NetArchTest.Rules;
using System.Reflection;

namespace FlashSales.ArchitectureTests;

/// <summary>
/// Automated guardrails for the hexagonal architecture. These tests fail the build
/// the moment an inner layer takes a dependency on an outer layer or on an
/// infrastructure concern (EF Core, Polly, ASP.NET Core).
///
/// Dependency rule: Api → Infrastructure → Application → Domain (never the reverse).
/// </summary>
public class HexagonalArchitectureTests
{
    private static readonly Assembly DomainAssembly = typeof(Domain.AssemblyMarker).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(Application.AssemblyMarker).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(Infrastructure.AssemblyMarker).Assembly;

    private const string ApplicationNamespace = "FlashSales.Application";
    private const string InfrastructureNamespace = "FlashSales.Infrastructure";
    private const string ApiNamespace = "FlashSales.Api";

    [Fact]
    public void Domain_Should_Not_Depend_On_Any_Outer_Layer()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(ApplicationNamespace, InfrastructureNamespace, ApiNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(BuildFailureMessage(result));
    }

    [Fact]
    public void Domain_Should_Be_Pure_No_Framework_Or_Persistence_Dependencies()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Microsoft.EntityFrameworkCore",
                "Npgsql",
                "Polly",
                "Microsoft.AspNetCore",
                "Microsoft.Extensions")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(BuildFailureMessage(result));
    }

    [Fact]
    public void Application_Should_Not_Depend_On_Infrastructure_Or_Api()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(InfrastructureNamespace, ApiNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(BuildFailureMessage(result));
    }

    [Fact]
    public void Application_Should_Not_Depend_On_Persistence_Or_Http_Frameworks()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOnAny("Microsoft.EntityFrameworkCore", "Npgsql", "Microsoft.AspNetCore")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(BuildFailureMessage(result));
    }

    [Fact]
    public void Infrastructure_Should_Not_Depend_On_Api()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .ShouldNot()
            .HaveDependencyOn(ApiNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(BuildFailureMessage(result));
    }

    // ---- Bounded-context isolation: the future microservice boundary ----
    // Catalog and Ordering may only share the SharedKernel vocabulary; a
    // dependency in either direction would entangle the future services.

    [Fact]
    public void CatalogContext_Should_Not_Depend_On_OrderingContext()
    {
        var result = Types.InAssembly(DomainAssembly)
            .That().ResideInNamespaceStartingWith("FlashSales.Domain.Catalog")
            .ShouldNot().HaveDependencyOnAny("FlashSales.Domain.Ordering")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(BuildFailureMessage(result));
    }

    [Fact]
    public void OrderingContext_Should_Not_Depend_On_CatalogContext()
    {
        var result = Types.InAssembly(DomainAssembly)
            .That().ResideInNamespaceStartingWith("FlashSales.Domain.Ordering")
            .ShouldNot().HaveDependencyOnAny("FlashSales.Domain.Catalog")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(BuildFailureMessage(result));
    }

    [Fact]
    public void SharedKernel_Should_Depend_On_Nothing()
    {
        var result = Types.InAssembly(typeof(SharedKernel.Money).Assembly)
            .ShouldNot().HaveDependencyOnAny(
                "FlashSales.Domain", ApplicationNamespace, InfrastructureNamespace, ApiNamespace,
                "Microsoft.EntityFrameworkCore", "Microsoft.Extensions", "Microsoft.AspNetCore",
                "Npgsql", "Polly")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(BuildFailureMessage(result));
    }

    [Fact]
    public void Application_Ports_Should_Be_Interfaces()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .That().ResideInNamespace($"{ApplicationNamespace}.Ports")
            .Should().BeInterfaces()
            .GetResult();

        result.IsSuccessful.Should().BeTrue(BuildFailureMessage(result));
    }

    private static string BuildFailureMessage(TestResult result) =>
        result.IsSuccessful
            ? string.Empty
            : "offending types: " + string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>());
}
