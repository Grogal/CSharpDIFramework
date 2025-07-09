// namespace CSharpDIFramework.Tests;
//
// public partial class DecoratorTests
// {
//     [Test]
//     public async Task DecoratedService_IsWrappedByDecorator()
//     {
//         var container = new SimpleDecoratorContainer();
//
//         var service = container.Resolve<IGreetingService>();
//         string result = service.Greet();
//
//         await Assert.That(result).IsEqualTo("Hello!");
//     }
//
// [Test]
// public async Task ChainedDecorators_AreAppliedInRegistrationOrder_WithOuterHavingDependencies()
// {
//     var container = new ChainedDecoratorContainer();
//
//     var service = container.Resolve<IGreetingService>();
//     var logger = container.Resolve<ILogger>() as StringBuilderLogger;
//     string result = service.Greet();
//
//     await Assert.That(logger!.Output.Trim()).IsEqualTo("Greeting...");
//     await Assert.That(result).IsEqualTo("Hello!");
// }
//
//     [Test]
//     public async Task Decorator_CorrectlyInheritsServiceLifetime()
//     {
//         var container = new DecoratorLifetimeContainer();
//
//         using (IContainerScope scope = container.CreateScope())
//         {
//             var instance1 = scope.Resolve<IGreetingService>();
//             var instance2 = scope.Resolve<IGreetingService>();
//             await Assert.That(instance1).IsNotSameReferenceAs(instance2);
//         }
//     }
//
//     [Test]
//     public async Task DisposableDecorator_IsDisposed_WhenScopeEnds()
//     {
//         var container = new DecoratorDisposalContainer();
//         IDisposableService? service;
//
//         using (IContainerScope scope = container.CreateScope())
//         {
//             service = scope.Resolve<IGreetingService>() as IDisposableService;
//         }
//
//         await Assert.That(service!.IsDisposed).IsTrue();
//     }
//
//     [Test]
//     public async Task DecoratorConstructorSelection_RespectsInjectAttribute()
//     {
//         var container = new DecoratorInjectCtorContainer();
//
//         var service = container.Resolve<IGreetingService>() as DecoratorWithInjectCtor;
//
//         await Assert.That(service!.UsedCtor).IsEqualTo("Inject");
//     }
//
//     [Test]
//     public async Task WhenMultipleDecoratorsAreRegistered_TheyAreAppliedInOrder()
//     {
//         // Arrange
//         var container = new DecoratorOrderContainer();
//
//         // Act
//         var service = container.Resolve<IOrderedService>();
//         string result = service.ApplyOrder();
//
//         // Assert
//         // Expected chain: new DecoratorB(new DecoratorA(new BaseService()))
//         await Assert.That(result).IsEqualTo("Base-A-B");
//     }
//
//     [Test]
//     public async Task WhenDecoratingModuleService_AllDecoratorsAreAppliedInOrder()
//     {
//         // Arrange
//         var container = new CombinedDecoratorContainer();
//
//         // Act
//         var service = container.Resolve<IOrderedService>();
//         string result = service.ApplyOrder();
//
//         // Assert
//         // The parser will process Module attributes first, then Container attributes.
//         // Expected chain: new DecoratorB(new DecoratorA(new BaseService()))
//         await Assert.That(result).IsEqualTo("Base-A-B");
//     }
//
//     [RegisterContainer]
//     [Singleton(typeof(IGreetingService), typeof(GreetingService))]
//     [Decorate(typeof(IGreetingService), typeof(ExclamationDecorator))]
//     public partial class SimpleDecoratorContainer { }
//
//     [RegisterContainer]
//     [Singleton(typeof(IGreetingService), typeof(GreetingService))]
//     [Decorate(typeof(IGreetingService), typeof(ExclamationDecorator))] // Inner
//     [Decorate(typeof(IGreetingService), typeof(LoggingDecorator))] // Outer
//     [Singleton(typeof(ILogger), typeof(StringBuilderLogger))]
//     public partial class ChainedDecoratorContainer { }
//
//     [RegisterContainer]
//     [Transient(typeof(IGreetingService), typeof(GreetingService))] // Service is Transient
//     [Decorate(typeof(IGreetingService), typeof(ExclamationDecorator))]
//     public partial class DecoratorLifetimeContainer { }
//
//     [RegisterContainer]
//     [Scoped(typeof(IGreetingService), typeof(GreetingService))]
//     [Decorate(typeof(IGreetingService), typeof(DisposableDecorator))]
//     public partial class DecoratorDisposalContainer { }
//
//     [RegisterContainer]
//     [Singleton(typeof(IGreetingService), typeof(GreetingService))]
//     [Decorate(typeof(IGreetingService), typeof(DecoratorWithInjectCtor))]
//     [Singleton(typeof(ILogger), typeof(StringBuilderLogger))] // Needed for the greedier ctor
//     public partial class DecoratorInjectCtorContainer { }
//
//     [RegisterContainer]
//     [Singleton(typeof(IOrderedService), typeof(BaseOrderedService))]
//     [Decorate(typeof(IOrderedService), typeof(DecoratorA))] // Applied first
//     [Decorate(typeof(IOrderedService), typeof(DecoratorB))] // Applied second (outermost)
//     public partial class DecoratorOrderContainer { }
//
//     [RegisterContainer]
//     [ImportModule(typeof(IPartialDecoratedModule))]
//     [Decorate(typeof(IOrderedService), typeof(DecoratorB))] // Container adds the outer decorator
//     public partial class CombinedDecoratorContainer { }
// }

