using System.Text;

namespace CSharpDIFramework.Tests;

// A single file containing all reusable services and modules for testing.

// =================================================================
// 1. Basic Service Interfaces & Implementations
// =================================================================

public interface ISingletonService { }

public class SingletonService : ISingletonService { }

public interface IScopedService { }

public class ScopedService : IScopedService { }

public interface ITransientService { }

public class TransientService : ITransientService { }

public interface IServiceWithDependency
{
    ISingletonService Singleton { get; }
}

public class ServiceWithDependency(ISingletonService singleton) : IServiceWithDependency
{
    #region IServiceWithDependency Implementation

    public ISingletonService Singleton { get; } = singleton;

    #endregion
}

// =================================================================
// 2. Services for Constructor Selection Tests
// =================================================================

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

// =================================================================
// 3. Disposable Services
// =================================================================

public interface IDisposableService : IDisposable
{
    bool IsDisposed { get; }
}

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

public interface IRepository
{
    IUnitOfWork UnitOfWork { get; }
}

public class DisposableRepository(IUnitOfWork uow) : IRepository, IDisposableService
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

    #region IRepository Implementation

    public IUnitOfWork UnitOfWork { get; } = uow;

    #endregion
}

// =================================================================
// 4. Services and Decorators for Decorator Pattern Tests
// =================================================================

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

public class ExclamationDecorator(IGreetingService inner) : IGreetingService
{
    #region IGreetingService Implementation

    public string Greet()
    {
        return $"{inner.Greet()}!";
    }

    #endregion
}

public class LoggingDecorator(IGreetingService inner, ILogger logger) : IGreetingService
{
    #region IGreetingService Implementation

    public string Greet()
    {
        logger.Log("Greeting...");
        return inner.Greet();
    }

    #endregion
}

public class DisposableDecorator(IGreetingService inner) : IGreetingService, IDisposableService
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

    #region IGreetingService Implementation

    public string Greet()
    {
        return inner.Greet();
    }

    #endregion
}

public class DecoratorWithInjectCtor : IGreetingService
{
    public DecoratorWithInjectCtor(IGreetingService inner, ILogger logger)
    {
        UsedCtor = "Greediest";
    }

    [Inject]
    public DecoratorWithInjectCtor(IGreetingService inner)
    {
        UsedCtor = "Inject";
    }

    public string UsedCtor { get; }

    #region IGreetingService Implementation

    public string Greet()
    {
        return "Decorated";
    }

    #endregion
}

public interface IOrderedService
{
    string ApplyOrder();
}

public class BaseOrderedService : IOrderedService
{
    #region IOrderedService Implementation

    public string ApplyOrder()
    {
        return "Base";
    }

    #endregion
}

public class DecoratorA(IOrderedService inner) : IOrderedService
{
    #region IOrderedService Implementation

    public string ApplyOrder()
    {
        return $"{inner.ApplyOrder()}-A";
    }

    #endregion
}

public class DecoratorB(IOrderedService inner) : IOrderedService
{
    #region IOrderedService Implementation

    public string ApplyOrder()
    {
        return $"{inner.ApplyOrder()}-B";
    }

    #endregion
}

// =================================================================
// 5. Services and Modules for Module Tests
// =================================================================

public interface IAppService { }

public class AppService(ILogger logger) : IAppService { }

public interface INotificationService { }

public class NotificationService(ILogger logger) : INotificationService { }

public interface IConfigService { }

public class ConfigService : IConfigService { }

public interface IMessageService
{
    string GetMessage();
}

public class HelloMessageService : IMessageService
{
    #region IMessageService Implementation

    public string GetMessage()
    {
        return "Hello";
    }

    #endregion
}

public class WorldMessageDecorator(IMessageService inner) : IMessageService
{
    #region IMessageService Implementation

    public string GetMessage()
    {
        return $"{inner.GetMessage()} World";
    }

    #endregion
}

[RegisterModule]
[Singleton(typeof(ILogger), typeof(StringBuilderLogger))]
public interface ILoggingModule { }

[RegisterModule]
[Singleton(typeof(IConfigService), typeof(ConfigService))]
public interface IConfigModule { }

[RegisterModule]
[ImportModule(typeof(IConfigModule))]
[ImportModule(typeof(ILoggingModule))]
public interface IChainedModule { }

[RegisterModule]
[ImportModule(typeof(ICyclicModuleB))]
[Singleton(typeof(ISingletonService), typeof(SingletonService))]
public interface ICyclicModuleA { }

[RegisterModule]
[ImportModule(typeof(ICyclicModuleA))]
[Scoped(typeof(IScopedService), typeof(ScopedService))]
public interface ICyclicModuleB { }

[RegisterModule]
[Singleton(typeof(IOrderedService), typeof(BaseOrderedService))]
[Decorate(typeof(IOrderedService), typeof(DecoratorA))]
public interface IPartialDecoratedModule { }

[RegisterModule]
[Singleton(typeof(IMessageService), typeof(HelloMessageService))]
public interface IMessageModule { }