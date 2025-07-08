using TUnit.Assertions.AssertConditions.Throws;

namespace CSharpDIFramework.Tests;

public partial class LifetimeTests
{
    [Test]
    public async ValueTask Singleton_IsResolvedAsSameInstance_FromRootAndScope()
    {
        // Arrange
        var container = new AllLifetimesContainer();

        // Act
        var rootInstance = container.Resolve<ISingletonService>();
        ISingletonService scopeInstance;
        using (IContainerScope scope = container.CreateScope())
        {
            scopeInstance = scope.Resolve<ISingletonService>();
        }

        // Assert
        await Assert.That(rootInstance).IsSameReferenceAs(scopeInstance);
    }

    [Test]
    public async ValueTask Scoped_IsResolvedAsSameInstance_WithinASingleScope()
    {
        // Arrange
        var container = new AllLifetimesContainer();

        // Act & Assert
        using (IContainerScope scope = container.CreateScope())
        {
            var instance1 = scope.Resolve<IScopedService>();
            var instance2 = scope.Resolve<IScopedService>();
            await Assert.That(instance1).IsSameReferenceAs(instance2);
        }
    }

    [Test]
    public async ValueTask Scoped_IsResolvedAsDifferentInstances_AcrossDifferentScopes()
    {
        // Arrange
        var container = new AllLifetimesContainer();
        IScopedService instance1, instance2;

        // Act
        using (IContainerScope scope1 = container.CreateScope())
        {
            instance1 = scope1.Resolve<IScopedService>();
        }

        using (IContainerScope scope2 = container.CreateScope())
        {
            instance2 = scope2.Resolve<IScopedService>();
        }

        // Assert
        await Assert.That(instance1).IsNotSameReferenceAs(instance2);
    }

    [Test]
    public async ValueTask Transient_IsResolvedAsNewInstance_OnEachResolution()
    {
        // Arrange
        var container = new AllLifetimesContainer();

        // Act & Assert
        using (IContainerScope scope = container.CreateScope())
        {
            var instance1 = scope.Resolve<ITransientService>();
            var instance2 = scope.Resolve<ITransientService>();
            await Assert.That(instance1).IsNotSameReferenceAs(instance2);
        }
    }

    [Test]
    public async ValueTask ScopedService_WhenResolvedFromRoot_ThrowsInvalidOperationException()
    {
        // Arrange
        var container = new AllLifetimesContainer();

        // Act & Assert
        await Assert.That(() => container.Resolve<IScopedService>())
                    .Throws<InvalidOperationException>();
    }

    [Test]
    public async ValueTask TransientService_WhenResolvedFromRoot_ThrowsInvalidOperationException()
    {
        // Arrange
        var container = new AllLifetimesContainer();

        // Act & Assert
        await Assert.That(() => container.Resolve<ITransientService>())
                    .Throws<InvalidOperationException>();
    }

    [RegisterContainer]
    [Singleton(typeof(ISingletonService), typeof(SingletonService))]
    [Scoped(typeof(IScopedService), typeof(ScopedService))]
    [Transient(typeof(ITransientService), typeof(TransientService))]
    public partial class AllLifetimesContainer { }
}