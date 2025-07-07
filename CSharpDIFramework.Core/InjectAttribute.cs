namespace CSharpDIFramework;

/// <summary>
///     Marks a specific constructor to be used by the dependency injection container
///     for creating an instance of a service.
/// </summary>
[AttributeUsage(AttributeTargets.Constructor)]
public class InjectAttribute : Attribute { }