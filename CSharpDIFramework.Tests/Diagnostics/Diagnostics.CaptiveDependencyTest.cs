// Tests for captive dependency scenarios. A captive dependency occurs when a service
// with a longer lifetime depends on a service with a shorter lifetime.

#if false
namespace CSharpDIFramework.Tests;

// --- LIFETIME DEFINITIONS ---
// Singleton: Lives for the entire application lifetime.
// Scoped: Lives for the lifetime of a scope (created by .CreateScope()).
// Transient: A new instance is created every time it's requested.
//
// The rule is: Singleton > Scoped > Transient. A service cannot depend on
// a service with a shorter lifetime.

// --- SCENARIO 1: Singleton depends on Scoped (ERROR) ---
public interface ISingletonServiceA { }

public class SingletonServiceA(IScopedServiceA service) : ISingletonServiceA { } // Problem here

public interface IScopedServiceA { }

public class ScopedServiceA : IScopedServiceA { }

// --- SCENARIO 2: Singleton depends on Transient (ERROR) ---
public interface ISingletonServiceB { }

public class SingletonServiceB(ITransientServiceB service) : ISingletonServiceB { } // Problem here

public interface ITransientServiceB { }

public class TransientServiceB : ITransientServiceB { }

// --- SCENARIO 3: Scoped depends on Transient (ERROR) ---
// This is the original test case, now using a direct dependency.
public interface IScopedServiceC { }

public class ScopedServiceC(ITransientServiceC service) : IScopedServiceC { } // Problem here

public interface ITransientServiceC { }

public class TransientServiceC : ITransientServiceC { }

// --- SCENARIO 4: Scoped depends on Transient via Decorator (ERROR) ---
// This is the original test case from the user prompt, preserved.
public interface IDecoratedDataService { }

public class DecoratedDataService : IDecoratedDataService { }

public class CachingDecorator(IDecoratedDataService decoratee, ITransientWorker worker) : IDecoratedDataService { } // Problem here

public interface ITransientWorker { }

public class TransientWorker : ITransientWorker { }

// --- SCENARIO 5: Valid Dependency (Singleton depends on Singleton) (NO ERROR) ---
public interface IAppShell { }

public class AppShell(IAppLogger logger) : IAppShell { }

public interface IAppLogger { }

public class AppLogger : IAppLogger { }

public interface IUnregisteredService { }

// This decorator correctly implements the service interface.
public class MyDecorator(IUnregisteredService decoratee) : IUnregisteredService { }

// --- SCENARIO 67: Decorator's dependency creates a cycle ---

public interface ICyclingServiceA { }

public class CyclingServiceA : ICyclingServiceA { }

public interface ICyclingServiceB { }

public class CyclingServiceBImpl : ICyclingServiceB
{
    public CyclingServiceBImpl(ICyclingServiceA serviceA) { }
}

public class CyclingDecorator : ICyclingServiceA
{
    // This decorator depends on the service that creates the cycle
    public CyclingDecorator(ICyclingServiceA inner, ICyclingServiceB serviceB) { }
}

// --- SCENARIO 8: Decorator with an ambiguous constructor ---

public interface IAmbiguousParamService { }

public class AmbiguousParamServiceImpl : IAmbiguousParamService { }

public class AmbiguousParamDecorator : IAmbiguousParamService
{
    // Invalid constructor: Which parameter is the decoratee?
    public AmbiguousParamDecorator(IAmbiguousParamService inner1, IAmbiguousParamService inner2) { }
}

// --- SCENARIO 9: Applying the same decorator more than once ---

public interface IDuplicateDecoratedService { }

public class DuplicateDecoratedServiceImpl : IDuplicateDecoratedService { }

public class ReusableDecorator : IDuplicateDecoratedService
{
    public ReusableDecorator(IDuplicateDecoratedService inner) { }
}

public interface ILongLivedService { }
public class LongLivedService : ILongLivedService { }

public interface IShortLivedService { } // Will be registered as Scoped
public class ShortLivedService : IShortLivedService { }

// This decorator is the problem. It decorates a Singleton but needs a Scoped service.
public class CaptiveDecorator : ILongLivedService
{
    private readonly ILongLivedService _inner;
    public CaptiveDecorator(ILongLivedService inner, IShortLivedService scoped) { }
}


[RegisterContainer]
[Singleton(typeof(ISingletonServiceA), typeof(SingletonServiceA))]
[Scoped(typeof(IScopedServiceA), typeof(ScopedServiceA))]
public partial class S1 { }

[RegisterContainer]
[Singleton(typeof(ISingletonServiceB), typeof(SingletonServiceB))]
[Transient(typeof(ITransientServiceB), typeof(TransientServiceB))]
public partial class S2 { }

[RegisterContainer]
[Scoped(typeof(IScopedServiceC), typeof(ScopedServiceC))]
[Transient(typeof(ITransientServiceC), typeof(TransientServiceC))]
public partial class S3 { }

[RegisterContainer]
[Scoped(typeof(IDecoratedDataService), typeof(DecoratedDataService))]
[Transient(typeof(ITransientWorker), typeof(TransientWorker))]
[Decorate(typeof(IDecoratedDataService), typeof(CachingDecorator))]
public partial class S4 { }

[RegisterContainer]
[Singleton(typeof(IAppShell), typeof(AppShell))]
[Singleton(typeof(IAppLogger), typeof(AppLogger))]
public partial class S5 { }

[RegisterContainer]
[Decorate(typeof(IUnregisteredService), typeof(MyDecorator))]
public partial class S6 { }

[RegisterContainer]
[Singleton(typeof(ICyclingServiceA), typeof(CyclingServiceA))]
[Decorate(typeof(ICyclingServiceA), typeof(CyclingDecorator))]
[Singleton(typeof(ICyclingServiceB), typeof(CyclingServiceBImpl))]
public partial class S7 { }

[RegisterContainer]
[Singleton(typeof(IAmbiguousParamService), typeof(AmbiguousParamServiceImpl))]
[Decorate(typeof(IAmbiguousParamService), typeof(AmbiguousParamDecorator))]
public partial class S8 { }

[RegisterContainer]
[Singleton(typeof(IDuplicateDecoratedService), typeof(DuplicateDecoratedServiceImpl))]
[Decorate(typeof(IDuplicateDecoratedService), typeof(ReusableDecorator))]
[Decorate(typeof(IDuplicateDecoratedService), typeof(ReusableDecorator))]
public partial class S9 { }

[RegisterContainer]
[Singleton(typeof(ILongLivedService), typeof(LongLivedService))]
[Scoped(typeof(IShortLivedService), typeof(ShortLivedService))]
[Decorate(typeof(ILongLivedService), typeof(CaptiveDecorator))] // <-- Error here
public partial class DecoratorCaptiveDependencyContainer { }

#endif