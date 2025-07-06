using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

namespace CSharpDIFramework.SourceGenerators;

public abstract record ServiceRegistration;

public record ContainerBlueprint(
    INamedTypeSymbol ContainerSymbol,
    string ContainerName,
    string? Namespace,
    ImmutableArray<ServiceRegistration> Registrations
)
{
    public INamedTypeSymbol ContainerSymbol { get; } = ContainerSymbol;
    public string ContainerName { get; } = ContainerName;
    public string? Namespace { get; } = Namespace;
    public ImmutableArray<ServiceRegistration> Registrations { get; } = Registrations;
}

public record ValidationResult(
    ImmutableArray<ContainerBlueprint> Blueprints,
    ImmutableArray<Diagnostic> Diagnostics
)
{
    public ImmutableArray<ContainerBlueprint> Blueprints { get; } = Blueprints;
    public ImmutableArray<Diagnostic> Diagnostics { get; } = Diagnostics;
}