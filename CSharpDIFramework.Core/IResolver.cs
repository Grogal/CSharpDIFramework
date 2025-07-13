namespace CSharpDIFramework;

/// <summary>
///     Defines a contract for resolving services from the container or a scope.
/// </summary>
public interface IResolver
{
    /// <summary>
    ///     Resolves a service of type T.
    /// </summary>
    TService Resolve<TService>();
}