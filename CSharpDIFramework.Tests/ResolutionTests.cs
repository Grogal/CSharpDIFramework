using TUnit.Assertions.AssertConditions.Throws;

namespace CSharpDIFramework.Tests;

public class ResolutionTests
{
    [Test]
    public async ValueTask Resolve_WhenServiceIsRegistered_ReturnsInstance()
    {
        var container = new AllLifetimesContainer();

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
}