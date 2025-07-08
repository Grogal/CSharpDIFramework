namespace CSharpDIFramework.Tests;

public class DisposalTests
{
    [Test]
    public async ValueTask ScopedDisposable_IsDisposed_WhenScopeEnds()
    {
        var container = new AllLifetimesDisposalContainer();
        IDisposableService? service;

        using (IContainerScope scope = container.CreateScope())
        {
            service = scope.Resolve<IScopedService>() as IDisposableService;
        }

        await Assert.That(service!.IsDisposed).IsTrue();
    }

    [Test]
    public async ValueTask TransientDisposable_IsDisposed_WhenScopeEnds()
    {
        var container = new AllLifetimesDisposalContainer();
        IDisposableService? service;

        using (IContainerScope scope = container.CreateScope())
        {
            service = scope.Resolve<ITransientService>() as IDisposableService;
        }

        await Assert.That(service!.IsDisposed).IsTrue();
    }

    [Test]
    public async ValueTask SingletonDisposable_IsNotDisposed_WhenScopeEnds()
    {
        var container = new AllLifetimesDisposalContainer();
        IDisposableService? service;

        using (IContainerScope scope = container.CreateScope())
        {
            service = scope.Resolve<ISingletonService>() as IDisposableService;
        }

        await Assert.That(service!.IsDisposed).IsFalse();
    }

    [Test]
    public async ValueTask SingletonDisposable_IsDisposed_WhenRootContainerIsDisposed()
    {
        var container = new AllLifetimesDisposalContainer();

        var service = container.Resolve<ISingletonService>() as IDisposableService;
        container.Dispose(); // Assuming the root container implements IDisposable

        await Assert.That(service!.IsDisposed).IsTrue();
    }

    [Test]
    public async ValueTask WhenDisposableTransientIsResolvedMultipleTimes_AllInstancesAreDisposed()
    {
        // Arrange
        var container = new AllLifetimesDisposalContainer();
        var instances = new List<IDisposableService?>();

        // Act
        using (IContainerScope scope = container.CreateScope())
        {
            instances.Add(scope.Resolve<ITransientService>() as IDisposableService);
            instances.Add(scope.Resolve<ITransientService>() as IDisposableService);
        }

        // Assert
        await Assert.That(instances[0]!.IsDisposed).IsTrue();
        await Assert.That(instances[1]!.IsDisposed).IsTrue();
        await Assert.That(instances[0]).IsNotSameReferenceAs(instances[1]);
    }

    [Test]
    public async ValueTask WhenTransientDependsOnScoped_AndBothAreDisposable_BothAreDisposed()
    {
        var container = new NestedValidDisposalContainer();
        DisposableRepository? repo;
        DisposableUnitOfWork? uow;

        using (IContainerScope scope = container.CreateScope())
        {
            repo = scope.Resolve<IRepository>() as DisposableRepository;
            uow = repo!.UnitOfWork as DisposableUnitOfWork;
        }

        await Assert.That(repo.IsDisposed).IsTrue();
        await Assert.That(uow!.IsDisposed).IsTrue();
    }
}