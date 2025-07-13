using TUnit.Assertions.AssertConditions.Throws;

namespace CSharpDIFramework.Tests;

public partial class TaggedScopeTests
{
    private const string ScopeName = "SessionScope";
    private const string SecondScopeName = "SecondSessionScope";

    [Test]
    public async Task ScopedToService_IsSingleton_WithinTaggedScope()
    {
        // Arrange
        var container = new TaggedScopeContainer();

        // Act
        using IContainerScope sessionScope = container.CreateScope(ScopeName);
        var instance1 = sessionScope.Resolve<IServiceWithDependency>();
        var instance2 = sessionScope.Resolve<IServiceWithDependency>();

        // Assert
        await Assert.That(instance1).IsNotNull();
        await Assert.That(instance1).IsSameReferenceAs(instance2);
    }

    [Test]
    public async Task ScopedToService_IsDifferent_AcrossDifferentTaggedScopes()
    {
        // Arrange
        var container = new TaggedScopeContainer();
        IServiceWithDependency instance1, instance2;

        // Act
        using (IContainerScope scopeA = container.CreateScope(ScopeName))
        {
            instance1 = scopeA.Resolve<IServiceWithDependency>();
        }

        using (IContainerScope scopeB = container.CreateScope(ScopeName))
        {
            instance2 = scopeB.Resolve<IServiceWithDependency>();
        }

        // Assert
        await Assert.That(instance1).IsNotSameReferenceAs(instance2);
    }

    [Test]
    public async Task ChildScope_ResolvesTaggedService_FromParentScope()
    {
        // Arrange
        var container = new TaggedScopeContainer();

        using IContainerScope sessionScope = container.CreateScope(ScopeName);
        var parentInstance = sessionScope.Resolve<IServiceWithDependency>();

        // Act
        using IContainerScope childScope = sessionScope.CreateScope(); // Child is untagged
        var childInstance = childScope.Resolve<IServiceWithDependency>();

        // Assert
        await Assert.That(childInstance).IsSameReferenceAs(parentInstance);
    }

    [Test]
    public async Task ScopedToService_WithDependencies_ResolvesThemCorrectly()
    {
        // Tests that ServiceWithDependency (ScopedTo) can resolve ISingletonService from the root.
        // Arrange
        var container = new TaggedScopeContainer();
        var rootSingleton = container.Resolve<ISingletonService>();

        // Act
        using IContainerScope sessionScope = container.CreateScope(ScopeName);
        var sessionService = sessionScope.Resolve<IServiceWithDependency>();

        // Assert: The service was created, and its dependency matches the root singleton.
        await Assert.That(sessionService).IsNotNull();
        await Assert.That(sessionService.Singleton).IsSameReferenceAs(rootSingleton);
    }

    [Test]
    public async Task Resolving_WhenMultipleTagsExist_DoesNotInterfere()
    {
        // Arrange
        var container = new TaggedScopeContainer();

        using IContainerScope sessionScope = container.CreateScope(ScopeName);
        var sessionService = sessionScope.Resolve<IServiceWithDependency>();

        // Act
        using IContainerScope turnScope = sessionScope.CreateScope(SecondScopeName);
        var turnService = turnScope.Resolve<ITransientService>();

        // Assert: Both services should be resolved correctly.
        await Assert.That(sessionService).IsNotNull();
        await Assert.That(turnService).IsNotNull();

        // Assert that the turn scope can still find the session service from its ancestor.
        var sessionServiceFromTurnScope = turnScope.Resolve<IServiceWithDependency>();
        await Assert.That(sessionServiceFromTurnScope).IsSameReferenceAs(sessionService);
    }

    [Test]
    public async Task Resolving_WhenChildScopeIsAlsoTagged_DelegatesCorrectly()
    {
        // Arrange
        var container = new TaggedScopeContainer();

        using IContainerScope sessionScope = container.CreateScope(ScopeName);
        // Act
        using IContainerScope turnScope = sessionScope.CreateScope(SecondScopeName);

        // Assert: Ask the inner "GameTurn" scope for a service owned by the "PlayerSession" scope.
        // It must correctly delegate the request up to its parent.
        var sessionService = turnScope.Resolve<IServiceWithDependency>();
        await Assert.That(sessionService).IsNotNull();
    }

    [Test]
    public async Task ResolvingScopedToService_DirectlyFromRootContainer_ThrowsInvalidOperationException()
    {
        // Arrange
        var container = new TaggedScopeContainer();

        // Act & Assert
        await Assert.That(() => container.Resolve<IServiceWithDependency>())
                    .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task ScopedToService_IsNotDisposed_WhenChildScopeIsDisposed()
    {
        // Arrange
        var container = new DisposableTaggedScopeContainer();
        IDisposableService? serviceInstance;

        using (IContainerScope sessionScope = container.CreateScope(ScopeName))
        {
            // Now, we resolve the service that LocalContextService depends on.
            var dependencyInstance = sessionScope.Resolve<IServiceWithDependency>();
            serviceInstance = dependencyInstance as IDisposableService; // This cast is now valid.

            await Assert.That(serviceInstance).IsNotNull();

            using (IContainerScope childScope = sessionScope.CreateScope())
            {
                // This will now work correctly. The DI container will resolve IServiceWithDependency
                // from the parent (sessionScope) and inject it into the new LocalContextService.
                _ = childScope.Resolve<ILocalContextService>();
            }

            // Assert: After the child scope is disposed, the session service it used should NOT be disposed.
            await Assert.That(serviceInstance!.IsDisposed).IsFalse();
        }

        // Assert: Only after its own home scope is disposed is the service disposed.
        await Assert.That(serviceInstance.IsDisposed).IsTrue();
    }

    [Test]
    public async Task ResolvingScopedToService_WhenNoTaggedScopeExists_ThrowsInvalidOperationException()
    {
        // Arrange
        var container = new TaggedScopeContainer();
        using IContainerScope scope = container.CreateScope(); // Create a scope WITHOUT the required tag

        // Act & Assert
        await Assert.That(() => scope.Resolve<IServiceWithDependency>())
                    .Throws<InvalidOperationException>();
    }

    [RegisterContainer]
    [Singleton(typeof(ISingletonService), typeof(SingletonService))]
    [ScopedTo(ScopeName, typeof(IServiceWithDependency), typeof(ServiceWithDependency))]
    [ScopedTo(SecondScopeName, typeof(ITransientService), typeof(TransientService))]
    [Scoped(typeof(ILocalContextService), typeof(LocalContextService))]
    public partial class TaggedScopeContainer { }

    [RegisterContainer]
    [Singleton(typeof(ISingletonService), typeof(SingletonService))]
    [ScopedTo(ScopeName, typeof(IServiceWithDependency), typeof(DisposableDependencyService))]
    [ScopedTo(ScopeName, typeof(IDisposableService), typeof(DisposableDependencyService))] // Register both interfaces to the same instance
    [Scoped(typeof(ILocalContextService), typeof(LocalContextService))]
    public partial class DisposableTaggedScopeContainer { }
}