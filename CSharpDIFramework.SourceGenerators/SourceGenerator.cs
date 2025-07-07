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
        IncrementalValuesProvider<(ServiceProviderDescription? Description, ImmutableArray<Diagnostic> Diagnostics)> parsedProvider =
            context.SyntaxProvider
                   .ForAttributeWithMetadataName(
                       Constants.RegisterContainerAttributeName,
                       (node, _) => node is ClassDeclarationSyntax,
                       (ctx, _) => BlueprintParser.Parse(ctx)
                   );

        IncrementalValuesProvider<ImmutableArray<Diagnostic>> parsingDiagnostics =
            parsedProvider.Select((source, _) => source.Diagnostics);

        context.RegisterSourceOutput(
            parsingDiagnostics, (spc, diagnostics) =>
            {
                foreach (Diagnostic? diagnostic in diagnostics)
                {
                    spc.ReportDiagnostic(diagnostic);
                }
            }
        );

        IncrementalValuesProvider<ServiceProviderDescription> descriptionProvider =
            parsedProvider.Select((source, _) => source.Description)
                          .Where(bp =>
                              {
                                  if (bp is null)
                                  {
                                      return false;
                                  }

                                  return true;
                              }
                          )!;

        context.RegisterSourceOutput(
            descriptionProvider, (spc, description) =>
            {
                if (description is null)
                {
                    return;
                }

                // spc.AddSource($"{description.ContainerSymbol.Name}.g.cs", description.ToString());

                var graphBuilder = new GraphBuilder(description);
                (ContainerBlueprint? blueprint, ImmutableArray<Diagnostic> diagnostics) = graphBuilder.Build();

                foreach (Diagnostic? diagnostic in diagnostics)
                {
                    spc.ReportDiagnostic(diagnostic);
                }

                if (diagnostics.Any(d => d.Severity == DiagnosticSeverity.Warning || d.Severity == DiagnosticSeverity.Error))
                {
                    string dummyBlueprintName = description.ContainerSymbol.Name;
                    spc.AddSource($"{dummyBlueprintName}.g.cs", CodeGenerator.GenerateDummyFromName(dummyBlueprintName, description.ContainerSymbol));
                }
                else if (blueprint != null)
                {
                    string sourceCode = CodeGenerator.Generate(blueprint);
                    spc.AddSource($"{blueprint.ContainerName}.g.cs", sourceCode);
                }
            }
        );
    }

    #endregion
}