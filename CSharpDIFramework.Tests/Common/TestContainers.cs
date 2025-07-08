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