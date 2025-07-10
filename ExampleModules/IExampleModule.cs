using CSharpDIFramework;

namespace ExampleModules;

public class ServiceFromModule
{
    public string GetMessage()
    {
        return "Hello from ExampleModules!";
    }
}

[RegisterModule]
[Singleton(typeof(ServiceFromModule))]
public interface IExampleModule { }