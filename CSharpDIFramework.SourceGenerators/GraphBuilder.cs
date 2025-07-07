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
            _description.ContainerSymbol.ContainingNamespace.IsGlobalNamespace ? null : _description.ContainerSymbol.ContainingNamespace.ToDisplayString(),
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

        var dependencies = new List<ResolvedService>();
        foreach (IParameterSymbol? parameter in constructor.Parameters)
        {
            if (!_registrationMap.TryGetValue(parameter.Type, out ServiceRegistration? dependencyRegistration))
            {
                _diagnostics.Add(
                    Diagnostic.Create(
                        Diagnostics.ServiceNotRegistered,
                        parameter.Locations.FirstOrDefault() ?? registration.RegistrationLocation,
                        parameter.Type.Name,
                        registration.ImplementationType.Name
                    )
                );
                continue;
            }

            if (registration.Lifetime > dependencyRegistration.Lifetime)
            {
                _diagnostics.Add(
                    Diagnostic.Create(
                        Diagnostics.LifestyleMismatch, // Your new descriptor
                        parameter.Locations.FirstOrDefault() ?? registration.RegistrationLocation,
                        registration.ServiceType.Name,
                        registration.Lifetime,
                        dependencyRegistration.ServiceType.Name,
                        dependencyRegistration.Lifetime
                    )
                );
            }

            ResolvedService? dependency = ResolveService(dependencyRegistration, path);
            if (dependency != null)
            {
                dependencies.Add(dependency);
            }
        }

        path.Pop();

        if (_diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
        {
            return null;
        }

        var resolvedService = new ResolvedService(registration, constructor, dependencies.ToImmutableArray());
        _resolvedServices[serviceType] = resolvedService;
        return resolvedService;
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
                    Diagnostics.NoPublicConstructor, location, implementationType.ToDisplayString()
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
                    Diagnostics.MultipleInjectConstructors, location, implementationType.ToDisplayString()
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
}