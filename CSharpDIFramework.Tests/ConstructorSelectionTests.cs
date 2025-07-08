namespace CSharpDIFramework.Tests;

public class ConstructorSelectionTests
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
}