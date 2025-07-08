namespace CSharpDIFramework.Tests;

public class DecoratorTests
{
    [Test]
    public async Task DecoratedService_IsWrappedByDecorator()
    {
        var container = new SimpleDecoratorContainer();

        var service = container.Resolve<IGreetingService>();
        string result = service.Greet();

        await Assert.That(result).IsEqualTo("Hello!");
    }

    [Test]
    public async Task ChainedDecorators_AreAppliedInRegistrationOrder_WithOuterHavingDependencies()
    {
        var container = new ChainedDecoratorContainer();

        var service = container.Resolve<IGreetingService>();
        var logger = container.Resolve<ILogger>() as StringBuilderLogger;
        string result = service.Greet();

        await Assert.That(logger!.Output.Trim()).IsEqualTo("Greeting...");
        await Assert.That(result).IsEqualTo("Hello!");
    }

    [Test]
    public async Task Decorator_CorrectlyInheritsServiceLifetime()
    {
        var container = new DecoratorLifetimeContainer();

        using (IContainerScope scope = container.CreateScope())
        {
            var instance1 = scope.Resolve<IGreetingService>();
            var instance2 = scope.Resolve<IGreetingService>();
            await Assert.That(instance1).IsNotSameReferenceAs(instance2);
        }
    }

    [Test]
    public async Task DisposableDecorator_IsDisposed_WhenScopeEnds()
    {
        var container = new DecoratorDisposalContainer();
        IGreetingService service;

        using (IContainerScope scope = container.CreateScope())
        {
            service = scope.Resolve<IGreetingService>();
        }

        var decorator = service as DisposableDecorator;
        await Assert.That(decorator!.IsDisposed).IsTrue();
    }

    [Test]
    public async Task DecoratorConstructorSelection_RespectsInjectAttribute()
    {
        var container = new DecoratorInjectCtorContainer();

        var service = container.Resolve<IGreetingService>() as DecoratorWithInjectCtor;

        await Assert.That(service!.UsedCtor).IsEqualTo("Inject");
    }

    [Test]
    public async Task WhenMultipleDecoratorsAreRegistered_TheyAreAppliedInOrder()
    {
        // Arrange
        var container = new DecoratorOrderContainer();

        // Act
        var service = container.Resolve<IOrderedService>();
        string result = service.ApplyOrder();

        // Assert
        // Expected chain: new DecoratorB(new DecoratorA(new BaseService()))
        await Assert.That(result).IsEqualTo("Base-A-B");
    }

    [Test]
    public async Task WhenDecoratingModuleService_AllDecoratorsAreAppliedInOrder()
    {
        // Arrange
        var container = new CombinedDecoratorContainer();

        // Act
        var service = container.Resolve<IOrderedService>();
        string result = service.ApplyOrder();

        // Assert
        // The parser will process Module attributes first, then Container attributes.
        // Expected chain: new DecoratorB(new DecoratorA(new BaseService()))
        await Assert.That(result).IsEqualTo("Base-A-B");
    }
}