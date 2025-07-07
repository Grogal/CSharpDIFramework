using TUnit.Assertions.AssertConditions.Throws;

namespace CSharpDIFramework.Tests;

public partial class ContainerTests
{
    [Test]
    public async ValueTask CanCreateEmptyContainer()
    {
        var container = new EmptyContainer();

        await Assert.That(container).IsNotNull();
    }

    [Test]
    public async ValueTask SimpleResolveContainer()
    {
        var container = new SimpleContainer();
        var service = container.Resolve<EmptyService>();
        var service2 = container.Resolve<EmptyService>();

        await Assert.That(service).IsNotNull();
        await Assert.That(service).IsSameReferenceAs(service2);
        await Assert.That(() => container.Resolve<IInterface>()).Throws<InvalidOperationException>();
    }

    [Test]
    public async ValueTask InterfaceResolveContainer()
    {
        var container = new SimpleInterfaceContainer();
        var service = container.Resolve<IInterface>();

        await Assert.That(service).IsNotNull();
        await Assert.That(service is InterfaceImplementation).IsTrue();
    }

    public class EmptyService { }

    [RegisterContainer]
    [Singleton(typeof(EmptyService))]
    public partial class SimpleContainer { }

    public interface IInterface { }

    public class InterfaceImplementation : IInterface { }

    public abstract class AbstractService { }

    public class FromAbstractEmptyService : AbstractService { }

    [RegisterContainer]
    public partial class EmptyContainer { }

    [RegisterContainer]
    [Singleton(typeof(InterfaceImplementation))]
    public partial class InterfaceContainer { }

    [RegisterContainer]
    [Singleton(typeof(IInterface), typeof(InterfaceImplementation))]
    public partial class SimpleInterfaceContainer { }

    [RegisterContainer]
    [Singleton(typeof(AbstractService), typeof(FromAbstractEmptyService))]
    public partial class FromAbstractContainer { }
}