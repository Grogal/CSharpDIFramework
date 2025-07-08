using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

using Microsoft.CodeAnalysis;

namespace CSharpDIFramework.SourceGenerators;

public static class Constants
{
    public const string RegisterContainerAttributeName = "CSharpDIFramework.RegisterContainerAttribute";
    public const string SingletonAttributeName = "CSharpDIFramework.SingletonAttribute";
    public const string TransientAttributeName = "CSharpDIFramework.TransientAttribute";
    public const string ScopedAttributeName = "CSharpDIFramework.ScopedAttribute";
    public const string InjectAttributeName = "CSharpDIFramework.InjectAttribute";
    public const string DecorateAttributeName = "CSharpDIFramework.DecorateAttribute";
    public const string RegisterModuleAttributeName = "CSharpDIFramework.RegisterModuleAttribute";
    public const string ImportModuleAttributeName = "CSharpDIFramework.ImportModuleAttribute";
}

public enum ServiceLifetime
{
    Transient,
    Scoped,
    Singleton
}

public record ServiceRegistration(
    string ServiceTypeFullName,
    INamedTypeSymbol ImplementationType,
    ServiceLifetime Lifetime,
    LocationInfo? RegistrationLocation,
    bool IsDisposable)
{
    public string ServiceTypeFullName { get; } = ServiceTypeFullName;
    public INamedTypeSymbol ImplementationType { get; } = ImplementationType;
    public ServiceLifetime Lifetime { get; } = Lifetime;
    public LocationInfo? RegistrationLocation { get; } = RegistrationLocation;
    public bool IsDisposable { get; } = IsDisposable;

    public HashSet<INamedTypeSymbol> DecoratorTypes { get; set; } = new(SymbolEqualityComparer.Default);
}

public record ServiceProviderDescription(
    INamedTypeSymbol ContainerSymbol,
    ImmutableArray<ServiceRegistration> Registrations,
    LocationInfo? DeclarationLocation)
{
    public INamedTypeSymbol ContainerSymbol { get; } = ContainerSymbol;
    public ImmutableArray<ServiceRegistration> Registrations { get; } = Registrations;
    public LocationInfo? DeclarationLocation { get; } = DeclarationLocation;

    public override string ToString()
    {
        var builder = new StringBuilder();

        builder.AppendLine("// Generated test output for ServiceProviderDescription");
        builder.AppendLine($"// Container: {ContainerSymbol.Name}");
        builder.AppendLine($"// Full Name: {ContainerSymbol.ToDisplayString()}");
        builder.AppendLine($"// Namespace: {ContainerSymbol.ContainingNamespace?.ToDisplayString() ?? "<global>"}");
        builder.AppendLine($"// Registration Count: {Registrations.Length}");
        builder.AppendLine();

        if (Registrations.Length > 0)
        {
            builder.AppendLine("// Service Registrations:");
            for (var i = 0; i < Registrations.Length; i++)
            {
                ServiceRegistration registration = Registrations[i];
                builder.AppendLine(
                    $"// [{i + 1}] {registration.Lifetime} - [Service]{registration.ServiceTypeFullName} -> [ResolveTo]{registration.ImplementationType.ToDisplayString()}"
                );
            }
        }
        else
        {
            builder.AppendLine("// No service registrations found");
        }

        builder.AppendLine();
        builder.AppendLine("// This is a test generation - actual container implementation would be generated here");
        builder.AppendLine($"namespace {ContainerSymbol.ContainingNamespace?.ToDisplayString() ?? "Global"}");
        builder.AppendLine("{");
        builder.AppendLine($"    public partial class {ContainerSymbol.Name}");
        builder.AppendLine("    {");
        builder.AppendLine("        // Generated container implementation would go here");
        builder.AppendLine("    }");
        builder.AppendLine("}");

        return builder.ToString();
    }
}

public record ResolvedDecorator(
    INamedTypeSymbol DecoratorType,
    IMethodSymbol SelectedConstructor,
    ImmutableArray<ResolvedService> Dependencies
)
{
    public INamedTypeSymbol DecoratorType { get; } = DecoratorType;
    public IMethodSymbol SelectedConstructor { get; } = SelectedConstructor;
    public ImmutableArray<ResolvedService> Dependencies { get; } = Dependencies;
}

public record ResolvedService
{
    public ResolvedService(
        ServiceRegistration sourceRegistration,
        IMethodSymbol selectedConstructor,
        ImmutableArray<ResolvedService> dependencies,
        ImmutableArray<ResolvedDecorator> decorators)
    {
        SourceRegistration = sourceRegistration;
        SelectedConstructor = selectedConstructor;
        Dependencies = dependencies;
        Decorators = decorators;
    }

    public ServiceRegistration SourceRegistration { get; }
    public IMethodSymbol SelectedConstructor { get; }
    public ImmutableArray<ResolvedService> Dependencies { get; }

    public ImmutableArray<ResolvedDecorator> Decorators { get; }

    public string ServiceTypeFullName => SourceRegistration.ServiceTypeFullName;
    public ServiceLifetime Lifetime => SourceRegistration.Lifetime;
}

public record ContainerBlueprint(
    INamedTypeSymbol ContainerSymbol,
    string ContainerName,
    string? Namespace,
    ImmutableArray<ResolvedService> Services, // Changed from Registrations
    LocationInfo? DeclarationLocation,
    ImmutableArray<string> ContainingTypeDeclarations
)
{
    public INamedTypeSymbol ContainerSymbol { get; } = ContainerSymbol;
    public string ContainerName { get; } = ContainerName;
    public string? Namespace { get; } = Namespace;
    public ImmutableArray<ResolvedService> Services { get; } = Services;

    public LocationInfo? DeclarationLocation { get; } = DeclarationLocation;

    public ImmutableArray<string> ContainingTypeDeclarations { get; } = ContainingTypeDeclarations;
}