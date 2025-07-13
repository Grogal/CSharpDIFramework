// This file contains service and container configurations that are designed to
// produce specific diagnostic errors from the source generator. It is excluded
// from the build by the #if false directive and serves as documentation and a
// source for manual or future automated diagnostic testing.

// ReSharper disable InconsistentNaming

#if false
namespace CSharpDIFramework.Tests.DiagnosticScenarios;

#region NDI0001: Container must be partial

// =================================================================

// EXPECT: NDI0001 at class declaration because the container class is not marked as partial.
[RegisterContainer]
public class Container_NotPartial { }

#endregion

#region NDI0003: No suitable public constructor found

// =================================================================

public interface IServiceWithNoPublicCtor { }

public class ServiceWithNoPublicCtor : IServiceWithNoPublicCtor
{
    // No public constructor available for the generator to use.
    internal ServiceWithNoPublicCtor() { }
}

// EXPECT: NDI0003 for ServiceWithNoPublicCtor.
[RegisterContainer]
[Singleton(typeof(IServiceWithNoPublicCtor), typeof(ServiceWithNoPublicCtor))]
public partial class Container_NoPublicConstructor { }

#endregion

#region NDI0004: Cyclic dependency detected

// =================================================================

// --- SCENARIO 1: Simple Two-Service Cycle ---
public interface ICycleA { }

public class CycleA(ICycleB b) : ICycleA { }

public interface ICycleB { }

public class CycleB(ICycleA a) : ICycleB { }

// EXPECT: NDI0004 at ICycleA or ICycleB registration. Path: CycleA -> CycleB -> CycleA
[RegisterContainer]
[Singleton(typeof(ICycleA), typeof(CycleA))]
[Singleton(typeof(ICycleB), typeof(CycleB))]
public partial class Container_SimpleCycle { }

// --- SCENARIO 2: Decorator-Induced Cycle ---
public interface IDecoratorCycleTarget { }

public class DecoratorCycleTarget(IDecoratorCycleDependency dep) : IDecoratorCycleTarget { }

public interface IDecoratorCycleDependency { }

public class DecoratorCycleDependency : IDecoratorCycleDependency { }

// This decorator creates the cycle by depending on the service that depends on it.
public class CycleInducingDecorator(IDecoratorCycleDependency decoratee, IDecoratorCycleTarget target) : IDecoratorCycleDependency;

// EXPECT: NDI0004. Path: DecoratorCycleTarget -> IDecoratorCycleDependency -> DecoratorCycleTarget
[RegisterContainer]
[Singleton(typeof(IDecoratorCycleTarget), typeof(DecoratorCycleTarget))]
[Singleton(typeof(IDecoratorCycleDependency), typeof(DecoratorCycleDependency))]
[Decorate(typeof(IDecoratorCycleDependency), typeof(CycleInducingDecorator))]
public partial class Container_DecoratorCycle { }

// --- SCENARIO 3: Cycle Across Modules ---
[RegisterModule]
[Singleton(typeof(IModuleCycleA), typeof(ModuleCycleA))]
public interface IModuleA_ForCycle { }

public interface IModuleCycleA { }

public class ModuleCycleA(IModuleCycleB b) : IModuleCycleA { }

[RegisterModule]
[Singleton(typeof(IModuleCycleB), typeof(ModuleCycleB))]
public interface IModuleB_ForCycle { }

public interface IModuleCycleB { }

public class ModuleCycleB(IModuleCycleA a) : IModuleCycleB { }

// EXPECT: NDI0004. The cycle is formed by combining registrations from both modules.
[RegisterContainer]
[ImportModule(typeof(IModuleA_ForCycle))]
[ImportModule(typeof(IModuleB_ForCycle))]
public partial class Container_CrossModuleCycle { }

#endregion

#region NDI0005: Service not registered

// =================================================================

// --- SCENARIO 1: Direct missing dependency ---
public interface IRegisteredService { }

public class RegisteredService(IUnregisteredDependency dep) : IRegisteredService { }

public interface IUnregisteredDependency { } // Never registered in the container

// EXPECT: NDI0005 because IUnregisteredDependency is required by RegisteredService but not registered.
[RegisterContainer]
[Singleton(typeof(IRegisteredService), typeof(RegisteredService))]
public partial class Container_MissingDependency { }

// --- SCENARIO 2: Missing dependency in a module ---
[RegisterModule]
[Singleton(typeof(IServiceWithMissingDep), typeof(ServiceWithMissingDep))]
public interface IModuleWithMissingDep { }

public interface IServiceWithMissingDep { }

public class ServiceWithMissingDep(IAnotherUnregisteredDep dep) : IServiceWithMissingDep { }

public interface IAnotherUnregisteredDep { } // Not registered in the module or the container

// EXPECT: NDI0005 when the container imports the module and tries to resolve the graph.
[RegisterContainer]
[ImportModule(typeof(IModuleWithMissingDep))]
public partial class Container_MissingDependencyInModule { }

#endregion

#region NDI0006: Implementation type not assignable

// =================================================================

public interface IServiceType { }

public class UnrelatedImplementation { } // Does not implement IServiceType

// EXPECT: NDI0006 because UnrelatedImplementation cannot be assigned to IServiceType.
[RegisterContainer]
[Singleton(typeof(IServiceType), typeof(UnrelatedImplementation))]
public partial class Container_NotAssignable { }

#endregion

#region NDI0007: Cannot instantiate abstract type

// =================================================================

public interface IAbstractService { }

// EXPECT: NDI0007 because IAbstractService is an interface and cannot be instantiated.
[RegisterContainer]
[Singleton(typeof(IAbstractService), typeof(IAbstractService))]
public partial class Container_AbstractImplementation { }

#endregion

#region NDI0008: Ambiguous constructors

// =================================================================

public interface IServiceWithAmbiguousCtors { }

public class ServiceWithAmbiguousCtors : IServiceWithAmbiguousCtors
{
    // Two constructors with the same number of parameters, and no [Inject] attribute.
    public ServiceWithAmbiguousCtors(ISingletonService s) { }
    public ServiceWithAmbiguousCtors(IScopedService s) { }
}

// EXPECT: NDI0008 because ServiceWithAmbiguousCtors has multiple public constructors with 1 parameter.
[RegisterContainer]
[Singleton(typeof(ISingletonService), typeof(SingletonService))]
[Scoped(typeof(IScopedService), typeof(ScopedService))]
[Transient(typeof(IServiceWithAmbiguousCtors), typeof(ServiceWithAmbiguousCtors))]
public partial class Container_AmbiguousConstructors { }

#endregion

#region NDI0009: Multiple [Inject] constructors

// =================================================================

public interface IServiceWithMultipleInject { }

public class ServiceWithMultipleInject : IServiceWithMultipleInject
{
    [Inject]
    public ServiceWithMultipleInject(ISingletonService s) { }

    [Inject]
    public ServiceWithMultipleInject() { }
}

// EXPECT: NDI0009 because ServiceWithMultipleInject has two constructors marked with [Inject].
[RegisterContainer]
[Singleton(typeof(ISingletonService), typeof(SingletonService))]
[Transient(typeof(IServiceWithMultipleInject), typeof(ServiceWithMultipleInject))]
public partial class Container_MultipleInjectConstructors { }

#endregion

#region NDI0010: Duplicate services registration

// =================================================================

// --- SCENARIO 1: Duplicate registration in container ---
public interface IDuplicateService { }

public class DuplicateImplA : IDuplicateService { }

public class DuplicateImplB : IDuplicateService { }

// EXPECT: NDI0010 at the second registration. Service IDuplicateService is already registered.
[RegisterContainer]
[Singleton(typeof(IDuplicateService), typeof(DuplicateImplA))]
[Singleton(typeof(IDuplicateService), typeof(DuplicateImplB))]
public partial class Container_DuplicateRegistration { }

// --- SCENARIO 2: Duplicate registration via module import ---
public interface ISharedServiceForDuplication { }

public class ModuleSharedService : ISharedServiceForDuplication { }

public class ContainerSharedService : ISharedServiceForDuplication { }

[RegisterModule]
[Singleton(typeof(ISharedServiceForDuplication), typeof(ModuleSharedService))]
public interface IDuplicateRegistrationModule { }

// EXPECT: NDI0010 due to conflict between module and container registration.
[RegisterContainer]
[ImportModule(typeof(IDuplicateRegistrationModule))]
[Singleton(typeof(ISharedServiceForDuplication), typeof(ContainerSharedService))]
public partial class Container_ModuleDuplicateRegistration { }

#endregion

#region NDI0011: Invalid lifestyle mismatch (Captive Dependency)

// =================================================================

// --- SCENARIO 1: Singleton depends on Scoped ---
public interface ICaptiveSingleton { }

public class CaptiveSingleton(ICaptiveScoped dependency) : ICaptiveSingleton { }

public interface ICaptiveScoped { }

public class CaptiveScoped : ICaptiveScoped { }

// EXPECT: NDI0011. CaptiveSingleton (Singleton) cannot depend on ICaptiveScoped (Scoped).
[RegisterContainer]
[Singleton(typeof(ICaptiveSingleton), typeof(CaptiveSingleton))]
[Scoped(typeof(ICaptiveScoped), typeof(CaptiveScoped))]
public partial class Container_SingletonDependsOnScoped { }

// --- SCENARIO 2: Captive dependency via module ---
[RegisterModule]
[Singleton(typeof(ICaptiveSingletonViaModule), typeof(CaptiveSingletonViaModule))]
public interface IModuleWithSingleton { }

public interface ICaptiveSingletonViaModule { }

public class CaptiveSingletonViaModule(IScopedDepForModule dep) : ICaptiveSingletonViaModule { }

// The dependency with the shorter lifetime is in the container.
public interface IScopedDepForModule { }

public class ScopedDepForModule : IScopedDepForModule { }

// EXPECT: NDI0011. The error is detected when the container's graph is built, linking the module's Singleton to the container's Scoped service.
[RegisterContainer]
[ImportModule(typeof(IModuleWithSingleton))]
[Scoped(typeof(IScopedDepForModule), typeof(ScopedDepForModule))]
public partial class Container_CaptiveDependencyViaModule { }

#endregion

#region NDI0012-NDI0016: Decorator-specific Errors

// =================================================================

// --- NDI0012: Decorator for an unregistered service ---
public interface IUnregisteredForDecorator { }

public class MyDecorator(IUnregisteredForDecorator decoratee) : IUnregisteredForDecorator { }

// EXPECT: NDI0012. Cannot apply decorator because IUnregisteredForDecorator has not been registered.
[RegisterContainer]
[Decorate(typeof(IUnregisteredForDecorator), typeof(MyDecorator))]
public partial class Container_DecoratorForUnregisteredService { }

// --- NDI0013: Ambiguous decorator constructors ---
public interface IAmbiguousDecoratorService { }

public class AmbiguousDecoratorServiceImpl : IAmbiguousDecoratorService { }

public class AmbiguousDecorator : IAmbiguousDecoratorService
{
    // Two constructors could be chosen as the "greediest", causing an ambiguity.
    public AmbiguousDecorator(IAmbiguousDecoratorService inner, ISingletonService s1) { }
    public AmbiguousDecorator(IAmbiguousDecoratorService inner, IScopedService s2) { }
}

// EXPECT: NDI0013 because AmbiguousDecorator has multiple constructors with 2 parameters.
[RegisterContainer]
[Singleton(typeof(IAmbiguousDecoratorService), typeof(AmbiguousDecoratorServiceImpl))]
[Decorate(typeof(IAmbiguousDecoratorService), typeof(AmbiguousDecorator))]
[Singleton(typeof(ISingletonService), typeof(SingletonService))]
[Scoped(typeof(IScopedService), typeof(ScopedService))]
public partial class Container_AmbiguousDecoratorConstructors { }

// --- NDI0014: Decorator missing the decorated service parameter ---
public interface IMissingParamService { }

public class MissingParamServiceImpl : IMissingParamService { }

public class DecoratorWithMissingParameter : IMissingParamService // Must implement interface
{
    // No parameter of type IMissingParamService.
    public DecoratorWithMissingParameter(ISingletonService s1) { }
}

// EXPECT: NDI0014 because the decorator must have a constructor with exactly one parameter of the decorated service type.
[RegisterContainer]
[Singleton(typeof(IMissingParamService), typeof(MissingParamServiceImpl))]
[Decorate(typeof(IMissingParamService), typeof(DecoratorWithMissingParameter))]
[Singleton(typeof(ISingletonService), typeof(SingletonService))]
public partial class Container_DecoratorMissingParameter { }

// --- NDI0015: Decorator has a captive dependency ---
public interface ILongLived { }

public class LongLived : ILongLived { }

public interface IShortLived { }

public class ShortLived : IShortLived { }

// This decorator is the problem. It decorates a Singleton but needs a Scoped service.
public class CaptiveDepDecorator(ILongLived inner, IShortLived scoped) : ILongLived;

// EXPECT: NDI0015. CaptiveDepDecorator (inherits Singleton) cannot depend on IShortLived (Scoped).
[RegisterContainer]
[Singleton(typeof(ILongLived), typeof(LongLived))]
[Scoped(typeof(IShortLived), typeof(ShortLived))]
[Decorate(typeof(ILongLived), typeof(CaptiveDepDecorator))]
public partial class Container_DecoratorCaptiveDependency { }

// --- NDI0016: Duplicate decorator registration ---
public interface IDuplicateDecoratedService { }

public class DuplicateDecoratedServiceImpl : IDuplicateDecoratedService { }

public class ReusableDecorator(IDuplicateDecoratedService inner) : IDuplicateDecoratedService { }

// EXPECT: NDI0016 on the second Decorate attribute.
[RegisterContainer]
[Singleton(typeof(IDuplicateDecoratedService), typeof(DuplicateDecoratedServiceImpl))]
[Decorate(typeof(IDuplicateDecoratedService), typeof(ReusableDecorator))]
[Decorate(typeof(IDuplicateDecoratedService), typeof(ReusableDecorator))]
public partial class Container_DuplicateDecorator { }

#endregion

#region NDI0017: Imported type is not a module

// =================================================================

// --- SCENARIO 1: Importing a regular class ---
public class NotAModule { } // Missing [RegisterModule] attribute

// EXPECT: NDI0017 because NotAModule is not marked with [RegisterModule].
[RegisterContainer]
[ImportModule(typeof(NotAModule))]
public partial class Container_ImportingNonModule { }

// --- SCENARIO 2: A module with a malformed attribute ---
// While the generator only looks for `[RegisterModule]` on an interface, a user might misuse it.
// This is more of a general C# compiler error, but good to keep in mind.
[RegisterModule]
public class NotAnInterfaceModule { }

// EXPECT: The C# compiler might flag the attribute usage first. If not, the generator should handle it gracefully.
// The current generator implementation correctly filters for interfaces with `[RegisterModule]`, so it would just ignore this.
// This test is to ensure the generator doesn't crash on this edge case.
[RegisterContainer]
[ImportModule(typeof(NotAnInterfaceModule))]
public partial class Container_ImportingClassModule { }

#endregion

#region NDI0005 and NDI0004 with Nullable Reference Types

// =================================================================

// --- SCENARIO 1: Missing Optional Dependency ---
public interface IServiceWithMissingOptionalDep { }

public class ServiceWithMissingOptionalDep : IServiceWithMissingOptionalDep
{
    // Even though this is marked as nullable, the DI container should still
    // report an error if it's not registered, following a "fail-fast" policy.
    public ServiceWithMissingOptionalDep(IUnregisteredOptionalDep? optionalDep) { }
}

public interface IUnregisteredOptionalDep { } // Never registered

// EXPECT: NDI0005. The generator must not treat the nullable annotation '?'
// as a reason to suppress the "Service not registered" error by injecting null.
[RegisterContainer]
[Singleton(typeof(IServiceWithMissingOptionalDep), typeof(ServiceWithMissingOptionalDep))]
public partial class Container_MissingNullableDependency { }

// --- SCENARIO 2: Cyclic Dependency with Nullable Parameter ---
public interface ICyclicWithNullable { }

public class CyclicWithNullable(ICyclicWithNullable? self) : ICyclicWithNullable
{
    // The '?' should not prevent the cycle detector from firing.
    // A dependency on oneself is always a cycle, regardless of nullability.
}

// EXPECT: NDI0004. The cycle detector must operate on types and ignore nullability annotations.
[RegisterContainer]
[Singleton(typeof(ICyclicWithNullable), typeof(CyclicWithNullable))]
public partial class Container_CyclicDependencyWithNullable { }

#endregion

#region IContainer Injection Scenarios

// --- Singleton with IContainer (should succeed) ---
public interface ISingletonWithContainer { }

public class SingletonWithContainer(IContainer container) : ISingletonWithContainer { }

// EXPECT: No diagnostic for this usage.
[RegisterContainer]
[Singleton(typeof(ISingletonWithContainer), typeof(SingletonWithContainer))]
public partial class Container_SingletonWithContainer { }

// --- Scoped with IContainer (should trigger NDI0011) ---
public interface IScopedWithContainer { }

public class ScopedWithContainer(IContainer container) : IScopedWithContainer { }

// EXPECT: NDI0011 at ScopedWithContainer registration: Scoped service cannot depend on IContainer (singleton).
[RegisterContainer]
[Scoped(typeof(IScopedWithContainer), typeof(ScopedWithContainer))]
public partial class Container_ScopedWithContainer { }

// --- Transient with IContainer (should trigger NDI0011) ---
public interface ITransientWithContainer { }

public class TransientWithContainer(IContainer container) : ITransientWithContainer { }

// EXPECT: NDI0011 at TransientWithContainer registration: Transient service cannot depend on IContainer (singleton).
[RegisterContainer]
[Transient(typeof(ITransientWithContainer), typeof(TransientWithContainer))]
public partial class Container_TransientWithContainer { }

#endregion

#region NDI0019 & NDI0020: ScopedTo (Tagged Scope) Lifetime Errors

// =================================================================

// --- SCENARIO 1: Conflicting Lifetimes (ScopedTo + Singleton) ---
public interface IConflictingLifetimeA { }

public class ConflictingLifetimeA : IConflictingLifetimeA { }

// EXPECT: NDI0019 (or similar) at the registration for IConflictingLifetimeA.
// A service cannot be both a Singleton and scoped to a specific tag.
[RegisterContainer]
[Singleton(typeof(IConflictingLifetimeA), typeof(ConflictingLifetimeA))]
[ScopedTo("MyTag", typeof(IConflictingLifetimeA), typeof(ConflictingLifetimeA))]
public partial class Container_ScopedToWithSingleton { }

// --- SCENARIO 2: Conflicting Lifetimes (ScopedTo + Scoped) ---
public interface IConflictingLifetimeB { }

public class ConflictingLifetimeB : IConflictingLifetimeB { }

// EXPECT: NDI0019 (or similar) at the registration for IConflictingLifetimeB.
// A service has an ambiguous lifetime, being both a normal Scoped service and a tagged Scoped service.
[RegisterContainer]
[Scoped(typeof(IConflictingLifetimeB), typeof(ConflictingLifetimeB))]
[ScopedTo("MyTag", typeof(IConflictingLifetimeB), typeof(ConflictingLifetimeB))]
public partial class Container_ScopedToWithScoped { }

// --- SCENARIO 3: Sideway Captive Dependency (Mismatched Tags) ---
public interface IServiceForTagA { }

public class ServiceForTagA(IServiceForTagB dependency) : IServiceForTagA { }

public interface IServiceForTagB { }

public class ServiceForTagB : IServiceForTagB { }

// EXPECT: NDI0020 (or a new diagnostic) at the registration for IServiceForTagA.
// A service scoped to "TagA" cannot safely depend on a service scoped to "TagB".
// The container cannot guarantee that the lifetime of "TagB" will outlive "TagA",
// which could lead to using a disposed dependency.
[RegisterContainer]
[ScopedTo("TagA", typeof(IServiceForTagA), typeof(ServiceForTagA))]
[ScopedTo("TagB", typeof(IServiceForTagB), typeof(ServiceForTagB))]
public partial class Container_MismatchedTagDependency { }

#endregion

#endif