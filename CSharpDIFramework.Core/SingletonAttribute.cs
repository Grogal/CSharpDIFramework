namespace CSharpDIFramework;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
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

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public class TransientAttributeName : Attribute
{
    public TransientAttributeName(Type serviceType, Type implementationType)
    {
        ServiceType = serviceType;
        ImplementationType = implementationType;
    }

    public TransientAttributeName(Type concreteType)
    {
        ServiceType = concreteType;
        ImplementationType = concreteType;
    }

    public Type ServiceType { get; }
    public Type ImplementationType { get; }
}

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public class ScopeAttribute : Attribute
{
    public ScopeAttribute(Type serviceType, Type implementationType)
    {
        ServiceType = serviceType;
        ImplementationType = implementationType;
    }

    public ScopeAttribute(Type concreteType)
    {
        ServiceType = concreteType;
        ImplementationType = concreteType;
    }

    public Type ServiceType { get; }
    public Type ImplementationType { get; }
}