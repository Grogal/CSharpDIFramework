// ReSharper disable once CheckNamespace

namespace CSharpDIFramework;

/// <summary>
///     Represents the root DI container, which can create scopes and resolve services.
/// </summary>
public interface IContainer : IResolver, IDisposable
{
    /// <summary>
    ///     Creates a new, limited scope from which scoped and transient services can be resolved.
    /// </summary>
    IContainerScope CreateScope();

    IContainerScope CreateScope(string tag);
}