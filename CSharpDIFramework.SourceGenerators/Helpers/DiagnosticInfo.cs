using System.Collections.Generic;
using System.Linq;

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
        string[]? arr = MessageArgs.GetArray();
        IEnumerable<object>? objs = arr;
        object[]? toArr = objs?.ToArray();
        return Diagnostic.Create(Descriptor, Location?.ToLocation(), toArr);
    }
}