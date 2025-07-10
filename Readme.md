**A compile-time Dependency Injection framework for .NET using C# Source Generators.**

This framework helps you manage dependencies in your application by generating the necessary container code at compile
time. This catches common configuration errors before you run your code and ensures your dependency injection setup is
fast and reliable.

## Features

- **Compile-Time Safety:** The source generator validates your container configuration during the build process,
  providing clear diagnostics for common errors.
- **No Runtime Reflection:** Eliminates reflection-based resolution, making the container's behavior predictable and
  easy to debug.
- **Full Lifetime Support:**
    - **Singleton:** One instance for the lifetime of the container.
    - **Scoped:** One instance per scope (`IContainerScope`).
    - **Transient:** A new instance every time it's resolved.
- **Decorator Support:** Easily apply decorators to your services in a clean, declarative way.
- **Modular Configuration:** Organize your service registrations into modules and import them into your main container.
- **Automatic `IDisposable` Handling:** Scoped and Transient services are disposed when their scope ends. Singleton
  services are disposed when the root container is disposed.

## Getting Started

### 1. Define Your Container

Create a `partial class` and mark it with the `[RegisterContainer]` attribute.

```csharp
using CSharpDIFramework;

[RegisterContainer]
public partial class MyContainer
{
    // The generator will implement the container here
}
```

### 2. Register Your Services

Use attributes to declare your services and their lifetimes directly on the container class.

```csharp
public interface IMyService { }
public class MyService : IMyService { }

public interface ILogger { }
public class ConsoleLogger : ILogger { }

[RegisterContainer]
[Singleton(typeof(ILogger), typeof(ConsoleLogger))]
[Scoped(typeof(IMyService), typeof(MyService))]
[Transient(typeof(SomeOtherService))] // Registers a concrete type
public partial class MyContainer { }
```

### 3. Build and Use the Container

Once your project is built, the source generator will have created the implementation for `MyContainer`.

```csharp
// The 'new' keyword works because the generator created a constructor.
var container = new MyContainer();

// Resolve a singleton from the root container
var logger = container.Resolve<ILogger>();
```

### 4. Work with Scopes

For `Scoped` and `Transient` services, you must create a scope. The scope manages the lifetime of these services.

```csharp
using (IContainerScope scope = container.CreateScope())
{
    // Resolve a scoped service. This instance lives for the duration of the 'scope'.
    var service1 = scope.Resolve<IMyService>();
    var service2 = scope.Resolve<IMyService>();
    // service1 is the same instance as service2

    // Resolve a transient service. A new instance is created each time.
    var transient1 = scope.Resolve<SomeOtherService>();
    var transient2 = scope.Resolve<SomeOtherService>();
    // transient1 is a different instance from transient2
}
// At the end of the 'using' block, any IDisposable services
// resolved from the scope will be disposed.
```

> **Note:** Resolving a `Scoped` or `Transient` service directly from the root container will throw an
`InvalidOperationException`. You must create a scope.

## Advanced Features

### Constructor Injection

The generator automatically selects a constructor for your implementation types based on these rules:

1. A single constructor marked with `[Inject]`.
2. If no `[Inject]` attribute is found, the single "greediest" public constructor (the one with the most parameters).

```csharp
public class ServiceWithDependencies
{
    // This constructor will be chosen because it has more parameters.
    public ServiceWithDependencies(ILogger logger, IMyService service) { }

    public ServiceWithDependencies(ILogger logger) { }
}

public class ServiceWithAttribute
{
    public ServiceWithAttribute(ILogger logger, IMyService service) { }

    // This constructor will be chosen because of the [Inject] attribute.
    [Inject]
    public ServiceWithAttribute(ILogger logger) { }
}
```

### Decorators

Use the `[Decorate]` attribute to wrap a registered service with another implementation. Decorators are applied in the
order they are registered.

```csharp
// 1. Service
public interface IGreetingService { string Greet(); }
public class GreetingService : IGreetingService { public string Greet() => "Hello"; }

// 2. Decorator
public class ExclamationDecorator(IGreetingService inner) : IGreetingService
{
    public string Greet() => $"{inner.Greet()}!";
}

// 3. Container Registration
[RegisterContainer]
[Singleton(typeof(IGreetingService), typeof(GreetingService))]
[Decorate(typeof(IGreetingService), typeof(ExclamationDecorator))] // Applied after registration
public partial class DecoratorContainer { }

// Usage:
var service = new DecoratorContainer().Resolve<IGreetingService>();
Console.WriteLine(service.Greet()); // Output: Hello!
```

### Modules

Organize registrations into separate modules and import them. A module is any `interface` marked with
`[RegisterModule]`.

```csharp
// --- Module Definition ---
[RegisterModule]
[Singleton(typeof(IConfigService), typeof(ConfigService))]
public interface IConfigModule { }

// --- Container Importing the Module ---
[RegisterContainer]
[ImportModule(typeof(IConfigModule))] // Import services from IConfigModule
[Singleton(typeof(ILogger), typeof(ConsoleLogger))]
public partial class ModularContainer { }
```

## Compile-Time Diagnostics

The source generator catches configuration errors at compile time, providing specific diagnostics to help you fix them.

| Code      | Description                              | Example Cause                                                                                       |
|:----------|:-----------------------------------------|:----------------------------------------------------------------------------------------------------|
| `NDI0001` | **Container must be partial**            | The container class is missing the `partial` keyword.                                               |
| `NDI0002` | **Incorrect attribute**                  | A registration attribute like `[Singleton]` has an invalid combination of constructor arguments.    |
| `NDI0003` | **No suitable public constructor**       | An implementation type has no public constructors.                                                  |
| `NDI0004` | **Cyclic dependency detected**           | `ServiceA` depends on `ServiceB`, and `ServiceB` depends on `ServiceA`.                             |
| `NDI0005` | **Service not registered**               | `ServiceA` depends on `IDependency`, but `IDependency` was never registered.                        |
| `NDI0006` | **Implementation not assignable**        | `[Singleton(typeof(IService), typeof(WrongImpl))]` where `WrongImpl` doesn't implement `IService`.  |
| `NDI0007` | **Cannot instantiate abstract type**     | An implementation type is an interface or an abstract class.                                        |
| `NDI0008` | **Ambiguous constructors**               | An implementation has multiple constructors with the same (greediest) number of parameters.         |
| `NDI0009` | **Multiple [Inject] constructors**       | An implementation has more than one constructor marked with `[Inject]`.                             |
| `NDI0010` | **Duplicate services registration**      | A service type (e.g., `ILogger`) is registered more than once in the container.                     |
| `NDI0011` | **Invalid lifestyle mismatch**           | A `Singleton` service tries to inject a `Scoped` service (a captive dependency).                    |
| `NDI0012` | **Decorator for unregistered service**   | `[Decorate]` is used for a service that has not been registered.                                    |
| `NDI0013` | **Ambiguous decorator constructors**     | A decorator has multiple candidate constructors, and the generator cannot choose one.               |
| `NDI0014` | **Decorator missing required parameter** | A decorator's constructor does not have a parameter for the service it is decorating.               |
| `NDI0015` | **Decorator has captive dependency**     | A decorator inherits a long lifetime (e.g., Singleton) and depends on a service with a shorter one. |
| `NDI0016` | **Duplicate decorator registration**     | The same decorator type is applied to the same service more than once.                              |
| `NDI0017` | **Imported type is not a module**        | A type passed to `[ImportModule]` is not an interface marked with `[RegisterModule]`.               |