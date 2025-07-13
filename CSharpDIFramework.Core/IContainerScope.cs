namespace CSharpDIFramework;

/// <summary>
///     Represents a limited scope from which services can be resolved.
///     Scoped and Transient services are disposed when the scope is disposed.
/// </summary>
public interface IContainerScope : IResolver, IDisposable
{
    IContainerScope CreateScope();
    IContainerScope CreateScope(string tag);
}