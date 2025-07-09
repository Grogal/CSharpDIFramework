// using TUnit.Assertions.AssertConditions.Throws;
//
// namespace CSharpDIFramework.Tests;
//
// public partial class ResolutionTests
// {
//     [Test]
//     public async ValueTask Resolve_WhenServiceIsRegistered_ReturnsInstance()
//     {
//         var container = new SimpleContainer();
//
//         var service = container.Resolve<ISingletonService>();
//
//         await Assert.That(service).IsNotNull();
//         await Assert.That(service).IsAssignableFrom<SingletonService>();
//     }
//
//     [Test]
//     public async ValueTask Resolve_WhenServiceIsNotRegistered_ThrowsInvalidOperationException()
//     {
//         var container = new EmptyContainer();
//
//         await Assert.That(() => container.Resolve<ISingletonService>()).Throws<InvalidOperationException>();
//     }
//
//     [Test]
//     public async ValueTask Resolve_WithDependency_InjectsCorrectService()
//     {
//         var container = new DependencyContainer();
//
//         using IContainerScope scope = container.CreateScope();
//
//         var singletonDep = container.Resolve<ISingletonService>();
//         var service = scope.Resolve<IServiceWithDependency>();
//
//         await Assert.That(service).IsNotNull();
//         await Assert.That(service.Singleton).IsSameReferenceAs(singletonDep);
//     }
//
//     [RegisterContainer]
//     [Singleton(typeof(ISingletonService), typeof(SingletonService))]
//     public partial class SimpleContainer { }
//
//     [RegisterContainer]
//     public partial class EmptyContainer { }
//
//     [RegisterContainer]
//     [Singleton(typeof(ISingletonService), typeof(SingletonService))]
//     [Transient(typeof(IServiceWithDependency), typeof(ServiceWithDependency))]
//     public partial class DependencyContainer { }
// }

