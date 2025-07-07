using System.Collections.Generic;
using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSharpDIFramework.SourceGenerators;

internal static class BlueprintParser
{
    public static (ServiceProviderDescription? Blueprint, ImmutableArray<Diagnostic> Diagnostics) Parse(GeneratorAttributeSyntaxContext context)
    {
        if (context.TargetNode is not ClassDeclarationSyntax classNode ||
            context.TargetSymbol is not INamedTypeSymbol classSymbol)
        {
            return (null, ImmutableArray<Diagnostic>.Empty);
        }

        Compilation compilation = context.SemanticModel.Compilation;
        var diagnostics = new List<Diagnostic>();
        var registrations = new List<ServiceRegistration>();

        Diagnostic? diagnostic = BlueprintValidator.ValidateContainerIsPartial(classNode, classSymbol);
        if (diagnostic is not null)
        {
            diagnostics.Add(diagnostic);
            return (null, diagnostics.ToImmutableArray());
        }

        foreach (AttributeData attributeData in classSymbol.GetAttributes())
        {
            ServiceRegistration? registration = ParseRegistrationAttribute(attributeData, diagnostics, compilation);
            if (registration != null)
            {
                registrations.Add(registration);
            }
        }

        var description = new ServiceProviderDescription(
            classSymbol,
            registrations.ToImmutableArray(),
            classNode.Identifier.GetLocation()
        );

        return (description, diagnostics.ToImmutableArray());
    }

    private static ServiceRegistration? ParseRegistrationAttribute(
        AttributeData attributeData,
        List<Diagnostic> diagnostics,
        Compilation compilation)
    {
        string? attributeName = attributeData.AttributeClass?.ToDisplayString();
        ServiceLifetime lifetime;

        switch (attributeName)
        {
            case Constants.SingletonAttributeName:
                lifetime = ServiceLifetime.Singleton;
                break;
            // Add Transient/Scoped here later
            default:
                return null;
        }

        (ITypeSymbol? serviceType, INamedTypeSymbol? implementationType) =
            ExtractServiceAndImplTypes(attributeData, diagnostics);

        if (serviceType is null || implementationType is null)
        {
            return null;
        }

        Conversion conversion = compilation.ClassifyConversion(implementationType, serviceType);
        if (conversion is { IsImplicit: false, IsIdentity: false })
        {
            diagnostics.Add(
                Diagnostic.Create(
                    Diagnostics.ImplementationNotAssignable,
                    attributeData.ApplicationSyntaxReference!.GetSyntax().GetLocation(),
                    implementationType.ToDisplayString(),
                    serviceType.ToDisplayString()
                )
            );
            return null;
        }

        if (implementationType.IsAbstract)
        {
            diagnostics.Add(
                Diagnostic.Create(
                    Diagnostics.ImplementationIsAbstract,
                    attributeData.ApplicationSyntaxReference!.GetSyntax().GetLocation(), implementationType.ToDisplayString()
                )
            );
            return null;
        }

        return new ServiceRegistration(
            serviceType,
            implementationType,
            lifetime,
            attributeData.ApplicationSyntaxReference!.GetSyntax().GetLocation()
        );
    }

    private static (ITypeSymbol? serviceType, INamedTypeSymbol? implementationType) ExtractServiceAndImplTypes(
        AttributeData attributeData,
        List<Diagnostic> diagnostics)
    {
        ITypeSymbol? serviceType;
        INamedTypeSymbol? implementationType;
        Location location = attributeData.ApplicationSyntaxReference!.GetSyntax().GetLocation();

        switch (attributeData.ConstructorArguments.Length)
        {
            case 2 when
                attributeData.ConstructorArguments[0].Value is ITypeSymbol st &&
                attributeData.ConstructorArguments[1].Value is INamedTypeSymbol it:
                serviceType = st;
                implementationType = it;
                break;
            case 1 when
                attributeData.ConstructorArguments[0].Value is INamedTypeSymbol ct:
                serviceType = ct;
                implementationType = ct;
                break;
            default:
                diagnostics.Add(Diagnostic.Create(Diagnostics.IncorrectAttribute, location, attributeData.AttributeClass?.Name));
                return (null, null);
        }

        return (serviceType, implementationType);
    }
}