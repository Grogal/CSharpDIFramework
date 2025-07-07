using System.Collections.Generic;
using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSharpDIFramework.SourceGenerators;

internal static class BlueprintParser
{
    /// <summary>
    ///     Parses a class syntax node and its corresponding symbol to create a container blueprint.
    /// </summary>
    public static (ContainerBlueprint? Blueprint, ImmutableArray<Diagnostic> Diagnostics) Parse(GeneratorAttributeSyntaxContext context)
    {
        if (context.TargetNode is not ClassDeclarationSyntax classNode ||
            context.TargetSymbol is not INamedTypeSymbol classSymbol)
        {
            return (null, ImmutableArray<Diagnostic>.Empty);
        }

        var diagnostics = new List<Diagnostic>();

        Diagnostic? diagnostic = BlueprintValidator.ValidateContainerIsPartial(classNode, classSymbol);
        if (diagnostic != null)
        {
            diagnostics.Add(diagnostic);
        }

        string containerName = classSymbol.Name;
        string? namespaceName = classSymbol.ContainingNamespace.IsGlobalNamespace
            ? null
            : classSymbol.ContainingNamespace.ToDisplayString();

        var registrations = new List<ServiceRegistration>();
        foreach (AttributeData? attributeData in classSymbol.GetAttributes())
        {
            ServiceRegistration? registration = ParseRegistration(containerName, attributeData, diagnostics);
            if (registration != null)
            {
                registrations.Add(registration);
            }
        }

        Location declarationLocation = classNode.Identifier.GetLocation();

        var containingTypeDeclarations = new List<string>();
        SyntaxNode? parent = classNode.Parent;
        while (parent is ClassDeclarationSyntax parentClass)
        {
            var parentDeclaration = $"public partial class {parentClass.Identifier.Text}";
            containingTypeDeclarations.Insert(0, parentDeclaration);
            parent = parent.Parent;
        }

        var blueprint = new ContainerBlueprint(
            classSymbol,
            containerName,
            namespaceName,
            registrations.ToImmutableArray(),
            declarationLocation,
            containingTypeDeclarations.ToImmutableArray()
        );

        return (blueprint, diagnostics.ToImmutableArray());
    }

    private static ServiceRegistration? ParseRegistration(string containerName, in AttributeData attributeData, List<Diagnostic> diagnostics)
    {
        if (attributeData.AttributeClass?.ToDisplayString() != Constants.SingletonAttributeName)
        {
            return null;
        }

        ITypeSymbol? serviceType;
        INamedTypeSymbol? implementationType;
        Location registrationLocation = attributeData.ApplicationSyntaxReference!.GetSyntax().GetLocation();

        if (attributeData.ConstructorArguments.Length == 2)
        {
            // [Singleton(typeof(IService), typeof(ServiceImpl))]
            serviceType = attributeData.ConstructorArguments[0].Value as ITypeSymbol;
            implementationType = attributeData.ConstructorArguments[1].Value as INamedTypeSymbol;
        }
        else if (attributeData.ConstructorArguments.Length == 1)
        {
            // [Singleton(typeof(ConcreteService))]
            serviceType = attributeData.ConstructorArguments[0].Value as ITypeSymbol;
            implementationType = serviceType as INamedTypeSymbol;
        }
        else
        {
            diagnostics.Add(
                Diagnostic.Create(
                    Diagnostics.IncorrectAttribute,
                    registrationLocation,
                    attributeData.AttributeClass?.ToDisplayString()
                )
            );
            return null;
        }

        if (serviceType is null || implementationType is null)
        {
            diagnostics.Add(
                Diagnostic.Create(
                    Diagnostics.IncorrectAttribute,
                    registrationLocation,
                    attributeData.AttributeClass?.ToDisplayString()
                )
            );
            return null;
        }

        if (implementationType.IsAbstract)
        {
            diagnostics.Add(
                Diagnostic.Create(
                    Diagnostics.ImplementationIsAbstract,
                    registrationLocation,
                    implementationType.ToDisplayString()
                )
            );
            return null;
        }

        IMethodSymbol? constructor = null;
        foreach (IMethodSymbol? ctor in implementationType.Constructors)
        {
            if (ctor.Parameters.IsEmpty && ctor.DeclaredAccessibility == Accessibility.Public)
            {
                constructor = ctor;
                break;
            }
        }

        if (constructor is null)
        {
            diagnostics.Add(
                Diagnostic.Create(
                    Diagnostics.NoPublicConstructor,
                    registrationLocation,
                    containerName
                )
            );
            return null;
        }

        return new ConstructorRegistration
        (
            serviceType,
            ServiceLifetime.Singleton,
            registrationLocation,
            implementationType,
            constructor,
            ImmutableArray<ServiceRegistration>.Empty
        );
    }
}