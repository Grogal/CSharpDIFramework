using System.Text;

// ReSharper disable UnusedParameter.Local

// ReSharper disable UnusedMember.Global
#pragma warning disable CS9113 // Parameter is unread.

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
// 1a. Services for IContainer Injection Test
// =================================================================

public interface ISingletonWithContainerDep
{
    IContainer Container { get; }
}

public class SingletonWithContainerDep : ISingletonWithContainerDep
{
    public SingletonWithContainerDep(IContainer container)
    {
        Container = container;
    }

    #region ISingletonWithContainerDep Implementation

    public IContainer Container { get; }

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

// =================================================================
// 6. Services for Closed Generic Tests
// =================================================================

// --- Entities for the generic repository ---
public class User
{
    public Guid Id { get; set; }
}

public class Product
{
    public int Id { get; set; }
}

// --- Generic Service Interface ---
public interface IRepository<T>
{
    T GetById(object id);
}

// --- Generic Service Implementation ---
public class EfRepository<T> : IRepository<T>
{
    #region IRepository<T> Implementation

    public T GetById(object id)
    {
        // Dummy implementation for testing
        return default!;
    }

    #endregion
}

// --- Service that depends on a closed generic ---
public interface IUserService
{
    IRepository<User> UserRepository { get; }
}

public class UserService(IRepository<User> userRepository) : IUserService
{
    #region IUserService Implementation

    public IRepository<User> UserRepository { get; } = userRepository;

    #endregion
}

// --- Decorator for a generic service ---
public class AuditingRepositoryDecorator<T>(IRepository<T> inner, ILogger logger) : IRepository<T>
{
    #region IRepository<T> Implementation

    public T GetById(object id)
    {
        logger.Log($"AUDIT: Getting {typeof(T).Name} with ID {id}");
        return inner.GetById(id);
    }

    #endregion
}

// =================================================================
// 7. Services for Nullable (Optional) Dependency Tests
// =================================================================

// An optional dependency that may or may not be registered.
public interface IOptionalDependency { }

public class OptionalDependency : IOptionalDependency { }

// A service that declares its dependency as nullable.
public interface IServiceWithOptionalDependency
{
    IOptionalDependency? InjectedDependency { get; }
}

public class ServiceWithOptionalDependency : IServiceWithOptionalDependency
{
    public ServiceWithOptionalDependency(IOptionalDependency? optionalDependency)
    {
        InjectedDependency = optionalDependency;
    }

    #region IServiceWithOptionalDependency Implementation

    public IOptionalDependency? InjectedDependency { get; }

    #endregion
}

// =================================================================
// 8. Services for Advanced/Unsupported Scenarios
// =================================================================

// A concrete class used to test explicit null registrations.
public class NullableConcreteService
{
    public string Message => "I am not null";
}

// A service that depends on the concrete nullable class.
public interface IServiceWithConcreteNullableDep
{
    NullableConcreteService? InjectedService { get; }
}

public class ServiceWithConcreteNullableDep(NullableConcreteService? service) : IServiceWithConcreteNullableDep
{
    #region IServiceWithConcreteNullableDep Implementation

    public NullableConcreteService? InjectedService { get; } = service;

    #endregion
}

public class SimpleFactory
{
    private readonly IResolver _resolver;

    public SimpleFactory(IResolver resolver)
    {
        _resolver = resolver;
    }

    public T Create<T>()
        where T : class
    {
        return _resolver.Resolve<T>();
    }
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