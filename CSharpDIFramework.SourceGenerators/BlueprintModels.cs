namespace CSharpDIFramework.SourceGenerators;

internal static class Constants
{
    public const string RegisterContainerAttributeName = "CSharpDIFramework.RegisterContainerAttribute";
    public const string SingletonAttributeName = "CSharpDIFramework.SingletonAttribute";
    public const string TransientAttributeName = "CSharpDIFramework.TransientAttribute";
    public const string ScopedAttributeName = "CSharpDIFramework.ScopedAttribute";
    public const string InjectAttributeName = "CSharpDIFramework.InjectAttribute";
    public const string DecorateAttributeName = "CSharpDIFramework.DecorateAttribute";
    public const string RegisterModuleAttributeName = "CSharpDIFramework.RegisterModuleAttribute";
    public const string ImportModuleAttributeName = "CSharpDIFramework.ImportModuleAttribute";

    public const string ResolverInterfaceName = "global::CSharpDIFramework.IResolver";
}

internal enum ServiceLifetime
{
    Transient,
    Scoped,
    Singleton
}

internal record ConstructorInfo(
    EquatableArray<string> ParameterTypeFullNames,
    bool HasInjectAttribute)
{
    public EquatableArray<string> ParameterTypeFullNames { get; } = ParameterTypeFullNames;
    public bool HasInjectAttribute { get; } = HasInjectAttribute;
}

internal record ServiceImplementationType(
    string FullName,
    LocationInfo? Location,
    EquatableArray<ConstructorInfo> Constructors)
{
    public string FullName { get; } = FullName;
    public LocationInfo? Location { get; } = Location;
    public EquatableArray<ConstructorInfo> Constructors { get; } = Constructors;
}

internal record DecoratorInfo(
    string FullName,
    LocationInfo? Location,
    EquatableArray<ConstructorInfo> Constructors)
{
    public string FullName { get; } = FullName;
    public LocationInfo? Location { get; } = Location;
    public EquatableArray<ConstructorInfo> Constructors { get; } = Constructors;
}

internal record ServiceRegistration(
    string ServiceTypeFullName,
    ServiceImplementationType ImplementationType,
    ServiceLifetime Lifetime,
    LocationInfo? RegistrationLocation,
    bool IsDisposable)
{
    public string ServiceTypeFullName { get; } = ServiceTypeFullName;
    public ServiceImplementationType ImplementationType { get; } = ImplementationType;
    public ServiceLifetime Lifetime { get; } = Lifetime;
    public LocationInfo? RegistrationLocation { get; } = RegistrationLocation;
    public bool IsDisposable { get; } = IsDisposable;
    public EquatableArray<DecoratorInfo> Decorators { get; set; } = EquatableArray<DecoratorInfo>.Empty;
}

internal record ServiceProviderDescription(
    string ContainerFullName,
    string ContainerName,
    string? Namespace,
    EquatableArray<string> ContainingTypeDeclarations,
    EquatableArray<ServiceRegistration> Registrations,
    LocationInfo? DeclarationLocation)
{
    public string ContainerFullName { get; } = ContainerFullName;
    public string ContainerName { get; } = ContainerName;
    public string? Namespace { get; } = Namespace;
    public EquatableArray<string> ContainingTypeDeclarations { get; } = ContainingTypeDeclarations;
    public EquatableArray<ServiceRegistration> Registrations { get; } = Registrations;
    public LocationInfo? DeclarationLocation { get; } = DeclarationLocation;
}

internal record ResolvedDecorator(
    DecoratorInfo SourceDecorator,
    ConstructorInfo SelectedConstructor,
    EquatableArray<ResolvedService> Dependencies
)
{
    public DecoratorInfo SourceDecorator { get; } = SourceDecorator;
    public ConstructorInfo SelectedConstructor { get; } = SelectedConstructor;
    public EquatableArray<ResolvedService> Dependencies { get; } = Dependencies;
}

internal record ResolvedService(
    ServiceRegistration SourceRegistration,
    ConstructorInfo SelectedConstructor,
    EquatableArray<ResolvedService> Dependencies,
    EquatableArray<ResolvedDecorator> Decorators
)
{
    public string ServiceTypeFullName => SourceRegistration.ServiceTypeFullName;
    public ServiceLifetime Lifetime => SourceRegistration.Lifetime;
    public ServiceRegistration SourceRegistration { get; } = SourceRegistration;
    public ConstructorInfo SelectedConstructor { get; } = SelectedConstructor;
    public EquatableArray<ResolvedService> Dependencies { get; } = Dependencies;
    public EquatableArray<ResolvedDecorator> Decorators { get; } = Decorators;
}

internal record ContainerBlueprint(
    string ContainerName,
    string? Namespace,
    EquatableArray<ResolvedService> Services,
    EquatableArray<string> ContainingTypeDeclarations
)
{
    public string ContainerName { get; } = ContainerName;
    public string? Namespace { get; } = Namespace;
    public EquatableArray<ResolvedService> Services { get; } = Services;
    public EquatableArray<string> ContainingTypeDeclarations { get; } = ContainingTypeDeclarations;
}