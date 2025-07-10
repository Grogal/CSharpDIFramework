namespace CSharpDIFramework;

/// <summary>
///     Marks an interface as a DI module for registration.
/// </summary>
[AttributeUsage(AttributeTargets.Interface)]
public class RegisterModuleAttribute : Attribute { }

/// <summary>
///     Imports another module into the current module for DI registration.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true)]
public class ImportModuleAttribute(Type moduleType) : Attribute
{
    public Type ModuleType { get; } = moduleType;
}