namespace CSharpDIFramework;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = false, AllowMultiple = true)]
public class SingletonAttribute : Attribute
{
    /// <summary>
    ///     Registers a service with a specific implementation.
    ///     e.g., [Singleton(typeof(IService), typeof(ServiceImpl))]
    /// </summary>
    public SingletonAttribute(Type serviceType, Type implementationType)
    {
        ServiceType = serviceType;
        ImplementationType = implementationType;
    }

    /// <summary>
    ///     Registers a concrete type that serves as its own implementation.
    ///     e.g., [Singleton(typeof(ConcreteService))]
    /// </summary>
    public SingletonAttribute(Type concreteType)
    {
        ServiceType = concreteType;
        ImplementationType = concreteType;
    }

    public Type ServiceType { get; }
    public Type ImplementationType { get; }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = false, AllowMultiple = true)]
public class TransientAttribute : Attribute
{
    public TransientAttribute(Type serviceType, Type implementationType)
    {
        ServiceType = serviceType;
        ImplementationType = implementationType;
    }

    public TransientAttribute(Type concreteType)
    {
        ServiceType = concreteType;
        ImplementationType = concreteType;
    }

    public Type ServiceType { get; }
    public Type ImplementationType { get; }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = false, AllowMultiple = true)]
public class ScopedAttribute : Attribute
{
    public ScopedAttribute(Type serviceType, Type implementationType)
    {
        ServiceType = serviceType;
        ImplementationType = implementationType;
    }

    public ScopedAttribute(Type concreteType)
    {
        ServiceType = concreteType;
        ImplementationType = concreteType;
    }

    public Type ServiceType { get; }
    public Type ImplementationType { get; }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = false, AllowMultiple = true)]
public class DecorateAttribute : Attribute
{
    public DecorateAttribute(Type serviceType, Type implementationType)
    {
        ServiceType = serviceType;
        ImplementationType = implementationType;
    }

    public Type ServiceType { get; }
    public Type ImplementationType { get; }
}