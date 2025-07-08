namespace CSharpDIFramework.Tests;

public partial class ConstructorSelectionTests
{
    [Test]
    public async ValueTask WhenInjectAttributeIsPresent_UsesAttributedConstructor()
    {
        // Arrange
        var container = new InjectCtorContainer();

        // Act
        using IContainerScope scope = container.CreateScope();
        var service = scope.Resolve<IServiceWithCtors>();

        // Assert
        await Assert.That(service.UsedCtor).IsEqualTo("Inject");
    }

    [Test]
    public async ValueTask WhenNoInjectAttribute_UsesGreediestPublicConstructor()
    {
        // Arrange
        var container = new GreedyCtorContainer();

        // Act
        using IContainerScope scope = container.CreateScope();
        var service = scope.Resolve<IServiceWithCtors>();

        // Assert
        await Assert.That(service.UsedCtor).IsEqualTo("Greediest");
    }

    [RegisterContainer]
    [Singleton(typeof(ISingletonService), typeof(SingletonService))]
    [Scoped(typeof(IScopedService), typeof(ScopedService))]
    [Transient(typeof(IServiceWithCtors), typeof(ServiceWithInjectCtor))]
    public partial class InjectCtorContainer { }

    [RegisterContainer]
    [Singleton(typeof(ISingletonService), typeof(SingletonService))]
    [Transient(typeof(IServiceWithCtors), typeof(ServiceWithGreedyCtor))]
    public partial class GreedyCtorContainer { }
}