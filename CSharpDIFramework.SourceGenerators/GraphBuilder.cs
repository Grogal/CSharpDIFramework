using System.Collections.Generic;
using System.Linq;

namespace CSharpDIFramework.SourceGenerators;

internal class GraphBuilder
{
    private readonly ServiceProviderDescription _description;
    private readonly List<DiagnosticInfo> _diagnostics = new();
    private readonly Dictionary<string, ServiceRegistration> _registrationMap = new();
    private readonly Dictionary<string, ResolvedService> _resolvedServices = new();

    public GraphBuilder(ServiceProviderDescription description)
    {
        _description = description;
        foreach (ServiceRegistration reg in description.Registrations)
        {
            _registrationMap.Add(reg.ServiceTypeFullName, reg);
        }
    }

    public (ContainerBlueprint? Blueprint, EquatableArray<DiagnosticInfo> Diagnostics) Build()
    {
        foreach (ServiceRegistration registration in _description.Registrations)
        {
            ResolveService(registration, new Stack<string>());
        }

        if (_diagnostics.Any(d => d.IsError))
        {
            return (null, new EquatableArray<DiagnosticInfo>(_diagnostics));
        }

        var blueprint = new ContainerBlueprint(
            _description.ContainerName,
            _description.Namespace,
            new EquatableArray<ResolvedService>(_resolvedServices.Values.ToList()),
            _description.ContainingTypeDeclarations
        );

        return (blueprint, new EquatableArray<DiagnosticInfo>(_diagnostics));
    }

    private ResolvedService? ResolveService(ServiceRegistration registration, Stack<string> path)
    {
        string serviceType = registration.ServiceTypeFullName;

        if (_resolvedServices.TryGetValue(serviceType, out ResolvedService? cached))
        {
            return cached;
        }

        if (path.Contains(serviceType))
        {
            string cyclePath = string.Join(" -> ", path.Reverse()) + $" -> {serviceType}";
            _diagnostics.Add(DiagnosticInfo.Create(Diagnostics.CyclicDependency, _registrationMap[path.Peek()].RegistrationLocation, path.Peek(), cyclePath));
            return null;
        }

        path.Push(serviceType);

        ConstructorInfo? constructor = SelectConstructor(registration.ImplementationType);
        if (constructor is null)
        {
            path.Pop();
            return null;
        }

        EquatableArray<ResolvedService> dependencies = ResolveParameters(constructor.ParameterTypeFullNames, registration, path);

        var resolvedDecorators = new List<ResolvedDecorator>();
        foreach (DecoratorInfo decoratorInfo in registration.Decorators)
        {
            ConstructorInfo? decoratorConstructor = SelectDecoratorConstructor(decoratorInfo, registration.ServiceTypeFullName);
            if (decoratorConstructor is null)
            {
                continue;
            }

            EquatableArray<ResolvedService> decoratorDependencies = ResolveParameters(
                decoratorConstructor.ParameterTypeFullNames, registration, path, registration.ServiceTypeFullName
            );
            resolvedDecorators.Add(new ResolvedDecorator(decoratorInfo, decoratorConstructor, decoratorDependencies));
        }

        path.Pop();

        if (_diagnostics.Any(d => d.IsError))
        {
            return null;
        }

        var resolvedService = new ResolvedService(registration, constructor, dependencies, new EquatableArray<ResolvedDecorator>(resolvedDecorators));
        _resolvedServices[serviceType] = resolvedService;
        return resolvedService;
    }

    private EquatableArray<ResolvedService> ResolveParameters(
        EquatableArray<string> parameterTypeNames,
        ServiceRegistration parentRegistration,
        Stack<string> path,
        string? decoratedServiceToExclude = null)
    {
        var dependencies = new List<ResolvedService>();
        foreach (string paramTypeName in parameterTypeNames)
        {
            if (decoratedServiceToExclude != null && paramTypeName == decoratedServiceToExclude)
            {
                continue;
            }

            if (!_registrationMap.TryGetValue(paramTypeName, out ServiceRegistration? dependencyRegistration))
            {
                _diagnostics.Add(
                    DiagnosticInfo.Create(
                        Diagnostics.ServiceNotRegistered, parentRegistration.RegistrationLocation, paramTypeName, parentRegistration.ImplementationType.FullName
                    )
                );
                continue;
            }

            ServiceLifetime effectiveParentLifetime = parentRegistration.Lifetime;
            if (effectiveParentLifetime > dependencyRegistration.Lifetime)
            {
                bool isDecorator = parentRegistration.ImplementationType.FullName != parentRegistration.ServiceTypeFullName;
                _diagnostics.Add(
                    DiagnosticInfo.Create(
                        isDecorator ? Diagnostics.DecoratorCaptiveDependency : Diagnostics.LifestyleMismatch,
                        parentRegistration.RegistrationLocation,
                        parentRegistration.ImplementationType.FullName,
                        effectiveParentLifetime.ToString(),
                        dependencyRegistration.ServiceTypeFullName,
                        dependencyRegistration.Lifetime.ToString()
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

        return new EquatableArray<ResolvedService>(dependencies);
    }

    private ConstructorInfo? SelectConstructor(ServiceImplementationType implementationType)
    {
        LocationInfo? location = implementationType.Location;
        ConstructorInfo[]? publicConstructors = implementationType.Constructors.GetArray();

        if (publicConstructors is null || publicConstructors.Length == 0)
        {
            _diagnostics.Add(DiagnosticInfo.Create(Diagnostics.NoPublicConstructor, location, implementationType.FullName));
            return null;
        }

        List<ConstructorInfo> injectConstructors = publicConstructors.Where(c => c.HasInjectAttribute).ToList();

        if (injectConstructors.Count > 1)
        {
            _diagnostics.Add(DiagnosticInfo.Create(Diagnostics.MultipleInjectConstructors, location, implementationType.FullName));
            return null;
        }

        if (injectConstructors.Count == 1)
        {
            return injectConstructors[0];
        }

        if (publicConstructors.Length == 1)
        {
            return publicConstructors[0];
        }

        int maxParams = publicConstructors.Max(c => c.ParameterTypeFullNames.Count);
        List<ConstructorInfo> greediestConstructors = publicConstructors.Where(c => c.ParameterTypeFullNames.Count == maxParams).ToList();

        if (greediestConstructors.Count > 1)
        {
            _diagnostics.Add(
                DiagnosticInfo.Create(
                    Diagnostics.AmbiguousConstructors,
                    location,
                    implementationType.FullName,
                    maxParams.ToString()
                )
            );
            return null;
        }

        return greediestConstructors[0];
    }

    private ConstructorInfo? SelectDecoratorConstructor(DecoratorInfo decoratorType, string serviceToDecorate)
    {
        LocationInfo? location = decoratorType.Location;
        ConstructorInfo[]? publicConstructors = decoratorType.Constructors.GetArray();

        if (publicConstructors is null || publicConstructors.Length == 0)
        {
            // This case should be caught by NoPublicConstructor on the decorator type itself, but we check defensively.
            _diagnostics.Add(DiagnosticInfo.Create(Diagnostics.NoPublicConstructor, location, decoratorType.FullName));
            return null;
        }

        List<ConstructorInfo> candidates = publicConstructors
                                           .Where(c => c.ParameterTypeFullNames.GetArray()?.Contains(serviceToDecorate) ?? false)
                                           .ToList();

        if (candidates.Count == 0)
        {
            _diagnostics.Add(DiagnosticInfo.Create(Diagnostics.DecoratorMissingDecoratedServiceParameter, location, decoratorType.FullName, serviceToDecorate));
            return null;
        }

        // Now apply constructor selection logic on the candidates
        List<ConstructorInfo> injectConstructors = candidates.Where(c => c.HasInjectAttribute).ToList();
        if (injectConstructors.Count > 1)
        {
            _diagnostics.Add(DiagnosticInfo.Create(Diagnostics.MultipleInjectConstructors, location, decoratorType.FullName));
            return null;
        }

        if (injectConstructors.Count == 1)
        {
            return injectConstructors[0];
        }

        if (candidates.Count == 1)
        {
            return candidates[0];
        }

        int maxParams = candidates.Max(c => c.ParameterTypeFullNames.Count);
        List<ConstructorInfo> greediestConstructors = candidates.Where(c => c.ParameterTypeFullNames.Count == maxParams).ToList();

        if (greediestConstructors.Count > 1)
        {
            _diagnostics.Add(
                DiagnosticInfo.Create(
                    Diagnostics.AmbiguousDecoratorConstructors,
                    location,
                    decoratorType.FullName,
                    maxParams.ToString()
                )
            );
            return null;
        }

        return greediestConstructors[0];
    }
}