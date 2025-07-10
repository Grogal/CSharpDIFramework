using TUnit.Assertions.AssertConditions.Throws;

namespace CSharpDIFramework.Tests;

public partial class ResolutionTests
{
    [Test]
    public async ValueTask Resolve_WhenServiceIsRegistered_ReturnsInstance()
    {
        var container = new SimpleContainer();

        var service = container.Resolve<ISingletonService>();

        await Assert.That(service).IsNotNull();
        await Assert.That(service).IsAssignableFrom<SingletonService>();
    }

    [Test]
    public async ValueTask Resolve_WhenServiceIsNotRegistered_ThrowsInvalidOperationException()
    {
        var container = new EmptyContainer();

        await Assert.That(() => container.Resolve<ISingletonService>()).Throws<InvalidOperationException>();
    }

    [Test]
    public async ValueTask Resolve_WithDependency_InjectsCorrectService()
    {
        var container = new DependencyContainer();

        using IContainerScope scope = container.CreateScope();

        var singletonDep = container.Resolve<ISingletonService>();
        var service = scope.Resolve<IServiceWithDependency>();

        await Assert.That(service).IsNotNull();
        await Assert.That(service.Singleton).IsSameReferenceAs(singletonDep);
    }

    [Test]
    public async ValueTask Resolve_SimpleFactory()
    {
        var container = new SimpleFactoryContainer();
        var factory = container.Resolve<SimpleFactory>();
        await Assert.That(factory).IsNotNull();

        var service = factory.Create<ISingletonService>();
        await Assert.That(service).IsNotNull();
    }

    [Test]
    public async ValueTask Resolve_SimpleScopedFactoryContainer()
    {
        var container = new SimpleScopedFactoryContainer();

        using IContainerScope scope = container.CreateScope();

        var factory = scope.Resolve<SimpleFactory>();
        await Assert.That(factory).IsNotNull();

        var service = factory.Create<IScopedService>();
        await Assert.That(service).IsNotNull();
    }

    [RegisterContainer]
    [Singleton(typeof(ISingletonService), typeof(SingletonService))]
    public partial class SimpleContainer { }

    [RegisterContainer]
    public partial class EmptyContainer { }

    [RegisterContainer]
    [Singleton(typeof(ISingletonService), typeof(SingletonService))]
    [Transient(typeof(IServiceWithDependency), typeof(ServiceWithDependency))]
    public partial class DependencyContainer { }

    [RegisterContainer]
    [Singleton(typeof(SimpleFactory))]
    [Singleton(typeof(ISingletonService), typeof(SingletonService))]
    public partial class SimpleFactoryContainer { }

    [RegisterContainer]
    [Scoped(typeof(SimpleFactory))]
    [Scoped(typeof(IScopedService), typeof(ScopedService))]
    public partial class SimpleScopedFactoryContainer { }
}