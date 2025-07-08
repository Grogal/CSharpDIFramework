using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;

namespace CSharpDIFramework.SourceGenerators;

internal class GraphBuilder
{
    private readonly ServiceProviderDescription _description;
    private readonly List<Diagnostic> _diagnostics = new();
    private readonly Dictionary<ITypeSymbol, ServiceRegistration> _registrationMap = new(SymbolEqualityComparer.Default);
    private readonly Dictionary<ITypeSymbol, ResolvedService> _resolvedServices = new(SymbolEqualityComparer.Default);

    public GraphBuilder(ServiceProviderDescription description)
    {
        _description = description;

        // build cache of registrations
        foreach (ServiceRegistration? reg in description.Registrations)
        {
            if (_registrationMap.ContainsKey(reg.ServiceType))
            {
                _diagnostics.Add(
                    Diagnostic.Create(
                        Diagnostics.DuplicateRegistrationConstructors, reg.RegistrationLocation, reg.ServiceType.ToDisplayString()
                    )
                );
            }
            else
            {
                _registrationMap.Add(reg.ServiceType, reg);
            }
        }
    }

    public (ContainerBlueprint? Blueprint, ImmutableArray<Diagnostic> Diagnostics) Build()
    {
        if (_diagnostics.Any())
        {
            return (null, _diagnostics.ToImmutableArray());
        }

        foreach (ServiceRegistration? registration in _description.Registrations)
        {
            ResolveService(registration, new Stack<ITypeSymbol>());
        }

        if (_diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
        {
            return (null, _diagnostics.ToImmutableArray());
        }

        var blueprint = new ContainerBlueprint(
            _description.ContainerSymbol,
            _description.ContainerSymbol.Name,
            _description.ContainerSymbol.ContainingNamespace.IsGlobalNamespace
                ? null
                : _description.ContainerSymbol.ContainingNamespace.ToDisplayString(),
            _resolvedServices.Values.ToImmutableArray(),
            _description.DeclarationLocation,
            GetContainingTypeDeclarations(_description.ContainerSymbol)
        );

        return (blueprint, _diagnostics.ToImmutableArray());
    }

    public static ImmutableArray<string> GetContainingTypeDeclarations(INamedTypeSymbol classSymbol)
    {
        var declarations = new List<string>();
        INamedTypeSymbol? parent = classSymbol.ContainingType;
        while (parent != null)
        {
            declarations.Insert(0, $"public partial class {parent.Name}");
            parent = parent.ContainingType;
        }

        return declarations.ToImmutableArray();
    }

    private ResolvedService? ResolveService(ServiceRegistration registration, Stack<ITypeSymbol> path)
    {
        ITypeSymbol serviceType = registration.ServiceType;

        if (_resolvedServices.TryGetValue(serviceType, out ResolvedService? cached))
        {
            return cached;
        }

        // --- Cycle Detection ---
        if (path.Contains(serviceType, SymbolEqualityComparer.Default))
        {
            // Reverse as it Stack
            string cyclePath = string.Join(" -> ", path.Reverse().Select(s => s.Name)) + $" -> {serviceType.Name}";
            _diagnostics.Add(
                Diagnostic.Create(
                    Diagnostics.CyclicDependency, _registrationMap[path.Peek()].RegistrationLocation, path.Peek().Name, cyclePath
                )
            );
            return null;
        }

        path.Push(serviceType);

        IMethodSymbol? constructor = SelectConstructor(registration.ImplementationType);

        if (constructor is null)
        {
            path.Pop();
            return null;
        }

        List<ResolvedService> dependencies = ResolveParameters(constructor.Parameters, registration, path);

        var resolvedDecorators = new List<ResolvedDecorator>();
        foreach (INamedTypeSymbol? decoratorType in registration.DecoratorTypes)
        {
            IMethodSymbol? decoratorConstructor = SelectDecoratorConstructor(decoratorType, registration.ServiceType);
            if (decoratorConstructor is null)
            {
                continue;
            }

            List<ResolvedService> decoratorDependencies = ResolveParameters(decoratorConstructor.Parameters, registration, path, registration.ServiceType);
            resolvedDecorators.Add(new ResolvedDecorator(decoratorType, decoratorConstructor, decoratorDependencies.ToImmutableArray()));
        }

        path.Pop();

        if (_diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
        {
            return null;
        }

        var resolvedService = new ResolvedService(registration, constructor, dependencies.ToImmutableArray(), resolvedDecorators.ToImmutableArray());
        _resolvedServices[serviceType] = resolvedService;
        return resolvedService;
    }

    private List<ResolvedService> ResolveParameters(
        ImmutableArray<IParameterSymbol> parameters,
        ServiceRegistration parentRegistration,
        Stack<ITypeSymbol> path,
        ITypeSymbol? exclude = null)
    {
        var dependencies = new List<ResolvedService>();
        foreach (IParameterSymbol? parameter in parameters)
        {
            if (exclude != null && SymbolEqualityComparer.Default.Equals(parameter.Type, exclude))
            {
                continue;
            }

            if (!_registrationMap.TryGetValue(parameter.Type, out ServiceRegistration? dependencyRegistration))
            {
                _diagnostics.Add(
                    Diagnostic.Create(
                        Diagnostics.ServiceNotRegistered,
                        parameter.Locations.FirstOrDefault()
                        ?? parentRegistration.RegistrationLocation, parameter.Type.Name,
                        parentRegistration.ImplementationType.Name
                    )
                );
                continue;
            }

            ServiceLifetime effectiveParentLifetime = parentRegistration.Lifetime;
            if (effectiveParentLifetime > dependencyRegistration.Lifetime)
            {
                // This checks for both regular and decorator captive dependencies.
                // We determine if this is a decorator by checking if the implementation type matches the service type.
                bool isDecorator = !SymbolEqualityComparer.Default.Equals(
                    parentRegistration.ImplementationType, parentRegistration.ServiceType
                );

                DiagnosticDescriptor descriptor = isDecorator
                    ? Diagnostics.DecoratorCaptiveDependency
                    : Diagnostics.LifestyleMismatch;

                _diagnostics.Add(
                    Diagnostic.Create(
                        descriptor,
                        parameter.Locations.FirstOrDefault() ?? parentRegistration.RegistrationLocation, parentRegistration.ImplementationType.Name,
                        effectiveParentLifetime,
                        dependencyRegistration.ServiceType.Name,
                        dependencyRegistration.Lifetime
                    )
                );
                continue;
            }

            ResolvedService? dependency = ResolveService(dependencyRegistration, path);
            if (dependency != null)
            {
                dependencies.Add(dependency);
            }
        }

        return dependencies;
    }

    private IMethodSymbol? SelectConstructor(INamedTypeSymbol implementationType)
    {
        Location location = implementationType.Locations.FirstOrDefault() ?? Location.None;

        ImmutableArray<IMethodSymbol> publicConstructors =
            implementationType.Constructors
                              .Where(c => c.DeclaredAccessibility == Accessibility.Public)
                              .ToImmutableArray();

        if (publicConstructors.IsEmpty)
        {
            _diagnostics.Add(
                Diagnostic.Create(
                    Diagnostics.NoPublicConstructor,
                    location,
                    implementationType.ToDisplayString()
                )
            );
            return null;
        }

        ImmutableArray<IMethodSymbol> injectConstructors = publicConstructors
                                                           .Where(c => c.GetAttributes()
                                                                        .Any(a => a.AttributeClass?.ToDisplayString() == Constants.InjectAttributeName)
                                                           )
                                                           .ToImmutableArray();

        if (injectConstructors.Length > 1)
        {
            _diagnostics.Add(
                Diagnostic.Create(
                    Diagnostics.MultipleInjectConstructors,
                    location,
                    implementationType.ToDisplayString()
                )
            );
            return null;
        }

        if (injectConstructors.Length == 1)
        {
            return injectConstructors[0];
        }

        if (publicConstructors.Length == 1)
        {
            return publicConstructors[0];
        }

        int maxParams = publicConstructors.Max(c => c.Parameters.Length);
        ImmutableArray<IMethodSymbol> greediestConstructors = publicConstructors.Where(c => c.Parameters.Length == maxParams).ToImmutableArray();

        if (greediestConstructors.Length > 1)
        {
            _diagnostics.Add(
                Diagnostic.Create(
                    Diagnostics.AmbiguousConstructors, location, implementationType.ToDisplayString(), maxParams
                )
            );
            return null;
        }

        return greediestConstructors[0];
    }

    private IMethodSymbol? SelectDecoratorConstructor(INamedTypeSymbol decoratorType, ITypeSymbol serviceToDecorate)
    {
        Location location = decoratorType.Locations.FirstOrDefault() ?? Location.None;
        ImmutableArray<IMethodSymbol> candidates = decoratorType.Constructors
                                                                .Where(c => c.DeclaredAccessibility == Accessibility.Public &&
                                                                            c.Parameters.Count(p => SymbolEqualityComparer.Default.Equals(
                                                                                                   p.Type, serviceToDecorate
                                                                                               )
                                                                            ) == 1
                                                                )
                                                                .ToImmutableArray();

        if (candidates.IsEmpty)
        {
            _diagnostics.Add(
                Diagnostic.Create(
                    Diagnostics.DecoratorMissingDecoratedServiceParameter,
                    location,
                    decoratorType.Name,
                    serviceToDecorate.Name
                )
            );
            return null;
        }

        ImmutableArray<IMethodSymbol> injectConstructors = candidates.Where(c =>
                                                                                c.GetAttributes()
                                                                                 .Any(a =>
                                                                                          a.AttributeClass?.ToDisplayString() == Constants.InjectAttributeName
                                                                                 )
                                                                     )
                                                                     .ToImmutableArray();
        if (injectConstructors.Length > 1)
        {
            _diagnostics.Add(
                Diagnostic.Create(
                    Diagnostics.MultipleInjectConstructors,
                    location,
                    decoratorType.ToDisplayString()
                )
            );
            return null;
        }

        if (injectConstructors.Length == 1)
        {
            return injectConstructors[0];
        }

        if (candidates.Length == 1)
        {
            return candidates[0];
        }

        int maxParams = candidates.Max(c => c.Parameters.Length);
        ImmutableArray<IMethodSymbol> greediestConstructors = candidates.Where(c => c.Parameters.Length == maxParams).ToImmutableArray();
        if (greediestConstructors.Length > 1)
        {
            _diagnostics.Add(
                Diagnostic.Create(
                    Diagnostics.AmbiguousDecoratorConstructors,
                    location,
                    decoratorType.ToDisplayString(),
                    maxParams
                )
            );
            return null;
        }

        return greediestConstructors[0];
    }
}