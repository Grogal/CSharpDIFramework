// Test cycles in dependency injection scenarios.

// ReSharper disable UnusedType.Global

// ReSharper disable UnusedParameter.Local
// ReSharper disable UnusedTypeParameter

#pragma warning disable CS9113 // Parameter is unread.
#if false
namespace CSharpDIFramework.Tests;

// --- SCENARIO 1: Simple Two-Service Cycle ---
public interface ISimpleA { }

public class SimpleA(ISimpleB b) : ISimpleA { }

public interface ISimpleB { }

public class SimpleB(ISimpleA a) : ISimpleB { }

// --- SCENARIO 2: Long Three-Service Cycle ---
public interface ILongA { }

public class LongA(ILongB b) : ILongA { }

public interface ILongB { }

public class LongB(ILongC c) : ILongB { }

public interface ILongC { }

public class LongC(ILongA a) : ILongC { }

// --- SCENARIO 3: Direct Self-Reference Cycle ---
public interface ISelfReference { }

public class SelfReference(ISelfReference self) : ISelfReference { }

// --- SCENARIO 4: Decorator-Induced Cycle ---
public interface IDecoratorTarget { }

public class DecoratorTarget(IDecoratorDependency dep) : IDecoratorTarget { }

public interface IDecoratorDependency { }

public class DecoratorDependency : IDecoratorDependency { }

// This decorator creates the cycle. It decorates IDecoratorDependency but needs IDecoratorTarget.
public class CycleDecorator(IDecoratorDependency decoratee, IDecoratorTarget target) : IDecoratorDependency;

// --- SCENARIO 5: Cross-Module Cycle ---
public interface IModuleAService { }

public class ModuleAService(IModuleBService b) : IModuleAService { }

// public class ModuleA : IModule
// {
//     #region IModule Implementation
//
//     public void Configure(IContainerBuilder builder)
//     {
//         builder.Register<IModuleAService, ModuleAService>().AsTransient();
//     }
//
//     #endregion
// }

public interface IModuleBService { }

public class ModuleBService(IModuleAService a) : IModuleBService { }

// public class ModuleB : IModule
// {
//     #region IModule Implementation
//
//     public void Configure(IContainerBuilder builder)
//     {
//         builder.Register<IModuleBService, ModuleBService>().AsTransient();
//     }
//
//     #endregion
// }

// --- SCENARIO 6: Acyclic Diamond Dependency ---
public interface IDiamondA { }

public class DiamondA(IDiamondB b, IDiamondC c) : IDiamondA { }

public interface IDiamondB { }

public class DiamondB(IDiamondD d) : IDiamondB { }

public interface IDiamondC { }

public class DiamondC(IDiamondD d) : IDiamondC { }

public interface IDiamondD { }

public class DiamondD : IDiamondD { }

// --- SCENARIO 7: Cycle Via Greedy Constructor ---
public interface IComplexService { }

public interface ITriggerService { }

public class TriggerService(IComplexService s) : ITriggerService { }

public class SafeDependency { }

public class ComplexService : IComplexService
{
    // The generator should ignore this constructor
    public ComplexService(SafeDependency d) { }

    // The generator should SELECT this constructor, which creates the cycle
    public ComplexService(SafeDependency d, ITriggerService t) { }
}

// --- SCENARIO 8: Cycle With Closed Generics ---
public interface IGenericCycleA<T> { }

public class GenericCycleA<T>(IGenericCycleB<T> b) : IGenericCycleA<T> { }

public interface IGenericCycleB<T> { }

public class GenericCycleB<T>(IGenericCycleA<T> a) : IGenericCycleB<T> { }

// --- SCENARIO 9: Cycle By Registration Override ---
public interface IOverriddenService { }

public class SafeImplementation : IOverriddenService { } // No dependencies

public class CyclicImplementation(IDependsOnOverridden d) : IOverriddenService { }

public interface IDependsOnOverridden { }

public class DependsOnOverridden(IOverriddenService s) : IDependsOnOverridden { }

// public class OverrideModule : IModule
// {
//     public void Configure(IContainerBuilder builder)
//     {
//         // This registration overrides the safe one and introduces the cycle.
//         builder.Register<IOverriddenService, CyclicImplementation>().AsTransient();
//     }
// }

// --- SCENARIO 10: Cycle Introduced by a Chained Decorator ---
public interface IMultiDecoratedService { }

public class MultiDecoratedImpl : IMultiDecoratedService { }

// A safe, passthrough decorator
public class DecoratorA(IMultiDecoratedService decoratee) : IMultiDecoratedService;

// This decorator introduces the cycle
public class DecoratorB(IMultiDecoratedService decoratee, ICycleIntroducer introducer) : IMultiDecoratedService;

public interface ICycleIntroducer { }

public class CycleIntroducer(IMultiDecoratedService s) : ICycleIntroducer { }

// --- SCENARIO 11: Cycle Involving IResolver Dependency ---
public interface IResolverUser { }

// This class depends on IResolver, which is fine.
// But it ALSO depends on IAnotherService, which creates the cycle.
// public class ResolverUser(IResolver resolver, IAnotherService another) : IResolverUser { }

public interface IAnotherService { }

public class AnotherService(IResolverUser user) : IAnotherService { }

// --- SCENARIO 12: Cycle Via Concrete Dependency ---
public interface IServiceThatStartsTheCycle { }

// This class depends on a concrete type, not an interface.
public class ServiceThatStartsTheCycle(ConcreteDependency c) : IServiceThatStartsTheCycle { }

// This class is NEVER registered in the container directly.
// The cycle detector must be smart enough to inspect its constructor.
public class ConcreteDependency
{
    // The dependency that closes the loop.
    public ConcreteDependency(IServiceThatStartsTheCycle service) { }
}

// --- SCENARIO 13: Acyclic Graph Using a Factory (Func<T>) ---
public interface ICycleBreakerA { }

// This class breaks the cycle by depending on a factory for B.
public class CycleBreakerA(Func<ICycleBreakerB> bFactory) : ICycleBreakerA { }

public interface ICycleBreakerB { }

public class CycleBreakerB(ICycleBreakerA a) : ICycleBreakerB { }

[RegisterContainer]
[Singleton(typeof(ISimpleA), typeof(SimpleA))]
[Singleton(typeof(ISimpleB), typeof(SimpleB))]
public partial class S1 { }

[RegisterContainer]
[Singleton(typeof(ILongA), typeof(LongA))]
[Singleton(typeof(ILongB), typeof(LongB))]
[Singleton(typeof(ILongC), typeof(LongC))]
public partial class S2 { }

[RegisterContainer]
[Singleton(typeof(ISelfReference), typeof(SelfReference))]
public partial class S3 { }

// public partial class S4 { }

// public partial class S5 { }

[RegisterContainer]
[Singleton(typeof(IDiamondA), typeof(DiamondA))]
[Singleton(typeof(IDiamondB), typeof(DiamondB))]
[Singleton(typeof(IDiamondC), typeof(DiamondC))]
[Singleton(typeof(IDiamondD), typeof(DiamondD))]
public partial class S6 { }

[RegisterContainer]
[Singleton(typeof(IComplexService), typeof(ComplexService))]
[Singleton(typeof(ITriggerService), typeof(TriggerService))]
[Singleton(typeof(SafeDependency), typeof(SafeDependency))]
public partial class S7 { }

// [RegisterContainer]
// [Singleton(typeof(IGenericCycleA<int>), typeof(GenericCycleA<int>))]
// [Singleton(typeof(IGenericCycleB<int>), typeof(GenericCycleB<int>))]
// public partial class S8 { }
//
// public partial class S9 { }
//
// public partial class S10 { }
//
[RegisterContainer]
[Singleton(typeof(ICycleIntroducer), typeof(CycleIntroducer))]
public partial class S11 { }

//
// public partial class S12 { }
//
[RegisterContainer]
[Singleton(typeof(IServiceThatStartsTheCycle), typeof(ServiceThatStartsTheCycle))]
public partial class S13 { }

[RegisterContainer]
[Singleton(typeof(ICycleBreakerA), typeof(CycleBreakerA))]
[Singleton(typeof(ICycleBreakerB), typeof(CycleBreakerB))]
public partial class S14 { }
#endif