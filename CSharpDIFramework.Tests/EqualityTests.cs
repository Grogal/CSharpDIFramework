using System.Diagnostics.CodeAnalysis;

using CSharpDIFramework.SourceGenerators;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace CSharpDIFramework.Tests;

public class EqualityTests
{
    [SuppressMessage("MicrosoftCodeAnalysisReleaseTracking", "RS2008:Enable analyzer release tracking")]
    private static readonly DiagnosticDescriptor s_testDescriptor = new("TEST001", "Title", "Message", "Category", DiagnosticSeverity.Error, true);

    [Test]
    public async Task EquatableArray_WithIdenticalContent_ShouldBeEqual()
    {
        // Arrange
        var array1 = new EquatableArray<string>(new[] { "A", "B" });
        var array2 = new EquatableArray<string>(new[] { "A", "B" });

        // Act & Assert
        await Assert.That(array1).IsEqualTo(array2);
        await Assert.That(array1.GetHashCode()).IsEqualTo(array2.GetHashCode());
    }

    [Test]
    public async Task EquatableArray_WithDifferentContent_ShouldNotBeEqual()
    {
        // Arrange
        var array1 = new EquatableArray<string>(new[] { "A", "B" });
        var array2 = new EquatableArray<string>(new[] { "A", "C" }); // Different content

        // Act & Assert
        await Assert.That(array1).IsNotEqualTo(array2);
    }

    [Test]
    public async Task EquatableArray_FromListConstructor_ShouldBeEqual()
    {
        // Arrange
        var list1 = new List<string> { "X", "Y", "Z" };
        var list2 = new List<string> { "X", "Y", "Z" };

        var eqArray1 = new EquatableArray<string>(list1);
        var eqArray2 = new EquatableArray<string>(list2);

        // Act & Assert
        await Assert.That(eqArray1).IsEqualTo(eqArray2);
        await Assert.That(eqArray1.GetHashCode()).IsEqualTo(eqArray2.GetHashCode());
    }

    [Test]
    public async Task LocationInfo_WithIdenticalValues_ShouldBeEqual()
    {
        // Arrange
        var location1 = new LocationInfo("file.cs", new TextSpan(10, 5), new LinePositionSpan(new LinePosition(1, 10), new LinePosition(1, 15)));
        var location2 = new LocationInfo("file.cs", new TextSpan(10, 5), new LinePositionSpan(new LinePosition(1, 10), new LinePosition(1, 15)));

        // Act & Assert
        // Records provide value equality for free.
        await Assert.That(location1).IsEqualTo(location2);
        await Assert.That(location1.GetHashCode()).IsEqualTo(location2.GetHashCode());
    }

    [Test]
    public async Task DiagnosticInfo_WithIdenticalValues_ShouldBeEqual()
    {
        // This is the most important test. It simulates two separate runs of the parser
        // creating what should be identical diagnostic information.

        // Arrange
        var location1 = new LocationInfo("file.cs", new TextSpan(10, 5), new LinePositionSpan(new LinePosition(1, 10), new LinePosition(1, 15)));
        var location2 = new LocationInfo("file.cs", new TextSpan(10, 5), new LinePositionSpan(new LinePosition(1, 10), new LinePosition(1, 15)));

        var args1 = new[] { "TypeName" };
        var args2 = new[] { "TypeName" };

        // Even though we create two separate instances, they should be equal in value.
        var diagInfo1 = DiagnosticInfo.Create(s_testDescriptor, location1, args1);
        var diagInfo2 = DiagnosticInfo.Create(s_testDescriptor, location2, args2);

        // Act & Assert
        await Assert.That(diagInfo1).IsEqualTo(diagInfo2);
        await Assert.That(diagInfo1.GetHashCode()).IsEqualTo(diagInfo2.GetHashCode());
    }

    [Test]
    public async Task DiagnosticInfo_WithDifferentMessageArgs_ShouldNotBeEqual()
    {
        // Arrange
        var location = new LocationInfo("file.cs", new TextSpan(10, 5), new LinePositionSpan(new LinePosition(1, 10), new LinePosition(1, 15)));

        var diagInfo1 = DiagnosticInfo.Create(s_testDescriptor, location, "TypeName1");
        var diagInfo2 = DiagnosticInfo.Create(s_testDescriptor, location, "TypeName2"); // Different arg

        // Act & Assert
        await Assert.That(diagInfo1).IsNotEqualTo(diagInfo2);
    }
}