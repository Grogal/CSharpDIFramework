namespace CSharpDIFramework.Tests;

public partial class GenericsTests
{
    #region Basic Resolution Tests

    [RegisterContainer]
    [Singleton(typeof(IRepository<User>), typeof(EfRepository<User>))]
    [Singleton(typeof(IRepository<Product>), typeof(EfRepository<Product>))]
    public partial class MultipleGenericsContainer { }

    [Test]
    public async Task CanResolve_DifferentClosedGenericServices_FromSameContainer()
    {
        // Arrange
        var container = new MultipleGenericsContainer();

        // Act
        var userRepository = container.Resolve<IRepository<User>>();
        var productRepository = container.Resolve<IRepository<Product>>();

        // Assert
        await Assert.That(userRepository).IsNotNull();
        await Assert.That(userRepository).IsAssignableFrom<EfRepository<User>>();

        await Assert.That(productRepository).IsNotNull();
        await Assert.That(productRepository).IsAssignableFrom<EfRepository<Product>>();
    }

    #endregion

    #region Dependency Injection Tests

    [RegisterContainer]
    [Singleton(typeof(IRepository<User>), typeof(EfRepository<User>))]
    [Transient(typeof(IUserService), typeof(UserService))]
    public partial class GenericDependencyContainer { }

    [Test]
    public async Task ServiceWithGenericDependency_IsResolvedCorrectly()
    {
        // Arrange
        var container = new GenericDependencyContainer();

        // Act
        using IContainerScope scope = container.CreateScope();
        var userService = scope.Resolve<IUserService>();
        var userRepository = container.Resolve<IRepository<User>>();

        // Assert
        await Assert.That(userService).IsNotNull();
        await Assert.That(userService.UserRepository).IsSameReferenceAs(userRepository);
    }

    #endregion

    #region Lifetime Tests

    [RegisterContainer]
    [Singleton(typeof(IRepository<User>), typeof(EfRepository<User>))]
    [Scoped(typeof(IRepository<Product>), typeof(EfRepository<Product>))]
    public partial class GenericLifetimeContainer { }

    [Test]
    public async Task Lifetimes_AreRespected_ForClosedGenericServices()
    {
        // Arrange
        var container = new GenericLifetimeContainer();

        // Act
        var singleton1 = container.Resolve<IRepository<User>>();
        var singleton2 = container.Resolve<IRepository<User>>();

        IRepository<Product> scoped1, scoped2, scoped3;
        using (IContainerScope scopeA = container.CreateScope())
        {
            scoped1 = scopeA.Resolve<IRepository<Product>>();
            scoped2 = scopeA.Resolve<IRepository<Product>>();
        }

        using (IContainerScope scopeB = container.CreateScope())
        {
            scoped3 = scopeB.Resolve<IRepository<Product>>();
        }

        // Assert
        await Assert.That(singleton1).IsSameReferenceAs(singleton2); // Singletons are same instance
        await Assert.That(scoped1).IsSameReferenceAs(scoped2); // Scoped are same within a scope
        await Assert.That(scoped1).IsNotSameReferenceAs(scoped3); // Scoped are different across scopes
    }

    #endregion

    #region Decorator Tests

    [RegisterContainer]
    [Singleton(typeof(ILogger), typeof(StringBuilderLogger))]
    [Scoped(typeof(IRepository<User>), typeof(EfRepository<User>))]
    [Decorate(typeof(IRepository<User>), typeof(AuditingRepositoryDecorator<User>))]
    public partial class GenericDecoratorContainer { }

    [Test]
    public async Task Decorator_CanBeApplied_ToClosedGenericService()
    {
        // Arrange
        var container = new GenericDecoratorContainer();

        // Act
        using IContainerScope scope = container.CreateScope();
        var userRepository = scope.Resolve<IRepository<User>>();
        var logger = container.Resolve<ILogger>() as StringBuilderLogger;

        userRepository.GetById(Guid.NewGuid());

        // Assert
        await Assert.That(userRepository).IsNotNull();
        await Assert.That(userRepository).IsAssignableFrom<AuditingRepositoryDecorator<User>>();
        await Assert.That(logger!.Output).Contains("AUDIT: Getting User with ID");
    }

    #endregion
}