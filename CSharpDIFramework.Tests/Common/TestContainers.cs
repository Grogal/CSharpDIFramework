namespace CSharpDIFramework.Tests;

// A container with no registrations.
[RegisterContainer]
public partial class EmptyContainer { }

// A container with all basic lifetime registrations.
[RegisterContainer]
[Singleton(typeof(ISingletonService), typeof(SingletonService))]
[Scoped(typeof(IScopedService), typeof(ScopedService))]
[Transient(typeof(ITransientService), typeof(TransientService))]
public partial class AllLifetimesContainer { }

// A container for testing dependency resolution.
[RegisterContainer]
[Singleton(typeof(ISingletonService), typeof(SingletonService))]
[Transient(typeof(IServiceWithDependency), typeof(ServiceWithDependency))]
public partial class DependencyContainer { }

// A container for testing disposal.
[RegisterContainer]
[Scoped(typeof(IDisposableService), typeof(DisposableService))]
public partial class DisposalContainer { }

// A container for testing disposal of all lifetimes
[RegisterContainer]
[Singleton(typeof(ISingletonService), typeof(DisposableSingleton))]
[Scoped(typeof(IScopedService), typeof(DisposableScoped))]
[Transient(typeof(ITransientService), typeof(DisposableTransient))]
public partial class AllLifetimesDisposalContainer { }

[RegisterContainer]
[Singleton(typeof(ISingletonService), typeof(SingletonService))]
[Scoped(typeof(IScopedService), typeof(ScopedService))]
[Transient(typeof(IServiceWithCtors), typeof(ServiceWithInjectCtor))]
public partial class InjectCtorContainer { }

[RegisterContainer]
[Singleton(typeof(ISingletonService), typeof(SingletonService))]
[Transient(typeof(IServiceWithCtors), typeof(ServiceWithGreedyCtor))]
public partial class GreedyCtorContainer { }

[RegisterContainer]
[Scoped(typeof(IUnitOfWork), typeof(DisposableUnitOfWork))]
[Transient(typeof(IRepository), typeof(DisposableRepository))]
public partial class NestedValidDisposalContainer { }

[RegisterContainer]
[Singleton(typeof(IGreetingService), typeof(GreetingService))]
[Decorate(typeof(IGreetingService), typeof(ExclamationDecorator))]
public partial class SimpleDecoratorContainer { }

[RegisterContainer]
[Singleton(typeof(IGreetingService), typeof(GreetingService))]
[Decorate(typeof(IGreetingService), typeof(ExclamationDecorator))]
[Decorate(typeof(IGreetingService), typeof(LoggingDecorator))] // LoggingDecorator is last/outermost
[Singleton(typeof(ILogger), typeof(StringBuilderLogger))]
public partial class ChainedDecoratorContainer { }

[RegisterContainer]
[Transient(typeof(IGreetingService), typeof(GreetingService))] // Service is Transient
[Decorate(typeof(IGreetingService), typeof(ExclamationDecorator))]
public partial class DecoratorLifetimeContainer { }

[RegisterContainer]
[Scoped(typeof(IGreetingService), typeof(GreetingService))]
[Decorate(typeof(IGreetingService), typeof(DisposableDecorator))]
public partial class DecoratorDisposalContainer { }

[RegisterContainer]
[Singleton(typeof(IGreetingService), typeof(GreetingService))]
[Decorate(typeof(IGreetingService), typeof(DecoratorWithInjectCtor))]
[Singleton(typeof(ILogger), typeof(StringBuilderLogger))] // Needed for the greedier ctor
public partial class DecoratorInjectCtorContainer { }

[RegisterModule]
[Singleton(typeof(ILogger), typeof(ConsoleLogger))]
public interface ILoggingModule { }

[RegisterContainer]
[ImportModule(typeof(ILoggingModule))]
[Transient(typeof(IAppService), typeof(AppService))]
public partial class ModularContainer { }

[RegisterModule]
[Singleton(typeof(ILogger), typeof(ModuleLogger))]
public interface ISharedLoggingModule { }

[RegisterContainer]
[ImportModule(typeof(ISharedLoggingModule))]
[Transient(typeof(INotificationService), typeof(NotificationService))]
public partial class ContainerWithModuleDependency { }

[RegisterModule]
[Singleton(typeof(IMessageService), typeof(HelloMessageService))] // Base service in module
public interface IMessageModule { }

[RegisterContainer]
[ImportModule(typeof(IMessageModule))]
[Decorate(typeof(IMessageService), typeof(WorldMessageDecorator))] // Decorator in container
public partial class ContainerDecoratingModuleService { }

[RegisterModule]
[Singleton(typeof(IConfigService), typeof(ConfigService))]
public interface IBaseModule { }

[RegisterModule]
[ImportModule(typeof(IBaseModule))] // This module imports the base one
[Singleton(typeof(ILogger), typeof(ModuleLogger))]
public interface IChainedLoggingModule { }

[RegisterContainer]
[ImportModule(typeof(IChainedLoggingModule))] // Container imports the chained module
public partial class NestedModuleContainer { }

[RegisterModule]
[ImportModule(typeof(ICyclicModuleB))]
[Singleton(typeof(ISingletonService), typeof(SingletonService))]
public interface ICyclicModuleA { }

[RegisterModule]
[ImportModule(typeof(ICyclicModuleA))]
[Scoped(typeof(IScopedService), typeof(ScopedService))]
public interface ICyclicModuleB { }

[RegisterContainer]
[ImportModule(typeof(ICyclicModuleA))] // Import one of the cyclic modules
public partial class CyclicModuleContainer { }

[RegisterContainer]
[Singleton(typeof(IOrderedService), typeof(BaseService))]
[Decorate(typeof(IOrderedService), typeof(DecoratorA))] // Applied first
[Decorate(typeof(IOrderedService), typeof(DecoratorB))] // Applied second (should be outermost)
public partial class DecoratorOrderContainer { }

// Module provides the base service and the first decorator
[RegisterModule]
[Singleton(typeof(IOrderedService), typeof(BaseService))]
[Decorate(typeof(IOrderedService), typeof(DecoratorA))]
public interface IPartialDecoratedModule { }

// Container imports the module and adds another decorator
[RegisterContainer]
[ImportModule(typeof(IPartialDecoratedModule))]
[Decorate(typeof(IOrderedService), typeof(DecoratorB))]
public partial class CombinedDecoratorContainer { }