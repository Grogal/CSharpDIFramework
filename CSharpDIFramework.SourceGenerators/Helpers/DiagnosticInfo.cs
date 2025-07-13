using Microsoft.CodeAnalysis;

namespace CSharpDIFramework.SourceGenerators;

internal sealed record DiagnosticInfo
{
    public DiagnosticInfo(DiagnosticDescriptor descriptor, LocationInfo? location, params string[] messageArgs)
        : this(
            descriptor,
            location,
            new EquatableArray<string>(messageArgs)
        ) { }

    private DiagnosticInfo(DiagnosticDescriptor descriptor, LocationInfo? location, EquatableArray<string> messageArgs)
    {
        Descriptor = descriptor;
        Location = location;
        MessageArgs = messageArgs;
    }

    public DiagnosticDescriptor Descriptor { get; }
    public LocationInfo? Location { get; }
    public EquatableArray<string> MessageArgs { get; }
    public bool IsError => Descriptor.DefaultSeverity == DiagnosticSeverity.Error;
    public bool IsWarning => Descriptor.DefaultSeverity == DiagnosticSeverity.Warning;

    public static DiagnosticInfo Create(DiagnosticDescriptor descriptor, Location location, params string[] messageArgs)
    {
        return new DiagnosticInfo(descriptor, LocationInfo.CreateFrom(location), messageArgs);
    }

    public static DiagnosticInfo Create(DiagnosticDescriptor descriptor, LocationInfo? location, params string[] messageArgs)
    {
        return new DiagnosticInfo(descriptor, location, messageArgs);
    }

    public Diagnostic CreateDiagnostic()
    {
        string[]? messageArgs = MessageArgs.GetArray();
        object[]? args = null;

        if (messageArgs is not null)
        {
            args = new object[messageArgs.Length];
            for (var i = 0; i < messageArgs.Length; i++)
            {
                args[i] = messageArgs[i];
            }
        }

        return Diagnostic.Create(Descriptor, Location?.ToLocation(), args);
    }
}