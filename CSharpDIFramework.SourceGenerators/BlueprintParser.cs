using System.Collections.Generic;
using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSharpDIFramework.SourceGenerators;

internal static class BlueprintParser
{
    private static INamedTypeSymbol s_disposableInterface = null!;

    public static (ServiceProviderDescription? Blueprint, ImmutableArray<Diagnostic> Diagnostics) Parse(GeneratorAttributeSyntaxContext context)
    {
        if (context.TargetNode is not ClassDeclarationSyntax classNode ||
            context.TargetSymbol is not INamedTypeSymbol classSymbol)
        {
            return (null, ImmutableArray<Diagnostic>.Empty);
        }

        Compilation compilation = context.SemanticModel.Compilation;
        s_disposableInterface = compilation.GetTypeByMetadataName("System.IDisposable")!;

        var diagnostics = new List<Diagnostic>();
        var registrationMap = new Dictionary<ITypeSymbol, ServiceRegistration>(SymbolEqualityComparer.Default);

        Diagnostic? diagnostic = BlueprintValidator.ValidateContainerIsPartial(classNode, classSymbol);
        if (diagnostic is not null)
        {
            diagnostics.Add(diagnostic);
            return (null, diagnostics.ToImmutableArray());
        }

        foreach (AttributeData? attributeData in classSymbol.GetAttributes())
        {
            if (attributeData.AttributeClass?.ToDisplayString() == Constants.DecorateAttributeName)
            {
                ParseDecoratorAttribute(attributeData, registrationMap, diagnostics, compilation);
            }
            else
            {
                ServiceRegistration? registration = ParseRegistrationAttribute(attributeData, diagnostics, compilation);
                if (registration != null)
                {
                    if (registrationMap.ContainsKey(registration.ServiceType))
                    {
                        diagnostics.Add(
                            Diagnostic.Create(
                                Diagnostics.DuplicateRegistrationConstructors, registration.RegistrationLocation, registration.ServiceType.ToDisplayString()
                            )
                        );
                    }
                    else
                    {
                        registrationMap.Add(registration.ServiceType, registration);
                    }
                }
            }
        }

        var description = new ServiceProviderDescription(
            classSymbol,
            registrationMap.Values.ToImmutableArray(),
            classNode.Identifier.GetLocation()
        );

        return (description, diagnostics.ToImmutableArray());
    }

    private static void ParseDecoratorAttribute(
        AttributeData attributeData,
        Dictionary<ITypeSymbol, ServiceRegistration> registrationMap,
        List<Diagnostic> diagnostics,
        Compilation compilation)
    {
        (ITypeSymbol? serviceType, INamedTypeSymbol? decoratorType) =
            ExtractServiceAndImplTypes(attributeData, diagnostics);

        // If same than we get 1 parameter
        if (serviceType is null || SymbolEqualityComparer.Default.Equals(decoratorType, serviceType))
        {
            return;
        }

        if (registrationMap.TryGetValue(serviceType, out ServiceRegistration? registrationToDecorate))
        {
            Conversion conversion = compilation.ClassifyConversion(decoratorType!, serviceType);
            if (conversion is { IsImplicit: false, IsIdentity: false })
            {
                diagnostics.Add(
                    Diagnostic.Create(
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
                    Diagnostic.Create(
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
                    Diagnostic.Create(
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
                Diagnostic.Create(
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
            case Constants.TransientAttributeName:
                lifetime = ServiceLifetime.Transient;
                break;
            case Constants.ScopedAttributeName:
                lifetime = ServiceLifetime.Scoped;
                break;
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

        bool isDisposable = implementationType.AllInterfaces.Contains(s_disposableInterface);

        return new ServiceRegistration(
            serviceType,
            implementationType,
            lifetime,
            attributeData.ApplicationSyntaxReference!.GetSyntax().GetLocation(),
            isDisposable
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