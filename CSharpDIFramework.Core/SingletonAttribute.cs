// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace CSharpDIFramework;

/// <summary>
///     Registers a service as a singleton in the DI container.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = false, AllowMultiple = true)]
public class SingletonAttribute : Attribute
{
    /// <summary>
    ///     Registers a service with a specific implementation.
    ///     [Singleton(typeof(IService), typeof(ServiceImpl))]
    /// </summary>
    public SingletonAttribute(Type serviceType, Type implementationType)
    {
        ServiceType = serviceType;
        ImplementationType = implementationType;
    }

    /// <summary>
    ///     Registers a concrete type that serves as its own implementation.
    ///     [Singleton(typeof(ConcreteService))]
    /// </summary>
    public SingletonAttribute(Type concreteType)
    {
        ServiceType = concreteType;
        ImplementationType = concreteType;
    }

    public Type ServiceType { get; }
    public Type ImplementationType { get; }
}

/// <summary>
///     Registers a service as transient in the DI container.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = false, AllowMultiple = true)]
public class TransientAttribute(Type serviceType, Type implementationType) : Attribute
{
    public TransientAttribute(Type concreteType) : this(concreteType, concreteType) { }

    public Type ServiceType { get; } = serviceType;
    public Type ImplementationType { get; } = implementationType;
}

/// <summary>
///     Registers a service as scoped in the DI container.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = false, AllowMultiple = true)]
public class ScopedAttribute(Type serviceType, Type implementationType) : Attribute
{
    public ScopedAttribute(Type concreteType) : this(concreteType, concreteType) { }

    public Type ServiceType { get; } = serviceType;
    public Type ImplementationType { get; } = implementationType;
}

/// <summary>
///     Registers a service with a lifetime scoped to the nearest parent scope
///     that has a matching tag. The service will be a singleton within that scope.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = false, AllowMultiple = true)]
public class ScopedToAttribute : Attribute
{
    /// <summary>
    ///     Registers a service with a specific implementation, scoped to a tagged scope.
    /// </summary>
    /// <param name="tag">The scope tag.</param>
    /// <param name="serviceType">The service interface type.</param>
    /// <param name="implementationType">The service implementation type.</param>
    public ScopedToAttribute(string tag, Type serviceType, Type implementationType)
    {
        Tag = tag;
        ServiceType = serviceType;
        ImplementationType = implementationType;
    }

    /// <summary>
    ///     Registers a concrete type that serves as its own implementation, scoped to a tagged scope.
    /// </summary>
    /// <param name="tag">The scope tag.</param>
    /// <param name="concreteType">The concrete service type.</param>
    public ScopedToAttribute(string tag, Type concreteType)
        : this(tag, concreteType, concreteType) { }

    public string Tag { get; }
    public Type ServiceType { get; }
    public Type ImplementationType { get; }
}

/// <summary>
///     Registers a decorator for a service in the DI container.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = false, AllowMultiple = true)]
public class DecorateAttribute(Type serviceType, Type implementationType) : Attribute
{
    public Type ServiceType { get; } = serviceType;
    public Type ImplementationType { get; } = implementationType;
}