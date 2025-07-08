namespace CSharpDIFramework;

[AttributeUsage(AttributeTargets.Interface)]
public class RegisterModuleAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true)]
public class ImportModuleAttribute(Type moduleType) : Attribute
{
    public Type ModuleType { get; } = moduleType;
}