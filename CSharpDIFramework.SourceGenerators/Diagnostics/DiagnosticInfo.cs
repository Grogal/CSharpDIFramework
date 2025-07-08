using Microsoft.CodeAnalysis;

namespace CSharpDIFramework.SourceGenerators;

internal sealed record DiagnosticInfo
{
    public DiagnosticInfo(
        DiagnosticDescriptor descriptor,
        Location location,
        string? messageArg1 = null,
        string? messageArg2 = null,
        string? messageArg3 = null)
    {
        Descriptor = descriptor;
        Location = LocationInfo.CreateFrom(location);
        MessageArg1 = messageArg1;
        MessageArg2 = messageArg2;
        MessageArg3 = messageArg3;
    }

    public DiagnosticInfo(
        DiagnosticDescriptor descriptor,
        LocationInfo? location,
        string? messageArg1 = null,
        string? messageArg2 = null,
        string? messageArg3 = null)
    {
        Descriptor = descriptor;
        Location = location;
        MessageArg1 = messageArg1;
        MessageArg2 = messageArg2;
        MessageArg3 = messageArg3;
    }

    public DiagnosticDescriptor Descriptor { get; }
    public LocationInfo? Location { get; }
    public string? MessageArg1 { get; }
    public string? MessageArg2 { get; }
    public string? MessageArg3 { get; }

    public Diagnostic CreateDiagnostic()
    {
        var diagnostic = Diagnostic.Create(
            Descriptor,
            Location?.ToLocation(),
            MessageArg1,
            MessageArg2,
            MessageArg3
        );
        return diagnostic;
    }
}