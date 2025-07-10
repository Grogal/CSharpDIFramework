namespace CSharpDIFramework;

/// <summary>
///     Marks a class as a DI container for registration.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class RegisterContainerAttribute : Attribute { }