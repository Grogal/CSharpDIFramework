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

    public static readonly DiagnosticDescriptor ImplementationNotAssignable = new(
        "NDI0005",
        "Implementation type not assignable",
        "The implementation type '{0}' cannot be assigned to service type '{1}'",
        "CSharpDIFramework.Usage",
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor ImplementationIsAbstract = new(
        "NDI0010",
        "Cannot instantiate abstract type",
        "The implementation type '{0}' is abstract or an interface and cannot be instantiated",
        "CSharpDIFramework.Usage",
        DiagnosticSeverity.Error,
        true
    );
}