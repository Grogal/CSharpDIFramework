using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSharpDIFramework.SourceGenerators;

[Generator]
public class SourceGenerator : IIncrementalGenerator
{
    private const string RegisterContainerAttributeName = "CSharpDIFramework.RegisterContainerAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<ContainerBlueprint> blueprintProvider =
            context.SyntaxProvider
                   .ForAttributeWithMetadataName(
                       RegisterContainerAttributeName,
                       (node, _) => node is ClassDeclarationSyntax,
                       (ctx, _) => BlueprintParser.Parse(ctx)
                   )
                   .Where(bp => bp is not null)!;

        var collectedBlueprints = blueprintProvider.Collect();

        context.RegisterSourceOutput(
            collectedBlueprints, (spc, blueprints) =>
            {
                ValidationResult validationResult = BlueprintValidator.Validate(blueprints);

                foreach (var diagnostic in validationResult.Diagnostics)
                {
                    spc.ReportDiagnostic(diagnostic);
                }

                if (validationResult.Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
                {
                    return;
                }

                // Phase 3: Generate code for each valid blueprint.
                foreach (var blueprint in validationResult.Blueprints)
                {
                    string sourceCode = CodeGenerator.Generate(blueprint);
                    spc.AddSource($"{blueprint.ContainerName}.g.cs", sourceCode);
                }
            }
        );
    }
}