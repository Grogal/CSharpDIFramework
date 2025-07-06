using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

namespace CSharpDIFramework.SourceGenerators;

internal static class BlueprintParser
{
    /// <summary>
    /// Parses a class syntax node and its corresponding symbol to create a container blueprint.
    /// </summary>
    public static ContainerBlueprint? Parse(GeneratorAttributeSyntaxContext context)
    {
        if (context.TargetSymbol is not INamedTypeSymbol classSymbol)
        {
            return null;
        }

        string containerName = classSymbol.Name;
        string? ns = classSymbol.ContainingNamespace.IsGlobalNamespace
            ? null
            : classSymbol.ContainingNamespace.ToDisplayString();

        var registrations = ImmutableArray<ServiceRegistration>.Empty;

        return new ContainerBlueprint(classSymbol, containerName, ns, registrations);
    }
}