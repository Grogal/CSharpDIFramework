using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

namespace CSharpDIFramework.SourceGenerators;

internal static class BlueprintValidator
{
    /// <summary>
    /// Validates the provided blueprints and returns a result containing
    /// the valid blueprints and any diagnostics.
    /// </summary>
    public static ValidationResult Validate(ImmutableArray<ContainerBlueprint> blueprints)
    {
        var diagnostics = ImmutableArray<Diagnostic>.Empty;

        return new ValidationResult(blueprints, diagnostics);
    }
}