using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSharpDIFramework.SourceGenerators;

[Generator]
public class SourceGenerator : IIncrementalGenerator
{
    #region IIncrementalGenerator Implementation

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<(ContainerBlueprint? Blueprint, ImmutableArray<Diagnostic> Diagnostics)> parsedProvider =
            context.SyntaxProvider
                   .ForAttributeWithMetadataName(
                       Constants.RegisterContainerAttributeName,
                       (node, _) => node is ClassDeclarationSyntax,
                       (ctx, _) => BlueprintParser.Parse(ctx)
                   );

        IncrementalValuesProvider<ImmutableArray<Diagnostic>> parsingDiagnostics =
            parsedProvider.Select((source, _) => source.Diagnostics);

        IncrementalValuesProvider<ContainerBlueprint> blueprintProvider =
            parsedProvider.Select((source, _) => source.Blueprint).Where(bp => bp is not null)!;

        context.RegisterSourceOutput(
            parsingDiagnostics, (spc, diagnostics) =>
            {
                foreach (Diagnostic? diagnostic in diagnostics)
                {
                    spc.ReportDiagnostic(diagnostic);
                }
            }
        );

        IncrementalValueProvider<(ImmutableArray<ContainerBlueprint> Left, Compilation Right)> combinedProvider =
            blueprintProvider.Collect().Combine(context.CompilationProvider);

        context.RegisterSourceOutput(
            combinedProvider, (spc, source) =>
            {
                ImmutableArray<ContainerBlueprint> blueprints = source.Left;
                Compilation? compilation = source.Right;

                if (blueprints.IsEmpty)
                {
                    return;
                }

                ValidationResult validationResult = BlueprintValidator.Validate(blueprints, compilation);

                foreach (Diagnostic? diagnostic in validationResult.Diagnostics)
                {
                    spc.ReportDiagnostic(diagnostic);
                }

                if (validationResult.Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
                {
                    return;
                }

                // Phase 3: Code Generation
                foreach (ContainerBlueprint? blueprint in validationResult.Blueprints)
                {
                    string sourceCode = CodeGenerator.Generate(blueprint);
                    spc.AddSource($"{blueprint.ContainerName}.g.cs", sourceCode);
                }
            }
        );
    }

    #endregion
}