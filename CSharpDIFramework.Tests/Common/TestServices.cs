using System.Text;

namespace CSharpDIFramework.Tests;

// --- Interfaces ---
public interface ISingletonService { }

public interface IScopedService { }

public interface ITransientService { }

public interface IEmptyService { }

public interface IDisposableService : IDisposable
{
    bool IsDisposed { get; }
}

public interface IServiceWithDependency
{
    ISingletonService Singleton { get; }
}

public class SingletonService : ISingletonService { }

public class ScopedService : IScopedService { }

public class TransientService : ITransientService { }

public class EmptyService : IEmptyService { }

public class DisposableSingleton : ISingletonService, IDisposableService
{
    #region IDisposable Implementation

    public void Dispose()
    {
        IsDisposed = true;
    }

    #endregion

    #region IDisposableService Implementation

    public bool IsDisposed { get; private set; }

    #endregion
}

public class DisposableScoped : IScopedService, IDisposableService
{
    #region IDisposable Implementation

    public void Dispose()
    {
        IsDisposed = true;
    }

    #endregion

    #region IDisposableService Implementation

    public bool IsDisposed { get; private set; }

    #endregion
}

public class DisposableTransient : ITransientService, IDisposableService
{
    #region IDisposable Implementation

    public void Dispose()
    {
        IsDisposed = true;
    }

    #endregion

    #region IDisposableService Implementation

    public bool IsDisposed { get; private set; }

    #endregion
}

public class DisposableService : IDisposableService
{
    #region IDisposable Implementation

    public void Dispose()
    {
        IsDisposed = true;
    }

    #endregion

    #region IDisposableService Implementation

    public bool IsDisposed { get; private set; }

    #endregion
}

public class ServiceWithDependency(ISingletonService singleton) : IServiceWithDependency
{
    #region IServiceWithDependency Implementation

    public ISingletonService Singleton { get; } = singleton;

    #endregion
}

public class ScopedDependsOnTransient(ITransientService transient) { }

public class SingletonDependsOnScoped(IScopedService scoped) { }

public class SingletonDependsOnTransient(ITransientService transient) { }

public interface IServiceWithCtors
{
    string UsedCtor { get; }
}

public class ServiceWithInjectCtor : IServiceWithCtors
{
    public ServiceWithInjectCtor()
    {
        UsedCtor = "Parameterless";
    }

    [Inject]
    public ServiceWithInjectCtor(ISingletonService s)
    {
        UsedCtor = "Inject";
    }

    public ServiceWithInjectCtor(ISingletonService s, IScopedService sc)
    {
        UsedCtor = "Greediest";
    }

    #region IServiceWithCtors Implementation

    public string UsedCtor { get; }

    #endregion
}

public class ServiceWithGreedyCtor : IServiceWithCtors
{
    public ServiceWithGreedyCtor()
    {
        UsedCtor = "Parameterless";
    }

    public ServiceWithGreedyCtor(ISingletonService s)
    {
        UsedCtor = "Greediest";
    }

    #region IServiceWithCtors Implementation

    public string UsedCtor { get; }

    #endregion
}

public interface IUnitOfWork : IDisposable { }

public class DisposableUnitOfWork : IUnitOfWork
{
    public bool IsDisposed { get; private set; }

    #region IDisposable Implementation

    public void Dispose()
    {
        IsDisposed = true;
    }

    #endregion
}

public interface IRepository : IDisposable { }

public class DisposableRepository : IRepository
{
    public DisposableRepository(IUnitOfWork uow)
    {
        UnitOfWork = uow;
    }

    public bool IsDisposed { get; private set; }
    public IUnitOfWork UnitOfWork { get; }

    #region IDisposable Implementation

    public void Dispose()
    {
        IsDisposed = true;
    }

    #endregion
}

public interface ILogger
{
    void Log(string message);
}

public class StringBuilderLogger : ILogger
{
    private readonly StringBuilder _sb = new();
    public string Output => _sb.ToString();

    #region ILogger Implementation

    public void Log(string message)
    {
        _sb.AppendLine(message);
    }

    #endregion
}

public interface IGreetingService
{
    string Greet();
}

public class GreetingService : IGreetingService
{
    #region IGreetingService Implementation

    public string Greet()
    {
        return "Hello";
    }

    #endregion
}

public class ExclamationDecorator : IGreetingService
{
    private readonly IGreetingService _inner;

    public ExclamationDecorator(IGreetingService inner)
    {
        _inner = inner;
    }

    #region IGreetingService Implementation

    public string Greet()
    {
        return $"{_inner.Greet()}!";
    }

    #endregion
}

// A decorator that has its own dependency (ILogger).
public class LoggingDecorator : IGreetingService
{
    private readonly IGreetingService _inner;
    private readonly ILogger _logger;

    public LoggingDecorator(IGreetingService inner, ILogger logger)
    {
        _inner = inner;
        _logger = logger;
    }

    #region IGreetingService Implementation

    public string Greet()
    {
        _logger.Log("Greeting...");
        return _inner.Greet();
    }

    #endregion
}

// A disposable decorator to test the disposal chain.
public class DisposableDecorator : IGreetingService, IDisposable
{
    private readonly IGreetingService _inner;

    public DisposableDecorator(IGreetingService inner)
    {
        _inner = inner;
    }

    public bool IsDisposed { get; private set; }

    #region IDisposable Implementation

    public void Dispose()
    {
        IsDisposed = true;
    }

    #endregion

    #region IGreetingService Implementation

    public string Greet()
    {
        return _inner.Greet();
    }

    #endregion
}

// A decorator with multiple constructors to test [Inject] selection.
public class DecoratorWithInjectCtor : IGreetingService
{
    private readonly IGreetingService _inner;

    public DecoratorWithInjectCtor(IGreetingService inner, ILogger logger)
    {
        _inner = inner;
        UsedCtor = "Greediest";
    }

    [Inject]
    public DecoratorWithInjectCtor(IGreetingService inner)
    {
        _inner = inner;
        UsedCtor = "Inject";
    }

    public string UsedCtor { get; }

    #region IGreetingService Implementation

    public string Greet()
    {
        return _inner.Greet();
    }

    #endregion
}