**A compile-time Dependency Injection framework for .NET using C# Source Generators.**

## Features

- **Compile-Time Safety:** The source generator validates your container configuration during the build process,
  providing clear diagnostics for common errors.
- **No Runtime Reflection:** Eliminates reflection-based resolution, making the container's behavior predictable and
  easy to debug.
- **Advanced Lifetime Support:**
    - **Singleton:** One instance for the lifetime of the container.
    - **Scoped:** One instance per scope (`IContainerScope`).
    - **ScopedTo(tag):** A powerful lifetime scoped to a specific, named parent scope, enabling complex lifecycle
      patterns.
    - **Transient:** A new instance every time it's resolved.
- **Decorator Support:** Easily apply decorators to your services in a clean, declarative way.
- **Modular Configuration:** Organize your service registrations into modules and import them into your main container.
- **Automatic `IDisposable` Handling:** `IDisposable` services are automatically disposed when their owning scope ends.

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

### Injectable Framework Services

You can inject two special framework interfaces into your services' constructors:

- **`IResolver`**: Provides access to `Resolve<T>()`. Can be injected into any service. When resolved from a scope, it
  represents that scope.
- **`IContainer`**: Provides access to `Resolve<T>()`, `Dispose()`, and `CreateScope()`. **Can only be injected
  into `Singleton` services.**

```csharp
public class MyFactory
{
    private readonly IResolver _resolver;
    public MyFactory(IResolver resolver) // Legal for any lifetime
    {
        _resolver = resolver;
    }
}

public class AppOrchestrator
{
    private readonly IContainer _container;
    public AppOrchestrator(IContainer container) // Only legal for Singletons
    {
        _container = container;
    }
}
```

### Nested and Tagged Scopes

For complex lifecycles (e.g., a "Player Session" in a game), you can create nested scopes. To create a service that is
shared across a specific set of scopes, use the **`[ScopedTo(tag)]`** attribute.

```csharp
public enum ScopeTags { PlayerSession }

public class FactionManager { /* ... */ } // A long-lived session service
public class HubController(FactionManager fm) { /* ... */ } // A short-lived context service

[RegisterContainer]
[ScopedTo(ScopeTags.PlayerSession, typeof(FactionManager))]
[Scoped(typeof(HubController))]
public partial class GameContainer { }

// --- Usage ---
var container = new GameContainer();

// 1. Create the outer scope with a tag.
using (var sessionScope = container.CreateScope(nameof(ScopeTags.PlayerSession)))
{
    // 2. Create a nested scope for a specific context.
    using (var hubScope = sessionScope.CreateScope())
    {
        // When HubController is resolved, the framework automatically finds the
        // FactionManager instance from the parent "PlayerSession" scope.
        var controller = hubScope.Resolve<HubController>();
    }
}
```

### Decorators

Use the `[Decorate]` attribute to wrap a registered service. Decorators are applied in order, with module decorators
applied before container decorators, creating an intuitive "inside-out" wrapping structure.

```csharp
// --- Module Definition ---
[RegisterModule]
[Decorate(typeof(IOrderedService), typeof(DecoratorA))]
public interface IMyModule { }

// --- Container Registration ---
[RegisterContainer]
[ImportModule(typeof(IMyModule))]
[Singleton(typeof(IOrderedService), typeof(BaseOrderedService))]
[Decorate(typeof(IOrderedService), typeof(DecoratorB))] // Applied last
public partial class DecoratorOrderContainer { }

// Resulting object: new DecoratorB(new DecoratorA(new BaseOrderedService()))
// Execution order: "Base-A-B"
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
public partial class ModularContainer { }
```

## Compile-Time Diagnostics

The source generator catches configuration errors at compile time, providing specific diagnostics to help you fix them.

| Code          | Description                              | Example Cause                                                                                                                                      |
|:--------------|:-----------------------------------------|:---------------------------------------------------------------------------------------------------------------------------------------------------|
| **`NDI0001`** | **Container must be partial**            | The container class is missing the `partial` keyword.                                                                                              |
| **`NDI0002`** | **Incorrect attribute usage**            | A registration attribute like `[Singleton]` has an invalid number or type of constructor arguments.                                                |
| **`NDI0003`** | **No suitable public constructor**       | An implementation type has no public constructors for the container to use.                                                                        |
| **`NDI0004`** | **Cyclic dependency detected**           | `ServiceA` depends on `ServiceB`, and `ServiceB` depends back on `ServiceA`.                                                                       |
| **`NDI0005`** | **Service not registered**               | `ServiceA` depends on `IDependency`, but `IDependency` was never registered in the container.                                                      |
| **`NDI0006`** | **Implementation not assignable**        | `[Singleton(typeof(IService), typeof(WrongImpl))]` where `WrongImpl` doesn't implement `IService`.                                                 |
| **`NDI0007`** | **Cannot instantiate abstract type**     | An implementation type is an interface or an `abstract class`.                                                                                     |
| **`NDI0008`** | **Ambiguous constructors**               | An implementation has multiple "greediest" constructors (with the same number of parameters) and no `[Inject]` attribute to resolve the ambiguity. |
| **`NDI0009`** | **Multiple [Inject] constructors**       | An implementation has more than one constructor marked with the `[Inject]` attribute.                                                              |
| **`NDI0010`** | **Duplicate service registration**       | The same service type is registered in two different imported modules.                                                                             |
| **`NDI0011`** | **Invalid lifestyle mismatch**           | A long-lived service depends on a shorter-lived one (e.g., a `Singleton` injects a `Scoped` service), creating a "captive dependency".             |
| **`NDI0012`** | **Decorator for unregistered service**   | `[Decorate]` is used for a service type that has not been registered with a lifetime attribute.                                                    |
| **`NDI0013`** | **Ambiguous decorator constructors**     | A decorator has multiple candidate constructors, and the generator cannot determine which one to use.                                              |
| **`NDI0014`** | **Decorator missing required parameter** | A decorator's constructor does not include a parameter of the service type it is decorating.                                                       |
| **`NDI0015`** | **Decorator has captive dependency**     | A decorator (which inherits its service's lifetime) depends on a service with a shorter lifetime.                                                  |
| **`NDI0016`** | **Duplicate decorator registration**     | The same decorator type is applied to the same service more than once.                                                                             |
| **`NDI0017`** | **Imported type is not a module**        | A type passed to `[ImportModule]` is not an `interface` marked with `[RegisterModule]`.                                                            |
| **`NDI0018`** | **Conflicting `[ScopedTo]` lifetime**    | A service has `[ScopedTo]` combined with `[Singleton]` or `[Transient]`.                                                                           |
| **`NDI0019`** | **Duplicate lifetime definition**        | A service has multiple lifetime attributes applied (e.g., `[Scoped]` and `[Transient]`).                                                           |
| **`NDI0020`** | **Mismatched scope tag dependency**      | A service `ScopedTo("A")` depends on a service `ScopedTo("B")`, which is unsafe as the lifetimes are not guaranteed to align.                      |