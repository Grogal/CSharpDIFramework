// namespace CSharpDIFramework.Tests;
//
// public partial class NullableDependencyTests
// {
//     [Test]
//     public async Task WhenNullableDependencyIsRegistered_ItIsCorrectlyInjected_And_IsNotNull()
//     {
//         // Arrange
//         var container = new ContainerWithDependencyAvailable();
//         var expectedDependency = container.Resolve<IOptionalDependency>();
//
//         // Act
//         using IContainerScope scope = container.CreateScope();
//         var service = scope.Resolve<IServiceWithOptionalDependency>();
//
//         // Assert
//         // This confirms the container injected the registered service, not null.
//         await Assert.That(service.InjectedDependency).IsNotNull();
//         await Assert.That(service.InjectedDependency).IsSameReferenceAs(expectedDependency);
//     }
//
//     [RegisterContainer]
//     [Singleton(typeof(IOptionalDependency), typeof(OptionalDependency))] // The dependency IS registered here.
//     [Transient(typeof(IServiceWithOptionalDependency), typeof(ServiceWithOptionalDependency))]
//     public partial class ContainerWithDependencyAvailable { }
//
//     // [RegisterContainer]
//     // // NOTE: NullableConcreteService is NOT registered in this container.
//     // [Transient(typeof(IServiceWithConcreteNullableDep), typeof(ServiceWithConcreteNullableDep))]
//     // public partial class ContainerWithDependencyUnavailable { }
//     //
//     // [Test]
//     // public async Task WhenNullableDependencyIsNotRegistered_ItIsResolvedAsNull()
//     // {
//     //     var container = new ContainerWithDependencyUnavailable();
//     //
//     //     using var scope = container.CreateScope();
//     //     var service = scope.Resolve<IServiceWithConcreteNullableDep>();
//     //
//     //     await Assert.That(service.InjectedService).IsNull();
//     // }
// }

