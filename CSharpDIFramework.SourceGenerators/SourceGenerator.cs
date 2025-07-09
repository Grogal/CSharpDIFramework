using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSharpDIFramework.SourceGenerators;

[Generator]
internal class SourceGenerator : IIncrementalGenerator
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

        IncrementalValuesProvider<ServiceProviderDescription> descriptionProvider =
            parsedProvider.Select((source, _) => source.Description)
                          .Where(desc => desc is not null)!;

        IncrementalValuesProvider<(ServiceProviderDescription, ContainerBlueprint?, EquatableArray<DiagnosticInfo>)> graphProvider =
            descriptionProvider.Select((description, _) =>
                {
                    var graphBuilder = new GraphBuilder(description);
                    (ContainerBlueprint? blueprint, EquatableArray<DiagnosticInfo> diagnostics) = graphBuilder.Build();
                    return (description, blueprint, diagnostics);
                }
            );

        context.RegisterSourceOutput(
            graphProvider, (spc, source) =>
            {
                (ServiceProviderDescription? description, ContainerBlueprint? blueprint, EquatableArray<DiagnosticInfo> diagnostics) = source;

                foreach (DiagnosticInfo diagnostic in diagnostics)
                {
                    spc.ReportDiagnostic(diagnostic.CreateDiagnostic());
                }

                if (description == null)
                {
                    return;
                }

                bool hasErrors = diagnostics.Any(d => d.Descriptor.DefaultSeverity == DiagnosticSeverity.Error);
                if (hasErrors)
                {
                    spc.AddSource($"{description.ContainerName}.g.cs", CodeGenerator.GenerateDummyContainer(description));
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