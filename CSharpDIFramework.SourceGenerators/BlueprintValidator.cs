using System.Collections.Generic;
using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSharpDIFramework.SourceGenerators;

internal static class BlueprintValidator
{
    // The public signature is changed to accept the compilation.
    public static ValidationResult Validate(
        ImmutableArray<ContainerBlueprint> blueprints,
        Compilation compilation)
    {
        var diagnostics = new List<Diagnostic>();

        foreach (ContainerBlueprint? blueprint in blueprints)
        {
            foreach (ServiceRegistration? registration in blueprint.Registrations)
            {
                if (registration is ConstructorRegistration ctorReg)
                {
                    ValidateImplementationIsAssignable(ctorReg, compilation, diagnostics);
                    ValidateImplementationIsConcrete(ctorReg, diagnostics);
                }
            }
        }

        return new ValidationResult(blueprints, diagnostics.ToImmutableArray());
    }

    public static Diagnostic? ValidateContainerIsPartial(in ClassDeclarationSyntax classNode, in INamedTypeSymbol classSymbol)
    {
        if (!classNode.Modifiers.Any(SyntaxKind.PartialKeyword))
        {
            return Diagnostic.Create(
                Diagnostics.ContainerNotPartial,
                classNode.Identifier.GetLocation(),
                classSymbol.Name
            );
        }

        return null;
    }

    private static void ValidateImplementationIsAssignable(
        ConstructorRegistration registration,
        Compilation compilation,
        List<Diagnostic> diagnostics)
    {
        Conversion conversion = compilation.ClassifyConversion(
            registration.ImplementationType,
            registration.ServiceType
        );

        if (!conversion.IsImplicit)
        {
            diagnostics.Add(
                Diagnostic.Create(
                    Diagnostics.ImplementationNotAssignable,
                    registration.RegistrationLocation,
                    registration.ImplementationType.ToDisplayString(),
                    registration.ServiceType.ToDisplayString()
                )
            );
        }
    }

    private static void ValidateImplementationIsConcrete(
        ConstructorRegistration registration,
        List<Diagnostic> diagnostics)
    {
        if (registration.ImplementationType.IsAbstract)
        {
            diagnostics.Add(
                Diagnostic.Create(
                    Diagnostics.ImplementationIsAbstract,
                    registration.RegistrationLocation,
                    registration.ImplementationType.ToDisplayString()
                )
            );
        }
    }
}