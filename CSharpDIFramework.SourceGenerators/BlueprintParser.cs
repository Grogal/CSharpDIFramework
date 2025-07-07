using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

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
        Location declarationLocation = classNode.Identifier.GetLocation();

        var preliminaryRegistrations = new Dictionary<ITypeSymbol, ServiceRegistration>(SymbolEqualityComparer.Default);
        foreach (AttributeData attributeData in classSymbol.GetAttributes())
        {
            (ITypeSymbol? serviceType, ServiceRegistration? registration) =
                ParsePreliminaryRegistration(attributeData, diagnostics);

            if (serviceType != null && registration != null)
            {
                // Check for duplicate service registrations
                if (preliminaryRegistrations.ContainsKey(serviceType))
                {
                    diagnostics.Add(
                        Diagnostic.Create(
                            Diagnostics.DuplicateRegistrationConstructors,
                            declarationLocation,
                            serviceType.ToDisplayString()
                        )
                    );
                }
                else
                {
                    preliminaryRegistrations.Add(serviceType, registration);
                }
            }
        }

        var finalRegistrations = new List<ServiceRegistration>();
        foreach (ServiceRegistration preliminaryReg in preliminaryRegistrations.Values)
        {
            if (preliminaryReg is ConstructorRegistration ctorReg)
            {
                (IMethodSymbol? selectedCtor, ImmutableArray<Diagnostic> ctorDiagnostics) =
                    SelectConstructor(ctorReg.ImplementationType);
                diagnostics.AddRange(ctorDiagnostics);

                if (selectedCtor is null)
                {
                    // Error was reported in SelectConstructor, so we create a dummy registration to prevent crashes.
                    finalRegistrations.Add(ctorReg with { Dependencies = ImmutableArray<ServiceRegistration>.Empty });
                    continue;
                }

                var dependencies = new List<ServiceRegistration>();
                var allDependenciesFound = true;
                foreach (IParameterSymbol parameter in selectedCtor.Parameters)
                {
                    if (preliminaryRegistrations.TryGetValue(parameter.Type, out ServiceRegistration? dependencyReg))
                    {
                        dependencies.Add(dependencyReg);
                    }
                    else
                    {
                        // Dependency not found!
                        diagnostics.Add(
                            Diagnostic.Create(
                                Diagnostics.ServiceNotRegistered,
                                parameter.Locations.FirstOrDefault() ?? ctorReg.RegistrationLocation,
                                parameter.Type.ToDisplayString(),
                                ctorReg.ImplementationType.ToDisplayString()
                            )
                        );
                        allDependenciesFound = false;
                    }
                }

                if (allDependenciesFound)
                {
                    finalRegistrations.Add(
                        ctorReg with
                        {
                            SelectedConstructor = selectedCtor,
                            Dependencies = dependencies.ToImmutableArray()
                        }
                    );
                }
            }
        }

        ImmutableArray<string> containingTypeDeclarations = GetContainingTypeDeclarations(classNode);

        var blueprint = new ContainerBlueprint(
            classSymbol,
            containerName,
            namespaceName,
            finalRegistrations.ToImmutableArray(),
            declarationLocation,
            containingTypeDeclarations
        );

        return (blueprint, diagnostics.ToImmutableArray());
    }

    private static (ITypeSymbol? ServiceType, ServiceRegistration? Registration) ParsePreliminaryRegistration(
        AttributeData attributeData,
        List<Diagnostic> diagnostics)
    {
        string? attributeName = attributeData.AttributeClass?.ToDisplayString();
        ServiceLifetime lifetime;

        switch (attributeName)
        {
            case Constants.SingletonAttributeName:
                lifetime = ServiceLifetime.Singleton;
                break;
            // Add Transient/Scoped cases here in the future
            default:
                return (null, null);
        }

        (ITypeSymbol? serviceType, INamedTypeSymbol? implementationType) =
            ExtractServiceAndImplTypes(attributeData, diagnostics);
        
        Location registrationLocation = attributeData.ApplicationSyntaxReference!.GetSyntax().GetLocation();

        if (serviceType is null || implementationType is null)
        {
            diagnostics.Add(
                Diagnostic.Create(
                    Diagnostics.IncorrectAttribute,
                    registrationLocation,
                    attributeData.AttributeClass?.ToDisplayString()
                )
            );
            return (null, null);
        }

        var registration = new ConstructorRegistration(
            serviceType,
            lifetime,
            registrationLocation,
            implementationType,
            null!, // Will be replaced in Pass 2
            ImmutableArray<ServiceRegistration>.Empty
        );

        return (serviceType, registration);
    }

    private static (ITypeSymbol? serviceType, INamedTypeSymbol? implementationType) ExtractServiceAndImplTypes(
        AttributeData attributeData,
        List<Diagnostic> diagnostics)
    {
        ITypeSymbol? serviceType;
        INamedTypeSymbol? implementationType;
        Location location = attributeData.ApplicationSyntaxReference!.GetSyntax().GetLocation();

        if (attributeData.ConstructorArguments.Length == 2 &&
            attributeData.ConstructorArguments[0].Value is ITypeSymbol st &&
            attributeData.ConstructorArguments[1].Value is INamedTypeSymbol it)
        {
            serviceType = st;
            implementationType = it;
        }
        else if (attributeData.ConstructorArguments.Length == 1 &&
                 attributeData.ConstructorArguments[0].Value is INamedTypeSymbol ct)
        {
            serviceType = ct;
            implementationType = ct;
        }
        else
        {
            diagnostics.Add(Diagnostic.Create(Diagnostics.IncorrectAttribute, location, attributeData.AttributeClass?.Name));
            return (null, null);
        }

        return (serviceType, implementationType);
    }

    private static (IMethodSymbol? constructor, ImmutableArray<Diagnostic> diagnostics) SelectConstructor(INamedTypeSymbol implementationType)
    {
        var diagnostics = new List<Diagnostic>();
        Location location = implementationType.Locations.FirstOrDefault() ?? Location.None;

        ImmutableArray<IMethodSymbol> publicConstructors =
            implementationType.Constructors
                              .Where(c => c.DeclaredAccessibility == Accessibility.Public)
                              .ToImmutableArray();

        if (publicConstructors.IsEmpty)
        {
            diagnostics.Add(Diagnostic.Create(Diagnostics.NoPublicConstructor, location, implementationType.ToDisplayString()));
            return (null, diagnostics.ToImmutableArray());
        }

        ImmutableArray<IMethodSymbol> injectConstructors =
            publicConstructors
                .Where(c => c.GetAttributes()
                             .Any(a => a.AttributeClass?.ToDisplayString() == Constants.InjectAttributeName)
                )
                .ToImmutableArray();

        if (injectConstructors.Length > 1)
        {
            diagnostics.Add(
                Diagnostic.Create(
                    Diagnostics.MultipleInjectConstructors, location, implementationType.ToDisplayString()
                )
            );
            return (null, diagnostics.ToImmutableArray());
        }

        if (injectConstructors.Length == 1)
        {
            return (injectConstructors[0], diagnostics.ToImmutableArray());
        }

        // No [Inject], so fallback to greediest
        if (publicConstructors.Length == 1)
        {
            return (publicConstructors[0], diagnostics.ToImmutableArray());
        }

        int maxParams = publicConstructors.Max(c => c.Parameters.Length);
        ImmutableArray<IMethodSymbol> greediestConstructors = publicConstructors.Where(c => c.Parameters.Length == maxParams).ToImmutableArray();

        if (greediestConstructors.Length > 1)
        {
            diagnostics.Add(
                Diagnostic.Create(
                    Diagnostics.AmbiguousConstructors, location, implementationType.ToDisplayString(), maxParams
                )
            );
            return (null, diagnostics.ToImmutableArray());
        }

        return (greediestConstructors[0], diagnostics.ToImmutableArray());
    }

    private static ImmutableArray<string> GetContainingTypeDeclarations(ClassDeclarationSyntax classNode)
    {
        var declarations = new List<string>();
        SyntaxNode? parent = classNode.Parent;
        while (parent is ClassDeclarationSyntax parentClass)
        {
            declarations.Insert(0, $"public partial class {parentClass.Identifier.Text}");
            parent = parent.Parent;
        }

        return declarations.ToImmutableArray();
    }
}