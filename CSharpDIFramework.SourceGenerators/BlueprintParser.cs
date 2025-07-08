using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

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

        DiagnosticInfo? partialDiagnostic = BlueprintValidator.ValidateContainerIsPartial(classNode, classSymbol);
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
        ParseAttributesFromType(
            classSymbol, registrationMap, diagnostics, compilation,
            new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default)
        );

        var description = new ServiceProviderDescription(
            classSymbol,
            registrationMap.Values.ToImmutableArray(),
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
        // Try to add the module to the set. If it's already there, we've found a cycle.
        if (!visitedModules.Add(typeSymbol))
        {
            return;
        }

        foreach (AttributeData? attributeData in typeSymbol.GetAttributes())
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
                    if (moduleType.GetAttributes()
                                  .Any(a =>
                                           SymbolEqualityComparer.Default.Equals(a.AttributeClass, s_registerModuleAttribute)
                                  ))
                    {
                        ParseAttributesFromType(moduleType, registrationMap, diagnostics, compilation, visitedModules);
                    }
                    else
                    {
                        diagnostics.Add(
                            new DiagnosticInfo(
                                Diagnostics.ImportedTypeNotAModule,
                                attributeData.ApplicationSyntaxReference!.GetSyntax().GetLocation(),
                                moduleType.ToDisplayString()
                            )
                        );
                    }
                }
                else
                {
                    diagnostics.Add(
                        new DiagnosticInfo(
                            Diagnostics.IncorrectAttribute,
                            attributeData.ApplicationSyntaxReference!.GetSyntax().GetLocation(),
                            attributeData.AttributeClass?.Name
                        )
                    );
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
                            new DiagnosticInfo(
                                Diagnostics.DuplicateRegistrationConstructors,
                                registration.RegistrationLocation,
                                registration.ServiceTypeFullName
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

        // If same than we get 1 parameter
        if (serviceType is null || SymbolEqualityComparer.Default.Equals(decoratorType, serviceType))
        {
            return;
        }

        if (registrationMap.TryGetValue(serviceType.ToDisplayString(s_fullyQualifiedFormat), out ServiceRegistration? registrationToDecorate))
        {
            Conversion conversion = compilation.ClassifyConversion(decoratorType!, serviceType);
            if (conversion is { IsImplicit: false, IsIdentity: false })
            {
                diagnostics.Add(
                    new DiagnosticInfo(
                        Diagnostics.ImplementationNotAssignable,
                        attributeData.ApplicationSyntaxReference!.GetSyntax().GetLocation(),
                        decoratorType!.ToDisplayString(),
                        serviceType.ToDisplayString()
                    )
                );
                return;
            }

            if (decoratorType!.IsAbstract)
            {
                diagnostics.Add(
                    new DiagnosticInfo(
                        Diagnostics.ImplementationIsAbstract,
                        attributeData.ApplicationSyntaxReference!.GetSyntax().GetLocation(),
                        decoratorType.ToDisplayString()
                    )
                );
                return;
            }

            bool wasAdded = registrationToDecorate.DecoratorTypes.Add(decoratorType);
            if (!wasAdded)
            {
                diagnostics.Add(
                    new DiagnosticInfo(
                        Diagnostics.DuplicateDecoratorRegistration,
                        attributeData.ApplicationSyntaxReference!.GetSyntax().GetLocation(),
                        decoratorType.Name,
                        serviceType.Name
                    )
                );
            }
        }
        else
        {
            diagnostics.Add(
                new DiagnosticInfo(
                    Diagnostics.DecoratorForUnregisteredService,
                    attributeData.ApplicationSyntaxReference!.GetSyntax().GetLocation(),
                    decoratorType!.Name,
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
                new DiagnosticInfo(
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
                new DiagnosticInfo(
                    Diagnostics.ImplementationIsAbstract,
                    attributeData.ApplicationSyntaxReference!.GetSyntax().GetLocation(), implementationType.ToDisplayString()
                )
            );
            return null;
        }

        bool isDisposable = implementationType.AllInterfaces.Contains(s_disposableInterface);

        return new ServiceRegistration(
            serviceType.ToDisplayString(s_fullyQualifiedFormat),
            implementationType,
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
                diagnostics.Add(
                    new DiagnosticInfo(
                        Diagnostics.IncorrectAttribute, location, attributeData.AttributeClass?.Name
                    )
                );
                return (null, null);
        }

        return (serviceType, implementationType);
    }
}