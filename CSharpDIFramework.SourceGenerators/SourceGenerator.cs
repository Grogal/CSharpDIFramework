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
        IncrementalValuesProvider<(ServiceProviderDescription? Description, EquatableArray<DiagnosticInfo> Diagnostics)> parsedProvider =
            context.SyntaxProvider
                   .ForAttributeWithMetadataName(
                       Constants.RegisterContainerAttributeName,
                       (node, _) => node is ClassDeclarationSyntax,
                       (ctx, _) => BlueprintParser.Parse(ctx)
                   );

        IncrementalValuesProvider<EquatableArray<DiagnosticInfo>> parsingDiagnostics =
            parsedProvider.Select((source, _) => source.Diagnostics);

        context.RegisterSourceOutput(
            parsingDiagnostics, (spc, diagnostics) =>
            {
                foreach (DiagnosticInfo? diagnostic in diagnostics)
                {
                    spc.ReportDiagnostic(diagnostic.CreateDiagnostic());
                }
            }
        );

        // Filter out null descriptions to get a clean provider of valid descriptions.
        IncrementalValuesProvider<ServiceProviderDescription> descriptionProvider =
            parsedProvider.Select((source, _) => source.Description)
                          .Where(desc => desc is not null)!;

        IncrementalValuesProvider<(ServiceProviderDescription, ContainerBlueprint?, ImmutableArray<Diagnostic>)> graphProvider =
            descriptionProvider.Select((description, _) =>
                {
                    var graphBuilder = new GraphBuilder(description);
                    (ContainerBlueprint? blueprint, ImmutableArray<Diagnostic> diagnostics) = graphBuilder.Build();
                    return (description, blueprint, diagnostics);
                }
            );

        context.RegisterSourceOutput(
            graphProvider, (spc, source) =>
            {
                (ServiceProviderDescription? description, ContainerBlueprint? blueprint, ImmutableArray<Diagnostic> diagnostics) = source;

                foreach (Diagnostic? diagnostic in diagnostics)
                {
                    spc.ReportDiagnostic(diagnostic);
                }

                foreach (Diagnostic? diagnostic in diagnostics)
                {
                    spc.ReportDiagnostic(diagnostic);
                }

                bool hasErrors = diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);
                if (hasErrors)
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