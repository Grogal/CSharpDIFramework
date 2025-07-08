#if false
namespace CSharpDIFramework.Tests;

public interface ISharedService { }

public class ModuleSharedService : ISharedService { }

public class ContainerSharedService : ISharedService { }

[RegisterModule]
[Singleton(typeof(ISharedService), typeof(ModuleSharedService))]
public interface IDuplicateRegistrationModule { }

[RegisterContainer]
[ImportModule(typeof(IDuplicateRegistrationModule))]
[Singleton(typeof(ISharedService), typeof(ContainerSharedService))] // <-- Duplicate registration
public partial class DuplicateRegistrationContainer { }

#endif