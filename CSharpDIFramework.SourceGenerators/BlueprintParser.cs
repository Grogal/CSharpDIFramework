using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSharpDIFramework.SourceGenerators;

internal static class BlueprintParser
{
    private static INamedTypeSymbol s_disposableInterface = null!;
    private static INamedTypeSymbol s_importModuleAttribute = null!;
    private static INamedTypeSymbol s_decorateAttribute = null!;
    private static INamedTypeSymbol s_registerModuleAttribute = null!;
    private static INamedTypeSymbol s_singletonAttribute = null!;
    private static INamedTypeSymbol s_scopedAttribute = null!;
    private static INamedTypeSymbol s_transientAttribute = null!;

    private static readonly SymbolDisplayFormat s_fullyQualifiedFormat = SymbolDisplayFormat.FullyQualifiedFormat
                                                                                            .WithGlobalNamespaceStyle(
                                                                                                SymbolDisplayGlobalNamespaceStyle.Included
                                                                                            );

    public static (ServiceProviderDescription? Blueprint, EquatableArray<DiagnosticInfo> Diagnostics) Parse(GeneratorAttributeSyntaxContext context)
    {
        if (context.TargetNode is not ClassDeclarationSyntax classNode ||
            context.TargetSymbol is not INamedTypeSymbol classSymbol)
        {
            return (null, EquatableArray<DiagnosticInfo>.Empty);
        }

        var diagnostics = new List<DiagnosticInfo>();

        DiagnosticInfo? partialDiagnostic = ValidateContainerIsPartial(classNode, classSymbol);
        if (partialDiagnostic is not null)
        {
            diagnostics.Add(partialDiagnostic);
            return (null, new EquatableArray<DiagnosticInfo>(diagnostics));
        }

        Compilation compilation = context.SemanticModel.Compilation;
        s_disposableInterface = compilation.GetTypeByMetadataName("System.IDisposable")!;
        s_importModuleAttribute = compilation.GetTypeByMetadataName(Constants.ImportModuleAttributeName)!;
        s_decorateAttribute = compilation.GetTypeByMetadataName(Constants.DecorateAttributeName)!;
        s_registerModuleAttribute = compilation.GetTypeByMetadataName(Constants.RegisterModuleAttributeName)!;
        s_singletonAttribute = compilation.GetTypeByMetadataName(Constants.SingletonAttributeName)!;
        s_scopedAttribute = compilation.GetTypeByMetadataName(Constants.ScopedAttributeName)!;
        s_transientAttribute = compilation.GetTypeByMetadataName(Constants.TransientAttributeName)!;

        var registrationMap = new Dictionary<string, ServiceRegistration>();
        ParseAttributesFromType(classSymbol, registrationMap, diagnostics, compilation, new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default));

        var description = new ServiceProviderDescription(
            classSymbol.ToDisplayString(s_fullyQualifiedFormat),
            classSymbol.Name,
            classSymbol.ContainingNamespace.IsGlobalNamespace ? null : classSymbol.ContainingNamespace.ToDisplayString(),
            GetContainingTypeDeclarations(classSymbol),
            new EquatableArray<ServiceRegistration>(registrationMap.Values.ToList()),
            LocationInfo.CreateFrom(classNode.Identifier.GetLocation())
        );

        return (description, new EquatableArray<DiagnosticInfo>(diagnostics));
    }

    private static void ParseAttributesFromType(
        INamedTypeSymbol typeSymbol,
        Dictionary<string, ServiceRegistration> registrationMap,
        List<DiagnosticInfo> diagnostics,
        Compilation compilation,
        HashSet<INamedTypeSymbol> visitedModules)
    {
        if (!visitedModules.Add(typeSymbol))
        {
            return; // Cycle in modules
        }

        foreach (AttributeData attributeData in typeSymbol.GetAttributes())
        {
            INamedTypeSymbol? attributeClassSymbol = attributeData.AttributeClass;
            if (attributeClassSymbol is null)
            {
                continue;
            }

            if (SymbolEqualityComparer.Default.Equals(attributeClassSymbol, s_importModuleAttribute))
            {
                if (attributeData.ConstructorArguments.Length == 1 &&
                    attributeData.ConstructorArguments[0].Value is INamedTypeSymbol moduleType)
                {
                    if (moduleType.GetAttributes().Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, s_registerModuleAttribute)))
                    {
                        ParseAttributesFromType(moduleType, registrationMap, diagnostics, compilation, visitedModules);
                    }
                    else
                    {
                        diagnostics.Add(
                            DiagnosticInfo.Create(
                                Diagnostics.ImportedTypeNotAModule, attributeData.ApplicationSyntaxReference!.GetSyntax().GetLocation(),
                                moduleType.ToDisplayString()
                            )
                        );
                    }
                }
            }
            else if (SymbolEqualityComparer.Default.Equals(attributeClassSymbol, s_decorateAttribute))
            {
                ParseDecoratorAttribute(attributeData, registrationMap, diagnostics, compilation);
            }
            else
            {
                ServiceRegistration? registration = ParseRegistrationAttribute(attributeData, diagnostics, compilation);
                if (registration != null)
                {
                    if (registrationMap.ContainsKey(registration.ServiceTypeFullName))
                    {
                        diagnostics.Add(
                            DiagnosticInfo.Create(
                                Diagnostics.DuplicateRegistrationConstructors, registration.RegistrationLocation, registration.ServiceTypeFullName
                            )
                        );
                    }
                    else
                    {
                        registrationMap.Add(registration.ServiceTypeFullName, registration);
                    }
                }
            }
        }

        visitedModules.Remove(typeSymbol);
    }

    private static void ParseDecoratorAttribute(
        AttributeData attributeData,
        Dictionary<string, ServiceRegistration> registrationMap,
        List<DiagnosticInfo> diagnostics,
        Compilation compilation)
    {
        (ITypeSymbol? serviceType, INamedTypeSymbol? decoratorType) =
            ExtractServiceAndImplTypes(attributeData, diagnostics);

        if (serviceType is null || decoratorType is null || SymbolEqualityComparer.Default.Equals(decoratorType, serviceType))
        {
            return;
        }

        string serviceTypeKey = serviceType.ToDisplayString(s_fullyQualifiedFormat);
        if (registrationMap.TryGetValue(serviceTypeKey, out ServiceRegistration? registrationToDecorate))
        {
            Conversion conversion = compilation.ClassifyConversion(decoratorType, serviceType);
            if (conversion is { IsImplicit: false, IsIdentity: false })
            {
                diagnostics.Add(
                    DiagnosticInfo.Create(
                        Diagnostics.ImplementationNotAssignable, attributeData.ApplicationSyntaxReference!.GetSyntax().GetLocation(),
                        decoratorType.ToDisplayString(), serviceType.ToDisplayString()
                    )
                );
                return;
            }

            if (decoratorType.IsAbstract)
            {
                diagnostics.Add(
                    DiagnosticInfo.Create(
                        Diagnostics.ImplementationIsAbstract, attributeData.ApplicationSyntaxReference!.GetSyntax().GetLocation(),
                        decoratorType.ToDisplayString()
                    )
                );
                return;
            }

            var decoratorInfo = new DecoratorInfo(
                decoratorType.ToDisplayString(s_fullyQualifiedFormat),
                LocationInfo.CreateFrom(decoratorType.Locations.FirstOrDefault()),
                GetConstructors(decoratorType)
            );

            if (registrationToDecorate.Decorators.Any(d => d.FullName == decoratorInfo.FullName))
            {
                diagnostics.Add(
                    DiagnosticInfo.Create(
                        Diagnostics.DuplicateDecoratorRegistration, attributeData.ApplicationSyntaxReference!.GetSyntax().GetLocation(), decoratorType.Name,
                        serviceType.Name
                    )
                );
                return;
            }

            // Immutable update
            List<DecoratorInfo> newDecorators = registrationToDecorate.Decorators.GetArray()?.ToList() ?? new List<DecoratorInfo>();
            newDecorators.Add(decoratorInfo);
            registrationMap[serviceTypeKey] = registrationToDecorate with { Decorators = new EquatableArray<DecoratorInfo>(newDecorators) };
        }
        else
        {
            diagnostics.Add(
                DiagnosticInfo.Create(
                    Diagnostics.DecoratorForUnregisteredService,
                    attributeData.ApplicationSyntaxReference!.GetSyntax().GetLocation(),
                    decoratorType.Name,
                    serviceType.Name
                )
            );
        }
    }

    private static ServiceRegistration? ParseRegistrationAttribute(
        AttributeData attributeData,
        List<DiagnosticInfo> diagnostics,
        Compilation compilation)
    {
        INamedTypeSymbol? attributeClassSymbol = attributeData.AttributeClass;
        ServiceLifetime lifetime;

        if (SymbolEqualityComparer.Default.Equals(attributeClassSymbol, s_singletonAttribute))
        {
            lifetime = ServiceLifetime.Singleton;
        }
        else if (SymbolEqualityComparer.Default.Equals(attributeClassSymbol, s_scopedAttribute))
        {
            lifetime = ServiceLifetime.Scoped;
        }
        else if (SymbolEqualityComparer.Default.Equals(attributeClassSymbol, s_transientAttribute))
        {
            lifetime = ServiceLifetime.Transient;
        }
        else
        {
            return null;
        }

        (ITypeSymbol? serviceType, INamedTypeSymbol? implementationType) = ExtractServiceAndImplTypes(attributeData, diagnostics);
        if (serviceType is null || implementationType is null)
        {
            return null;
        }

        Conversion conversion = compilation.ClassifyConversion(implementationType, serviceType);
        if (conversion is { IsImplicit: false, IsIdentity: false })
        {
            diagnostics.Add(
                DiagnosticInfo.Create(
                    Diagnostics.ImplementationNotAssignable, attributeData.ApplicationSyntaxReference!.GetSyntax().GetLocation(),
                    implementationType.ToDisplayString(), serviceType.ToDisplayString()
                )
            );
            return null;
        }

        if (implementationType.IsAbstract)
        {
            diagnostics.Add(
                DiagnosticInfo.Create(
                    Diagnostics.ImplementationIsAbstract, attributeData.ApplicationSyntaxReference!.GetSyntax().GetLocation(),
                    implementationType.ToDisplayString()
                )
            );
            return null;
        }

        bool isDisposable = implementationType.AllInterfaces.Contains(s_disposableInterface, SymbolEqualityComparer.Default);

        return new ServiceRegistration(
            serviceType.ToDisplayString(s_fullyQualifiedFormat),
            new ServiceImplementationType(
                implementationType.ToDisplayString(s_fullyQualifiedFormat),
                LocationInfo.CreateFrom(implementationType.Locations.FirstOrDefault()),
                GetConstructors(implementationType)
            ),
            lifetime,
            LocationInfo.CreateFrom(attributeData.ApplicationSyntaxReference!.GetSyntax().GetLocation()),
            isDisposable
        );
    }

    private static (ITypeSymbol? serviceType, INamedTypeSymbol? implementationType) ExtractServiceAndImplTypes(
        AttributeData attributeData,
        List<DiagnosticInfo> diagnostics)
    {
        ITypeSymbol? serviceType;
        INamedTypeSymbol? implementationType;
        Location location = attributeData.ApplicationSyntaxReference!.GetSyntax().GetLocation();

        switch (attributeData.ConstructorArguments.Length)
        {
            case 2 when attributeData.ConstructorArguments[0].Value is ITypeSymbol st &&
                        attributeData.ConstructorArguments[1].Value is INamedTypeSymbol it:
                serviceType = st;
                implementationType = it;
                break;
            case 1 when attributeData.ConstructorArguments[0].Value is INamedTypeSymbol ct:
                serviceType = ct;
                implementationType = ct;
                break;
            default:
                diagnostics.Add(
                    DiagnosticInfo.Create(
                        Diagnostics.IncorrectAttribute,
                        location,
                        attributeData.AttributeClass?.Name!
                    )
                );
                return (null, null);
        }

        return (serviceType, implementationType);
    }

    private static EquatableArray<ConstructorInfo> GetConstructors(INamedTypeSymbol implementationType)
    {
        IEnumerable<IMethodSymbol> publicConstructors = implementationType.Constructors
                                                                          .Where(c => c.DeclaredAccessibility == Accessibility.Public);

        var constructorInfos = new List<ConstructorInfo>();
        foreach (IMethodSymbol ctor in publicConstructors)
        {
            string[] parameters = ctor.Parameters
                                      .Select(p => p.Type.ToDisplayString(s_fullyQualifiedFormat))
                                      .ToArray();

            bool hasInjectAttribute = ctor.GetAttributes()
                                          .Any(a => a.AttributeClass?.ToDisplayString() == Constants.InjectAttributeName);

            constructorInfos.Add(new ConstructorInfo(new EquatableArray<string>(parameters), hasInjectAttribute));
        }

        return new EquatableArray<ConstructorInfo>(constructorInfos);
    }

    /// <summary>
    /// Gets the full declarations of all containing types for a given symbol, ensuring they are marked as partial.
    /// This is crucial for generating code for nested container classes.
    /// </summary>
    private static EquatableArray<string> GetContainingTypeDeclarations(INamedTypeSymbol classSymbol)
    {
        var declarations = new List<string>();
        INamedTypeSymbol? parent = classSymbol.ContainingType;
        while (parent != null)
        {
            // Get the full, correct declaration for the parent type.
            string? declaration = GetTypeDeclarationFromSymbol(parent);
            if (declaration is not null)
            {
                declarations.Insert(0, declaration);
            }
            // If the declaration is null, it means the parent is not in a source (e.g., from metadata).
            // A container cannot be nested in a type from another assembly, so this is a safe assumption.

            parent = parent.ContainingType;
        }

        return new EquatableArray<string>(declarations);
    }

    /// <summary>
    /// Reconstructs the source-code signature of a type declaration from its symbol.
    /// Example: "public partial static record MyRecordT where T: new()"
    /// </summary>
    private static string? GetTypeDeclarationFromSymbol(INamedTypeSymbol typeSymbol)
    {
        // Find the syntax declaration for the symbol. We only need one, even for partial types.
        if (typeSymbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is not TypeDeclarationSyntax typeSyntax)
        {
            return null; // Should not happen for a container's parent types.
        }

        var sb = new StringBuilder();

        // 1. Modifiers (e.g., public, internal, partial, static)
        bool hasPartial = false;
        foreach (var modifier in typeSyntax.Modifiers)
        {
            sb.Append(modifier.Text).Append(' ');
            if (modifier.IsKind(SyntaxKind.PartialKeyword))
            {
                hasPartial = true;
            }
        }

        // The source generator MUST output a partial class. The check for this is done
        // in Parse(), but we ensure the generated code has it regardless.
        if (!hasPartial)
        {
            // This case should be blocked by a diagnostic, but we generate the correct code defensively.
            sb.Insert(0, "partial ");
        }

        // 2. Type Keyword (class, struct, record)
        sb.Append(typeSyntax.Keyword.Text).Append(' ');

        // 3. Identifier and Generic Type Parameters (e.g., MyType<T>)
        sb.Append(typeSyntax.Identifier.Text);
        if (typeSyntax.TypeParameterList is not null)
        {
            sb.Append(typeSyntax.TypeParameterList);
        }

        // 4. Generic Constraints (e.g., where T : new())
        foreach (var constraintClause in typeSyntax.ConstraintClauses)
        {
            sb.Append(' ').Append(constraintClause);
        }

        return sb.ToString();
    }

    private static DiagnosticInfo? ValidateContainerIsPartial(in ClassDeclarationSyntax classNode, in INamedTypeSymbol classSymbol)
    {
        if (!classNode.Modifiers.Any(SyntaxKind.PartialKeyword))
        {
            return DiagnosticInfo.Create(Diagnostics.ContainerNotPartial, classNode.Identifier.GetLocation(), classSymbol.Name);
        }

        return null;
    }
}