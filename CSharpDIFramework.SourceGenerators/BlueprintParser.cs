using System.Collections.Immutable;
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
    private static INamedTypeSymbol s_scopedToAttribute = null!;

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
        s_scopedToAttribute = compilation.GetTypeByMetadataName(Constants.ScopedToAttributeName)!;

        var registrationMap = new Dictionary<string, ServiceRegistration>();
        ParseContainer(classSymbol, registrationMap, diagnostics, compilation);

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

    private static void ParseContainer(
        INamedTypeSymbol containerSymbol,
        Dictionary<string, ServiceRegistration> finalRegistrationMap,
        List<DiagnosticInfo> diagnostics,
        Compilation compilation)
    {
        var visitedModules = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);

        // --- PASS 1: GATHER ALL ATTRIBUTES ---
        // Recursively collect all attributes from the container and its imported modules.
        // This ensures we have a complete picture before making any decisions.
        var allAttributes = new List<(AttributeData Attribute, LocationInfo? FallbackLocation)>();
        GatherAttributes(containerSymbol, allAttributes, diagnostics, visitedModules);

        // --- PASS 2: PROCESS LIFETIME REGISTRATIONS ---
        // This pass ONLY looks for lifetime attributes and creates temporary registrations.
        var tempRegistrations = new Dictionary<string, List<ServiceRegistration>>();
        foreach ((AttributeData attributeData, LocationInfo? fallbackLocation) in allAttributes)
        {
            ServiceRegistration? registration = TryParseLifetimeAttribute(attributeData, diagnostics, compilation, fallbackLocation);
            if (registration != null)
            {
                if (!tempRegistrations.TryGetValue(registration.ServiceTypeFullName, out List<ServiceRegistration>? list))
                {
                    list = new List<ServiceRegistration>();
                    tempRegistrations[registration.ServiceTypeFullName] = list;
                }

                list.Add(registration);
            }
        }

        // --- PASS 3: VALIDATE LIFETIMES AND POPULATE FINAL MAP ---
        // Now, validate the temporary registrations. This is where we detect conflicting lifetimes.
        foreach (KeyValuePair<string, List<ServiceRegistration>> kmp in tempRegistrations)
        {
            string? serviceName = kmp.Key;
            List<ServiceRegistration>? registrations = kmp.Value;

            if (registrations.Count > 1)
            {
                ServiceRegistration? scopedToReg =
                    registrations.FirstOrDefault(r => r.Lifetime == ServiceLifetime.ScopedToTag);
                ServiceRegistration? otherReg =
                    registrations.FirstOrDefault(r => r.Lifetime != ServiceLifetime.ScopedToTag);

                if (scopedToReg != null && otherReg != null)
                {
                    // SPECIFIC ERROR: A [ScopedTo] attribute is conflicting with another lifetime.
                    // This is where we use your NDI0018 diagnostic.
                    diagnostics.Add(
                        DiagnosticInfo.Create(
                            Diagnostics.ScopedToOnNonScopedService, // The NDI0018 diagnostic
                            scopedToReg.RegistrationLocation, // Location of the [ScopedTo] attribute
                            otherReg.Lifetime.ToString(), // The conflicting lifetime (e.g., "Singleton")
                            serviceName
                        )
                    );
                }
                else
                {
                    // GENERIC ERROR: Some other combination of multiple lifetimes (e.g., [Scoped] and [Transient]).
                    // We fall back to the NDI0019 diagnostic here.
                    diagnostics.Add(
                        DiagnosticInfo.Create(
                            Diagnostics.DuplicateLifetimeDefinition,
                            registrations[1].RegistrationLocation,
                            serviceName,
                            registrations[0].Lifetime.ToString(),
                            registrations[1].Lifetime.ToString()
                        )
                    );
                }
            }
            else if (finalRegistrationMap.ContainsKey(serviceName))
            {
                // This handles the case where two different modules register the same service.
                diagnostics.Add(DiagnosticInfo.Create(Diagnostics.DuplicateRegistrationConstructors, registrations[0].RegistrationLocation, serviceName));
            }
            else
            {
                finalRegistrationMap.Add(serviceName, registrations[0]);
            }
        }

        // --- PASS 4: PROCESS DECORATORS ---
        // Only after the final registration map is complete can we safely process decorators.
        foreach ((AttributeData attributeData, LocationInfo? fallbackLocation) in allAttributes)
        {
            if (SymbolEqualityComparer.Default.Equals(attributeData.AttributeClass, s_decorateAttribute))
            {
                ParseDecoratorAttribute(attributeData, finalRegistrationMap, diagnostics, compilation, fallbackLocation);
            }
        }
    }

    private static void GatherAttributes(
        INamedTypeSymbol typeSymbol,
        List<(AttributeData, LocationInfo?)> allAttributes,
        List<DiagnosticInfo> diagnostics,
        HashSet<INamedTypeSymbol> visitedModules,
        LocationInfo? fallbackLocation = null)
    {
        if (!visitedModules.Add(typeSymbol))
        {
            return;
        }

        ImmutableArray<AttributeData> attributes = typeSymbol.GetAttributes();

        // --- STEP 1: Process all module imports FIRST. ---
        // This recursive step ensures that attributes from imported modules are always
        // added to the list before the attributes of the current typeSymbol.
        foreach (AttributeData? attr in attributes)
        {
            if (SymbolEqualityComparer.Default.Equals(attr.AttributeClass, s_importModuleAttribute))
            {
                if (attr.ConstructorArguments.Length == 1 && attr.ConstructorArguments[0].Value is INamedTypeSymbol moduleType)
                {
                    if (moduleType.GetAttributes()
                                  .Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, s_registerModuleAttribute)))
                    {
                        var importLocation = LocationInfo.CreateFrom(attr.ApplicationSyntaxReference?.GetSyntax().GetLocation());
                        GatherAttributes(moduleType, allAttributes, diagnostics, visitedModules, importLocation);
                    }
                    else
                    {
                        diagnostics.Add(
                            DiagnosticInfo.Create(
                                Diagnostics.ImportedTypeNotAModule,
                                attr.ApplicationSyntaxReference!.GetSyntax().GetLocation(),
                                moduleType.ToDisplayString()
                            )
                        );
                    }
                }
            }
        }

        // --- STEP 2: Add the current type's attributes LAST. ---
        // We add all attributes EXCEPT for the ImportModule attributes, which we have already processed.
        foreach (AttributeData? attr in attributes)
        {
            if (!SymbolEqualityComparer.Default.Equals(attr.AttributeClass, s_importModuleAttribute))
            {
                allAttributes.Add((attr, fallbackLocation));
            }
        }
    }

    private static void ParseDecoratorAttribute(
        AttributeData attributeData,
        Dictionary<string, ServiceRegistration> registrationMap,
        List<DiagnosticInfo> diagnostics,
        Compilation compilation,
        LocationInfo? fallbackLocation)
    {
        LocationInfo? registrationLocation =
            LocationInfo.CreateFrom(attributeData.ApplicationSyntaxReference?.GetSyntax().GetLocation()) ?? fallbackLocation;

        (ITypeSymbol? serviceType, INamedTypeSymbol? decoratorType) =
            ExtractServiceAndImplTypes(attributeData, diagnostics, registrationLocation, false);

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
                        Diagnostics.ImplementationNotAssignable, registrationLocation,
                        decoratorType.ToDisplayString(), serviceType.ToDisplayString()
                    )
                );
                return;
            }

            if (decoratorType.IsAbstract)
            {
                diagnostics.Add(
                    DiagnosticInfo.Create(
                        Diagnostics.ImplementationIsAbstract, registrationLocation,
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
                        Diagnostics.DuplicateDecoratorRegistration, registrationLocation, decoratorType.Name,
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
                    registrationLocation,
                    decoratorType.Name,
                    serviceType.Name
                )
            );
        }
    }

    private static ServiceRegistration? TryParseLifetimeAttribute(
        AttributeData attributeData,
        List<DiagnosticInfo> diagnostics,
        Compilation compilation,
        LocationInfo? fallbackLocation)
    {
        LocationInfo? registrationLocation = LocationInfo.CreateFrom(attributeData.ApplicationSyntaxReference?.GetSyntax().GetLocation()) ?? fallbackLocation;
        INamedTypeSymbol? attributeClassSymbol = attributeData.AttributeClass;
        ServiceLifetime lifetime;
        string? scopeTag = null;
        var isScopedToTagAttr = false;

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
        else if (SymbolEqualityComparer.Default.Equals(attributeClassSymbol, s_scopedToAttribute))
        {
            lifetime = ServiceLifetime.ScopedToTag;
            isScopedToTagAttr = true;
            if (attributeData.ConstructorArguments.Length > 0 && attributeData.ConstructorArguments[0].Value is { } tag)
            {
                scopeTag = tag.ToString();
            }
        }
        else
        {
            return null; // Not a lifetime attribute.
        }

        (ITypeSymbol? serviceType, INamedTypeSymbol? implementationType) =
            ExtractServiceAndImplTypes(attributeData, diagnostics, registrationLocation, isScopedToTagAttr);

        if (serviceType is null || implementationType is null)
        {
            return null;
        }

        if (!compilation.ClassifyConversion(implementationType, serviceType).IsImplicit)
        {
            diagnostics.Add(
                DiagnosticInfo.Create(
                    Diagnostics.ImplementationNotAssignable, registrationLocation, implementationType.ToDisplayString(), serviceType.ToDisplayString()
                )
            );
            return null;
        }

        if (implementationType.IsAbstract)
        {
            diagnostics.Add(DiagnosticInfo.Create(Diagnostics.ImplementationIsAbstract, registrationLocation, implementationType.ToDisplayString()));
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
            registrationLocation,
            isDisposable
        ) { ScopeTag = scopeTag };
    }

    private static (ITypeSymbol? serviceType, INamedTypeSymbol? implementationType) ExtractServiceAndImplTypes(
        AttributeData attributeData,
        List<DiagnosticInfo> diagnostics,
        LocationInfo? location,
        bool isScopedTo)
    {
        ITypeSymbol? serviceType;
        INamedTypeSymbol? implementationType;

        int typeArgOffset = isScopedTo ? 1 : 0;

        switch (attributeData.ConstructorArguments.Length - typeArgOffset)
        {
            case 2 when attributeData.ConstructorArguments[typeArgOffset].Value is ITypeSymbol st &&
                        attributeData.ConstructorArguments[typeArgOffset + 1].Value is INamedTypeSymbol it:
                serviceType = st;
                implementationType = it;
                break;
            case 1 when attributeData.ConstructorArguments[typeArgOffset].Value is INamedTypeSymbol ct:
                serviceType = ct;
                implementationType = ct;
                break;
            default:
                diagnostics.Add(DiagnosticInfo.Create(Diagnostics.IncorrectAttribute, location, attributeData.AttributeClass?.Name!));
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
    ///     Gets the full declarations of all containing types for a given symbol, ensuring they are marked as partial.
    ///     This is crucial for generating code for nested container classes.
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
    ///     Reconstructs the source-code signature of a type declaration from its symbol.
    ///     Example: "public partial static record MyRecordT where T: new()"
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
        var hasPartial = false;
        foreach (SyntaxToken modifier in typeSyntax.Modifiers)
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
        foreach (TypeParameterConstraintClauseSyntax constraintClause in typeSyntax.ConstraintClauses)
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