using CSharpDIFramework;

Console.WriteLine("Hello, World!");

internal partial class Parent<T>
    where T : class
{
    [RegisterContainer]
    public partial class MyContainer { }
}