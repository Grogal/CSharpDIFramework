using System.Text;

namespace CSharpDIFramework.SourceGenerators;

internal static class CodeGenerator
{
    public static string Generate(ContainerBlueprint blueprint)
    {
        var sb = new StringBuilder();

        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();

        if (!string.IsNullOrEmpty(blueprint.Namespace))
        {
            sb.AppendLine($"namespace {blueprint.Namespace!};");
            sb.AppendLine();
        }

        var indentLevel = 0;
        foreach (string declaration in blueprint.ContainingTypeDeclarations)
        {
            sb.AppendLine($"{new string(' ', indentLevel * 4)}{declaration}");
            sb.AppendLine($"{new string(' ', indentLevel * 4)}{{");
            indentLevel++;
        }

        var baseIndent = new string(' ', indentLevel * 4);
        sb.AppendLine($"{baseIndent}public partial class {blueprint.ContainerName} : global::CSharpDIFramework.IContainer");
        sb.AppendLine($"{baseIndent}{{");
        sb.AppendLine($"{baseIndent}    private bool _isDisposed;");

        List<ResolvedService> singletons = blueprint.Services.GetArray()!.Where(s => s.Lifetime == ServiceLifetime.Singleton).ToList();

        GenerateSingletonFields(sb, singletons, indentLevel + 1);
        GenerateConstructor(sb, blueprint.ContainerName, singletons, indentLevel + 1);
        GenerateSingletonFactories(sb, singletons, indentLevel + 1);
        GenerateResolveMethod(sb, blueprint, indentLevel + 1);
        GenerateCreateScopeMethod(sb, indentLevel + 1);
        GenerateDisposeMethod(sb, blueprint, indentLevel + 1);
        GenerateScopeClass(sb, blueprint, indentLevel + 1);

        sb.AppendLine($"{baseIndent}}}");

        for (int i = indentLevel - 1; i >= 0; i--)
        {
            sb.AppendLine($"{new string(' ', i * 4)}}}");
        }

        return sb.ToString();
    }

    public static string GenerateDummyContainer(ServiceProviderDescription description)
    {
        var sb = new StringBuilder();

        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("// DI Container generation failed due to errors.");
        sb.AppendLine("// This is a dummy implementation. Please see the Error List for details.");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();

        if (!string.IsNullOrEmpty(description.Namespace))
        {
            sb.AppendLine($"namespace {description.Namespace!};");
            sb.AppendLine();
        }

        foreach (string declaration in description.ContainingTypeDeclarations)
        {
            sb.AppendLine(declaration);
            sb.AppendLine("{");
        }

        var indent = new string(' ', description.ContainingTypeDeclarations.Count * 4);
        sb.AppendLine($"{indent}public partial class {description.ContainerName}");
        sb.AppendLine($"{indent}{{");
        sb.AppendLine($"{indent}    private const string ErrorMessage = \"CSharpDIFramework container generation failed. See compile-time errors.\";");
        sb.AppendLine($"{indent}    public TService Resolve<TService>() => throw new global::System.InvalidOperationException(ErrorMessage);");
        sb.AppendLine($"{indent}    public void Dispose() {{}}");
        sb.AppendLine($"{indent}}}");

        for (int i = description.ContainingTypeDeclarations.Count - 1; i >= 0; i--)
        {
            sb.AppendLine($"{new string(' ', i * 4)}}}");
        }

        return sb.ToString();
    }

    private static void GenerateSingletonFields(StringBuilder sb, List<ResolvedService> singletons, int indentLevel)
    {
        var indent = new string(' ', indentLevel * 4);
        foreach (ResolvedService s in singletons)
        {
            string serviceType = s.ServiceTypeFullName;
            sb.AppendLine($"{indent}private readonly global::System.Lazy<{serviceType}> {GetSingletonFieldName(s.ServiceTypeFullName)};");
        }

        sb.AppendLine();
    }

    private static void GenerateConstructor(StringBuilder sb, string containerName, List<ResolvedService> singletons, int indentLevel)
    {
        var indent = new string(' ', indentLevel * 4);
        var innerIndent = new string(' ', (indentLevel + 1) * 4);

        sb.AppendLine($"{indent}public {containerName}()");
        sb.AppendLine($"{indent}{{");

        foreach (ResolvedService s in singletons)
        {
            string serviceType = s.ServiceTypeFullName;

            sb.AppendLine(
                $"{innerIndent}{GetSingletonFieldName(s.ServiceTypeFullName)} = new global::System.Lazy<{serviceType}>(() => {GetFactoryMethodName(s.ServiceTypeFullName)}());"
            );
        }

        sb.AppendLine($"{indent}}}");
        sb.AppendLine();
    }

    private static void GenerateSingletonFactories(StringBuilder sb, List<ResolvedService> singletons, int indentLevel)
    {
        var indent = new string(' ', indentLevel * 4);
        var innerIndent = new string(' ', (indentLevel + 1) * 4);

        foreach (ResolvedService service in singletons)
        {
            string serviceType = service.ServiceTypeFullName;

            sb.AppendLine($"{indent}private {serviceType} {GetFactoryMethodName(service.ServiceTypeFullName)}()");
            sb.AppendLine($"{indent}{{");

            sb.AppendLine($"{innerIndent}return {GenerateInstanceCreation(service, "this")};");

            sb.AppendLine($"{indent}}}");
            sb.AppendLine();
        }
    }

    private static void GenerateResolveMethod(StringBuilder sb, ContainerBlueprint blueprint, int indentLevel)
    {
        var indent = new string(' ', indentLevel * 4);
        var innerIndent = new string(' ', (indentLevel + 1) * 4);
        var doubleIndent = new string(' ', (indentLevel + 2) * 4);

        sb.AppendLine($"{indent}public TService Resolve<TService>()");
        sb.AppendLine($"{indent}{{");

        var isFirst = true;
        foreach (ResolvedService service in blueprint.Services)
        {
            string ifPrefix = isFirst ? "if" : "else if";
            isFirst = false;

            string serviceType = service.ServiceTypeFullName;
            sb.AppendLine($"{innerIndent}{ifPrefix} (typeof(TService) == typeof({serviceType}))");
            sb.AppendLine($"{innerIndent}{{");

            switch (service.Lifetime)
            {
                case ServiceLifetime.Singleton:
                    sb.AppendLine($"{doubleIndent}return (TService)(object){GetSingletonFieldName(service.ServiceTypeFullName)}.Value;");
                    break;
                case ServiceLifetime.Transient:
                    sb.AppendLine(
                        $"{doubleIndent}throw new global::System.InvalidOperationException($\"Service '{service.ServiceTypeFullName}' has a Transient lifetime and cannot be resolved from the root container. Please create and resolve from a scope.\");"
                    );
                    break;
                case ServiceLifetime.Scoped:
                    sb.AppendLine(
                        $"{doubleIndent}throw new global::System.InvalidOperationException($\"Service '{service.ServiceTypeFullName}' has a {service.Lifetime} lifetime and cannot be resolved from the root container. Please create and resolve from a scope.\");"
                    );
                    break;
                case ServiceLifetime.ScopedToTag:
                    sb.AppendLine(
                        $"{new string(' ', (indentLevel + 2) * 4)}throw new global::System.InvalidOperationException($\"Service '{{typeof(TService).FullName}}' is scoped to tag '{service.SourceRegistration.ScopeTag}' and cannot be resolved from the root container. A scope with this tag must be created first.\");"
                    );
                    break;
            }

            sb.AppendLine($"{innerIndent}}}");
        }

        sb.AppendLine(
            $"{innerIndent}throw new global::System.InvalidOperationException($\"Service of type {{typeof(TService).FullName}} is not registered.\");"
        );
        sb.AppendLine($"{indent}}}");
    }

    private static void GenerateCreateScopeMethod(StringBuilder sb, int indentLevel)
    {
        var indent = new string(' ', indentLevel * 4);
        sb.AppendLine($"{indent}public global::CSharpDIFramework.IContainerScope CreateScope()");
        sb.AppendLine($"{indent}{{");
        sb.AppendLine($"{indent}    return new Scope(this, this, null);"); // Pass null for the tag
        sb.AppendLine($"{indent}}}");
        sb.AppendLine();

        sb.AppendLine($"{indent}public global::CSharpDIFramework.IContainerScope CreateScope(string tag)");
        sb.AppendLine($"{indent}{{");
        sb.AppendLine($"{indent}    return new Scope(this, this, tag);");
        sb.AppendLine($"{indent}}}");
        sb.AppendLine();
    }

    private static void GenerateScopeClass(StringBuilder sb, ContainerBlueprint blueprint, int indentLevel)
    {
        var indent = new string(' ', indentLevel * 4);

        sb.AppendLine($"{indent}private sealed partial class Scope : global::CSharpDIFramework.IContainerScope");
        sb.AppendLine($"{indent}{{");
        AddScopeFields(sb, blueprint, indentLevel + 1);
        AddScopeConstructor(sb, blueprint, indentLevel + 1);
        AddScopeResolveMethod(sb, blueprint, indentLevel + 1); // Use helper
        AddScopeCreateScopeMethods(sb, indentLevel + 1); // Use helper
        AddScopeDisposeMethod(sb, indentLevel + 1); // Use helper
        sb.AppendLine($"{indent}}}");
    }

    private static void AddScopeFields(StringBuilder sb, ContainerBlueprint blueprint, int indentLevel)
    {
        var indent = new string(' ', indentLevel * 4);
        sb.AppendLine($"{indent}private readonly {blueprint.ContainerName} _root;");
        sb.AppendLine($"{indent}private readonly global::CSharpDIFramework.IResolver _parentResolver;");
        sb.AppendLine($"{indent}private readonly string? _tag; // NEW");
        sb.AppendLine($"{indent}private readonly global::System.Collections.Generic.Dictionary<global::System.Type, object> _scopedInstances = new();");
        sb.AppendLine($"{indent}private readonly global::System.Collections.Generic.List<global::System.IDisposable> _disposables = new();");
        sb.AppendLine($"{indent}private bool _isDisposed;");
        sb.AppendLine();
    }

    private static void AddScopeConstructor(StringBuilder sb, ContainerBlueprint blueprint, int indentLevel)
    {
        var indent = new string(' ', indentLevel * 4);
        sb.AppendLine($"{indent}public Scope({blueprint.ContainerName} root, global::CSharpDIFramework.IResolver parentResolver, string? tag)");
        sb.AppendLine($"{indent}{{");
        sb.AppendLine($"{indent}    _root = root;");
        sb.AppendLine($"{indent}    _parentResolver = parentResolver;");
        sb.AppendLine($"{indent}    _tag = tag;");
        sb.AppendLine($"{indent}}}");
        sb.AppendLine();
    }

    private static void AddScopeCreateScopeMethods(StringBuilder sb, int indentLevel)
    {
        var indent = new string(' ', indentLevel * 4);
        sb.AppendLine($"{indent}public global::CSharpDIFramework.IContainerScope CreateScope()");
        sb.AppendLine($"{indent}{{ return new Scope(_root, this, null); }}");
        sb.AppendLine();
        sb.AppendLine($"{indent}public global::CSharpDIFramework.IContainerScope CreateScope(string tag)");
        sb.AppendLine($"{indent}{{ return new Scope(_root, this, tag); }}");
        sb.AppendLine();
    }

    private static void AddScopeResolveMethod(StringBuilder sb, ContainerBlueprint blueprint, int indentLevel)
    {
        var indent = new string(' ', indentLevel * 4);
        var innerIndent = new string(' ', (indentLevel + 1) * 4);
        var tripleIndent = new string(' ', (indentLevel + 2) * 4);

        sb.AppendLine($"{indent}public TService Resolve<TService>()");
        sb.AppendLine($"{indent}{{");
        sb.AppendLine($"{innerIndent}if (_isDisposed) throw new global::System.ObjectDisposedException(nameof(Scope));");

        var isFirst = true;
        foreach (ResolvedService? service in blueprint.Services)
        {
            string ifPrefix = isFirst ? "if" : "else if";
            isFirst = false;

            sb.AppendLine($"{innerIndent}{ifPrefix} (typeof(TService) == typeof({service.ServiceTypeFullName}))");
            sb.AppendLine($"{innerIndent}{{");

            switch (service.Lifetime)
            {
                case ServiceLifetime.Singleton:
                    sb.AppendLine($"{tripleIndent}return _root.Resolve<TService>();");
                    break;

                case ServiceLifetime.ScopedToTag:
                    GenerateScopedToTagLogic(sb, service, indentLevel + 2);
                    break;

                case ServiceLifetime.Scoped:
                    GenerateScopedLogic(sb, service, indentLevel + 2);
                    break;

                case ServiceLifetime.Transient:
                    GenerateTransientLogic(sb, service, indentLevel + 2);
                    break;
            }

            sb.AppendLine($"{innerIndent}}}");
        }

        // Fallback for services not found (e.g., from a misconfigured module)
        sb.AppendLine(
            $"{innerIndent}throw new global::System.InvalidOperationException($\"Service of type {{typeof(TService).FullName}} is not registered.\");"
        );
        sb.AppendLine($"{indent}}}");
        sb.AppendLine();
    }

    private static void GenerateScopedLogic(StringBuilder sb, ResolvedService service, int indentLevel)
    {
        var indent = new string(' ', indentLevel * 4);
        sb.AppendLine($"{indent}if (_scopedInstances.TryGetValue(typeof(TService), out var inst)) return (TService)inst;");
        sb.AppendLine($"{indent}var newInst = {GenerateInstanceCreation(service, "this")};");
        sb.AppendLine($"{indent}_scopedInstances.Add(typeof(TService), newInst);");
        sb.AppendLine($"{indent}return (TService)(object)newInst;");
    }

    private static void GenerateTransientLogic(StringBuilder sb, ResolvedService service, int indentLevel)
    {
        var indent = new string(' ', indentLevel * 4);
        sb.AppendLine($"{indent}var transientInstance = {GenerateInstanceCreation(service, "this")};");
        sb.AppendLine($"{indent}if (transientInstance is global::System.IDisposable d) {{ _disposables.Add(d); }}");
        sb.AppendLine($"{indent}return (TService)(object)transientInstance;");
    }

    private static void GenerateScopedToTagLogic(StringBuilder sb, ResolvedService service, int indentLevel)
    {
        var indent = new string(' ', indentLevel * 4);
        var innerIndent = new string(' ', (indentLevel + 1) * 4);

        sb.AppendLine($"{indent}if (this._tag == \"{service.SourceRegistration.ScopeTag}\")");
        sb.AppendLine($"{indent}{{");
        sb.AppendLine($"{innerIndent}if (_scopedInstances.TryGetValue(typeof(TService), out var inst)) return (TService)inst;");
        sb.AppendLine($"{innerIndent}var newInst = {GenerateInstanceCreation(service, "this")};");
        sb.AppendLine($"{innerIndent}_scopedInstances.Add(typeof(TService), newInst);");
        sb.AppendLine($"{innerIndent}return (TService)(object)newInst;");
        sb.AppendLine($"{indent}}}");
        sb.AppendLine($"{indent}else");
        sb.AppendLine($"{indent}{{");
        sb.AppendLine($"{innerIndent}return _parentResolver.Resolve<TService>();");
        sb.AppendLine($"{indent}}}");
    }

    private static void AddScopeDisposeMethod(StringBuilder sb, int indentLevel)
    {
        var indent = new string(' ', indentLevel * 4);
        var innerIndent = new string(' ', (indentLevel + 1) * 4);

        sb.AppendLine($"{indent}public void Dispose()");
        sb.AppendLine($"{indent}{{");
        sb.AppendLine($"{innerIndent}if (_isDisposed) return;");
        sb.AppendLine($"{innerIndent}_isDisposed = true;");
        // Dispose transients first, then scoped instances
        sb.AppendLine($"{innerIndent}foreach (var disposable in _disposables) {{ disposable.Dispose(); }}");
        sb.AppendLine($"{innerIndent}foreach (var instance in _scopedInstances.Values) {{ (instance as global::System.IDisposable)?.Dispose(); }}");
        sb.AppendLine($"{indent}}}");
    }

    private static void GenerateDisposeMethod(StringBuilder sb, ContainerBlueprint blueprint, int indentLevel)
    {
        var indent = new string(' ', indentLevel * 4);
        var innerIndent = new string(' ', (indentLevel + 1) * 4);

        sb.AppendLine($"{indent}public void Dispose()");
        sb.AppendLine($"{indent}{{");
        sb.AppendLine($"{innerIndent}if (_isDisposed) return;");
        sb.AppendLine($"{innerIndent}_isDisposed = true;");
        sb.AppendLine();

        IEnumerable<ResolvedService> disposableSingletons =
            blueprint.Services.Where(s =>
                                         s.Lifetime == ServiceLifetime.Singleton && s.SourceRegistration.IsDisposable
            );
        foreach (ResolvedService? service in disposableSingletons)
        {
            string fieldName = GetSingletonFieldName(service.ServiceTypeFullName);

            sb.AppendLine($"{innerIndent}if ({fieldName}.IsValueCreated)");
            sb.AppendLine($"{innerIndent}{{");
            sb.AppendLine($"{innerIndent}    (({fieldName}.Value as global::System.IDisposable))?.Dispose();");
            sb.AppendLine($"{innerIndent}}}");
        }

        sb.AppendLine($"{indent}}}");
        sb.AppendLine();
    }

    // --- Helper Methods for Naming ---

    private static string SanitizeTypeName(string typeName)
    {
        // This replaces characters that are invalid in an identifier with underscores.
        // e.g., "global::My.Generic<System.Int32>" becomes "global__My_Generic_System_Int32_"
        return typeName.Replace("::", "__")
                       .Replace(".", "_")
                       .Replace("<", "_")
                       .Replace(">", "_")
                       .Replace(",", "_");
    }

    private static string GetSingletonFieldName(string serviceType)
    {
        return $"_singleton_{SanitizeTypeName(serviceType)}";
    }

    private static string GetFactoryMethodName(string serviceType)
    {
        return $"Create_{SanitizeTypeName(serviceType)}";
    }

    private static string GenerateInstanceCreation(ResolvedService service, string resolverContext)
    {
        // The main logic for creating the base service instance.
        string baseImplType = service.SourceRegistration.ImplementationType.FullName;
        string baseArgs = GenerateArgumentList(
            service.SelectedConstructor.ParameterTypeFullNames,
            resolverContext
        );
        var currentCall = $"new {baseImplType}({baseArgs})";

        // Now, apply decorators. The logic is cleaner because we reuse the helper.
        foreach (ResolvedDecorator decorator in service.Decorators)
        {
            string decoratorTypeName = decorator.SourceDecorator.FullName;
            string decoratorArgs = GenerateArgumentList(
                decorator.SelectedConstructor.ParameterTypeFullNames,
                resolverContext,
                service.ServiceTypeFullName, // The service being decorated
                currentCall // The instance to pass for the decorated service
            );
            currentCall = $"new {decoratorTypeName}({decoratorArgs})";
        }

        return currentCall;
    }

    /// <summary>
    ///     A helper method that generates the comma-separated argument list for a constructor call.
    /// </summary>
    private static string GenerateArgumentList(
        EquatableArray<string> parameterTypes,
        string resolverContext,
        string? decoratedServiceType = null,
        string? decoratedServiceInstance = null)
    {
        IEnumerable<string> arguments = parameterTypes
                                        .GetArray()!
                                        .Select(paramTypeName =>
                                            {
                                                if (paramTypeName == Constants.ResolverInterfaceName)
                                                {
                                                    // This parameter is the IResolver interface.
                                                    return resolverContext;
                                                }

                                                if (paramTypeName == Constants.ContainerInterfaceName)
                                                {
                                                    // Only the root container can provide IContainer. resolverContext must be the root.
                                                    return "this";
                                                }

                                                if (decoratedServiceType != null && paramTypeName == decoratedServiceType)
                                                {
                                                    // This parameter is for the service being decorated.
                                                    return decoratedServiceInstance!;
                                                }

                                                // Otherwise, it's a standard service that needs to be resolved.
                                                return $"{resolverContext}.Resolve<{paramTypeName}>()";
                                            }
                                        );

        return string.Join(", ", arguments);
    }
}