using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

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

            DetectCycles(blueprint, diagnostics);
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

    private static void DetectCycles(ContainerBlueprint blueprint, List<Diagnostic> diagnostics)
    {
        Dictionary<ITypeSymbol, ServiceRegistration> registrationMap = blueprint.Registrations.ToDictionary<ITypeSymbol, ServiceRegistration>(
            r => r.ServiceType,
            SymbolEqualityComparer.Default
        );

        var visiting = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
        var visited = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);

        foreach (ServiceRegistration reg in blueprint.Registrations)
        {
            if (!visited.Contains(reg.ServiceType))
            {
                DetectCyclesRecursive(reg, registrationMap, new Stack<ITypeSymbol>(), visiting, visited, diagnostics);
            }
        }
    }

    private static void DetectCyclesRecursive(
        ServiceRegistration currentReg,
        IReadOnlyDictionary<ITypeSymbol, ServiceRegistration> allRegistrations,
        Stack<ITypeSymbol> path,
        HashSet<ITypeSymbol> visiting,
        HashSet<ITypeSymbol> visited,
        List<Diagnostic> diagnostics)
    {
        visiting.Add(currentReg.ServiceType);
        path.Push(currentReg.ServiceType);

        if (currentReg is ConstructorRegistration ctorReg)
        {
            foreach (ServiceRegistration dependencyStub in ctorReg.Dependencies)
            {
                // We must look up the full dependency from the map to continue traversal
                if (!allRegistrations.TryGetValue(dependencyStub.ServiceType, out ServiceRegistration? fullDependency))
                {
                    // This case should have been caught by the parser as a "ServiceNotRegistered" error,
                    // but we handle it defensively here.
                    continue;
                }

                if (visiting.Contains(fullDependency.ServiceType))
                {
                    // Cycle detected!
                    string cyclePath = string.Join(" -> ", path.Reverse().Select(s => s.Name).Concat([fullDependency.ServiceType.Name]));
                    diagnostics.Add(
                        Diagnostic.Create(
                            Diagnostics.CyclicDependency,
                            currentReg.RegistrationLocation,
                            currentReg.ServiceType.ToDisplayString(),
                            cyclePath
                        )
                    );
                    continue; // Continue checking other branches
                }

                if (!visited.Contains(fullDependency.ServiceType))
                {
                    DetectCyclesRecursive(fullDependency, allRegistrations, path, visiting, visited, diagnostics);
                }
            }
        }

        path.Pop();
        visiting.Remove(currentReg.ServiceType);
        visited.Add(currentReg.ServiceType);
    }
}