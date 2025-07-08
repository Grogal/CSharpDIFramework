namespace CSharpDIFramework.Tests;

public partial class ModulesTests
{
    [Test]
    public async Task WhenModuleIsImported_ServicesFromModuleAreResolvable()
    {
        var container = new ModularContainer();

        using IContainerScope scope = container.CreateScope();
        var logger = scope.Resolve<ILogger>();
        var appService = scope.Resolve<IAppService>();

        await Assert.That(logger).IsNotNull();
        await Assert.That(appService).IsNotNull();
    }

    [Test]
    public async Task ContainerService_CanResolveDependency_FromImportedModule()
    {
        var container = new ContainerWithModuleDependency();

        using IContainerScope scope = container.CreateScope();
        var notificationService = scope.Resolve<INotificationService>();

        await Assert.That(notificationService).IsNotNull();
    }

    [Test]
    public async Task ContainerDecorator_CanBeApplied_ToModuleService()
    {
        var container = new ContainerDecoratingModuleService();

        var messageService = container.Resolve<IMessageService>();
        string result = messageService.GetMessage();

        await Assert.That(result).IsEqualTo("Hello World");
    }

    [Test]
    public async Task WhenModuleIsChained_ServicesFromAllModulesAreResolvable()
    {
        var container = new NestedModuleContainer();

        var logger = container.Resolve<ILogger>();
        var config = container.Resolve<IConfigService>();

        await Assert.That(logger).IsNotNull();
        await Assert.That(config).IsNotNull();
    }

    [Test]
    public async Task WhenModulesAreCyclic_GeneratorSucceeds_AndServicesAreResolvable()
    {
        var container = new CyclicModuleContainer();

        var singleton = container.Resolve<ISingletonService>();
        using IContainerScope scope = container.CreateScope();
        var scoped = scope.Resolve<IScopedService>();

        await Assert.That(singleton).IsNotNull();
        await Assert.That(scoped).IsNotNull();
    }

    [RegisterContainer]
    [ImportModule(typeof(ILoggingModule))]
    [Transient(typeof(IAppService), typeof(AppService))]
    public partial class ModularContainer { }

    [RegisterContainer]
    [ImportModule(typeof(ILoggingModule))]
    [Transient(typeof(INotificationService), typeof(NotificationService))]
    public partial class ContainerWithModuleDependency { }

    [RegisterContainer]
    [ImportModule(typeof(IMessageModule))]
    [Decorate(typeof(IMessageService), typeof(WorldMessageDecorator))] // Decorator in container
    public partial class ContainerDecoratingModuleService { }

    [RegisterContainer]
    [ImportModule(typeof(IChainedModule))] // Container imports the module that imports other modules
    public partial class NestedModuleContainer { }

    [RegisterContainer]
    [ImportModule(typeof(ICyclicModuleA))] // Import one of the cyclic modules
    public partial class CyclicModuleContainer { }
}