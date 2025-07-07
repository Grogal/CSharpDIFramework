using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

namespace CSharpDIFramework.SourceGenerators;

public static class Constants
{
    public const string RegisterContainerAttributeName = "CSharpDIFramework.RegisterContainerAttribute";
    public const string SingletonAttributeName = "CSharpDIFramework.SingletonAttribute";
}

public enum ServiceLifetime
{
    Transient,
    Scoped,
    Singleton
}

public record ContainerBlueprint(
    INamedTypeSymbol ContainerSymbol,
    string ContainerName,
    string? Namespace,
    ImmutableArray<ServiceRegistration> Registrations,
    Location DeclarationLocation,
    ImmutableArray<string> ContainingTypeDeclarations
)
{
    public INamedTypeSymbol ContainerSymbol { get; } = ContainerSymbol;
    public string ContainerName { get; } = ContainerName;
    public string? Namespace { get; } = Namespace;
    public ImmutableArray<ServiceRegistration> Registrations { get; } = Registrations;
    public Location DeclarationLocation { get; } = DeclarationLocation;

    public ImmutableArray<string> ContainingTypeDeclarations { get; } = ContainingTypeDeclarations;
}

public record ValidationResult(
    ImmutableArray<ContainerBlueprint> Blueprints,
    ImmutableArray<Diagnostic> Diagnostics
)
{
    public ImmutableArray<ContainerBlueprint> Blueprints { get; } = Blueprints;
    public ImmutableArray<Diagnostic> Diagnostics { get; } = Diagnostics;
}

/// <summary>
///     This is the base class for ALL service registrations.
/// </summary>
public abstract record ServiceRegistration
{
    /// <summary>
    ///     This is the base class for ALL service registrations.
    /// </summary>
    protected ServiceRegistration(ITypeSymbol ServiceType, ServiceLifetime Lifetime, Location RegistrationLocation)
    {
        this.ServiceType = ServiceType;
        this.Lifetime = Lifetime;
        this.RegistrationLocation = RegistrationLocation;
    }

    /// <summary>The type of the service being requested, e.g., IService.</summary>
    public ITypeSymbol ServiceType { get; }

    public ServiceLifetime Lifetime { get; }

    /// <summary>A location in source code for error reporting.</summary>
    public Location RegistrationLocation { get; }
}

public sealed record ConstructorRegistration : ServiceRegistration
{
    public ConstructorRegistration(
        ITypeSymbol ServiceType,
        ServiceLifetime Lifetime,
        Location RegistrationLocation,
        INamedTypeSymbol ImplementationType,
        IMethodSymbol SelectedConstructor,
        ImmutableArray<ServiceRegistration> Dependencies) : base(
        ServiceType, Lifetime, RegistrationLocation
    )
    {
        this.ImplementationType = ImplementationType;
        this.SelectedConstructor = SelectedConstructor;
        this.Dependencies = Dependencies;
    }

    /// <summary>The concrete class to create, e.g., ServiceImpl.</summary>
    public INamedTypeSymbol ImplementationType { get; }

    /// <summary>The constructor that was selected to create this service.</summary>
    public IMethodSymbol SelectedConstructor { get; }

    /// <summary>A list of the services this constructor depends on.</summary>
    public ImmutableArray<ServiceRegistration> Dependencies { get; }
}