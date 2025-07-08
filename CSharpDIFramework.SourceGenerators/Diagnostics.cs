using Microsoft.CodeAnalysis;

namespace CSharpDIFramework.SourceGenerators;

internal static class Diagnostics
{
    public static readonly DiagnosticDescriptor ContainerNotPartial = new(
        "NDI0001",
        "Container must be partial",
        "The container class '{0}' must be declared as partial",
        "CSharpDIFramework.Usage",
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor IncorrectAttribute = new(
        "NDI0002",
        "Current attribute is not correct",
        "The attribute '{0}' is not applicable to the current context",
        "CSharpDIFramework.Usage",
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor NoPublicConstructor = new(
        "NDI0003",
        "No suitable public constructor found",
        "The implementation type '{0}' has no public parameterless constructor. A public constructor is required for instantiation.",
        "CSharpDIFramework.Usage",
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor CyclicDependency = new(
        "NDI0004",
        "Cyclic dependency detected",
        "A cyclic dependency was detected for service '{0}'. Cycle path: {1}.",
        "CSharpDIFramework.Usage",
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor ServiceNotRegistered = new(
        "NDI0005",
        "Service not registered",
        "The service '{0}' is required by '{1}' but is not registered in the container",
        "CSharpDIFramework.Usage",
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor ImplementationNotAssignable = new(
        "NDI0006",
        "Implementation type not assignable",
        "The implementation type '{0}' cannot be assigned to service type '{1}'",
        "CSharpDIFramework.Usage",
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor ImplementationIsAbstract = new(
        "NDI0007",
        "Cannot instantiate abstract type",
        "The implementation type '{0}' is abstract or an interface and cannot be instantiated",
        "CSharpDIFramework.Usage",
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor AmbiguousConstructors = new(
        "NDI0008",
        "Ambiguous constructors",
        "The implementation type '{0}' has multiple public constructors with {1} parameters. Please use the [Inject] attribute to specify which constructor to use.",
        "CSharpDIFramework.Usage",
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor MultipleInjectConstructors = new(
        "NDI0009",
        "Multiple [Inject] constructors",
        "The implementation type '{0}' has multiple constructors marked with the [Inject] attribute. Only one is permitted.",
        "CSharpDIFramework.Usage",
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor DuplicateRegistrationConstructors = new(
        "NDI0010",
        "Duplicate services registration",
        "The service type '{0}' is registered multiple times with the same implementation and constructor signature. Duplicate registrations are not allowed.",
        "CSharpDIFramework.Usage",
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor LifestyleMismatch = new(
        "NDI0011",
        "Invalid lifestyle mismatch",
        "Service '{0}' with a '{1}' lifetime cannot depend on service '{2}' with a shorter '{3}' lifetime. This creates a captive dependency.",
        "CSharpDIFramework.Usage",
        DiagnosticSeverity.Error, // This is severe enough to be an error.
        true
    );

    public static readonly DiagnosticDescriptor DecoratorForUnregisteredService = new(
        "NDI0012",
        "Decorator for unregistered service",
        "Cannot apply decorator '{0}' because the service '{1}' has not been registered",
        "CSharpDIFramework.Usage", DiagnosticSeverity.Error, true
    );

    public static readonly DiagnosticDescriptor AmbiguousDecoratorConstructors = new(
        "NDI0013",
        "Ambiguous decorator constructors",
        "The decorator '{0}' has multiple candidate constructors with {1} parameters. Please use the [Inject] attribute to specify which one to use.",
        "CSharpDIFramework.Usage", DiagnosticSeverity.Error, true
    );

    public static readonly DiagnosticDescriptor DecoratorMissingDecoratedServiceParameter = new(
        "NDI0014",
        "Decorator missing required parameter",
        "The decorator '{0}' must have a public constructor with exactly one parameter of the decorated service type '{1}'", "CSharpDIFramework.Usage",
        DiagnosticSeverity.Error, true
    );

    public static readonly DiagnosticDescriptor DecoratorCaptiveDependency = new(
        "NDI0015",
        "Decorator has captive dependency",
        "The decorator '{0}', which inherits a '{1}' lifetime from its service, cannot depend on service '{2}' with a shorter '{3}' lifetime",
        "CSharpDIFramework.Usage", DiagnosticSeverity.Error, true
    );

    public static readonly DiagnosticDescriptor DuplicateDecoratorRegistration = new(
        "NDI0016", "Duplicate decorator registration",
        "The decorator '{0}' is already registered for service '{1}'. Applying the same decorator multiple times is not allowed.", "CSharpDIFramework.Usage",
        DiagnosticSeverity.Error, true
    );
}