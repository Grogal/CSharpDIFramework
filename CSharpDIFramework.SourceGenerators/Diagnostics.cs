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
        id: "NDI0004",
        title: "Cyclic dependency detected",
        messageFormat: "A cyclic dependency was detected for service '{0}'. Cycle path: {1}.",
        category: "CSharpDIFramework.Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    public static readonly DiagnosticDescriptor ServiceNotRegistered = new(
        id: "NDI0005",
        title: "Service not registered",
        messageFormat: "The service '{0}' is required by '{1}' but is not registered in the container",
        category: "CSharpDIFramework.Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
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
        id: "NDI0008",
        title: "Ambiguous constructors",
        messageFormat:
        "The implementation type '{0}' has multiple public constructors with {1} parameters. Please use the [Inject] attribute to specify which constructor to use.",
        category: "CSharpDIFramework.Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    public static readonly DiagnosticDescriptor MultipleInjectConstructors = new(
        id: "NDI0009",
        title: "Multiple [Inject] constructors",
        messageFormat: "The implementation type '{0}' has multiple constructors marked with the [Inject] attribute. Only one is permitted.",
        category: "CSharpDIFramework.Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
}