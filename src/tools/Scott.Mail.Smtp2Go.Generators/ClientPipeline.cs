using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Scott.Mail.Smtp2Go.Generators;

/// <summary>A parameter of a generated method, as declared on the interface.</summary>
internal sealed record ParameterSpec(string Type, string Name, string Default);

/// <summary>
/// An interface method the class does not implement and whose shape the generator can fill in. <see cref="RequestType"/> is null for the
/// shape without a request body; <see cref="Path"/> comes from the <c>[Smtp2GoEndpoint]</c> on the request type or the response data type.
/// </summary>
internal sealed record PassThroughMethod(string Name, string ReturnType, string? RequestType, string ResponseDataType, string Path, EquatableArray<ParameterSpec> Parameters);

/// <summary>A class marked <c>[GeneratedClient]</c>.</summary>
internal sealed record ClientTarget(string TypeName, string Namespace, bool IsPartial, string? ConnectionName, EquatableArray<PassThroughMethod> Methods, LocationInfo? Location);

/// <summary>
/// Generates the pass-through methods of the family clients: for every unimplemented interface method shaped
/// <c>Task&lt;ApiResponse&lt;TData&gt;&gt; M([TRequest request,] RequestOptions? options, CancellationToken cancellationToken)</c>,
/// a null check and a <c>SendAsync</c> on the connection against the endpoint named by the model's <c>[Smtp2GoEndpoint]</c>.
/// </summary>
internal static class ClientPipeline
{
    private const string EndpointAttribute = "Scott.Mail.Smtp2Go.Transport.Smtp2GoEndpointAttribute";
    private const string ConnectionTypeName = "Smtp2GoConnection";
    private const string JsonElement = "global::System.Text.Json.JsonElement";

    public static void Register(IncrementalGeneratorInitializationContext context, IncrementalValueProvider<EndpointSnapshot> snapshot)
    {
        IncrementalValuesProvider<ClientTarget> targets = context.SyntaxProvider.ForAttributeWithMetadataName(
            GenerationAttributes.GeneratedClient,
            static (node, _) => node is ClassDeclarationSyntax,
            static (context, _) => Describe(context));

        context.RegisterSourceOutput(targets.Combine(snapshot), static (context, pair) => Emit(context, pair.Left, pair.Right));
    }

    private static ClientTarget Describe(GeneratorAttributeSyntaxContext context)
    {
        INamedTypeSymbol type = (INamedTypeSymbol)context.TargetSymbol;
        bool isPartial = ((ClassDeclarationSyntax)context.TargetNode).Modifiers.Any(m => m.ValueText == "partial");
        string? connection = null;
        foreach (IMethodSymbol constructor in type.InstanceConstructors)
        {
            foreach (IParameterSymbol parameter in constructor.Parameters)
            {
                if (parameter.Type.Name == ConnectionTypeName)
                {
                    connection = parameter.Name;
                    break;
                }
            }
        }

        ImmutableArray<PassThroughMethod>.Builder methods = ImmutableArray.CreateBuilder<PassThroughMethod>();
        foreach (INamedTypeSymbol @interface in type.AllInterfaces)
        {
            foreach (ISymbol member in @interface.GetMembers())
            {
                if (member is IMethodSymbol { MethodKind: MethodKind.Ordinary } method
                    && type.FindImplementationForInterfaceMember(method) is null
                    && Describe(method) is { } passThrough)
                {
                    methods.Add(passThrough);
                }
            }
        }

        return new ClientTarget(
            type.Name,
            type.ContainingNamespace.IsGlobalNamespace ? string.Empty : type.ContainingNamespace.ToDisplayString(),
            isPartial,
            connection,
            new EquatableArray<PassThroughMethod>(methods.ToImmutable()),
            LocationInfo.From(context.TargetNode));
    }

    /// <summary>The method as a pass-through, or null when its shape is not one the generator fills in.</summary>
    private static PassThroughMethod? Describe(IMethodSymbol method)
    {
        if (method.ReturnType is not INamedTypeSymbol { Name: "Task", TypeArguments.Length: 1 } task
            || task.TypeArguments[0] is not INamedTypeSymbol { Name: "ApiResponse", TypeArguments.Length: 1 } response)
        {
            return null;
        }

        ImmutableArray<IParameterSymbol> parameters = method.Parameters;
        if (parameters.Length is not (2 or 3)
            || parameters[parameters.Length - 2].Type.Name != "RequestOptions"
            || parameters[parameters.Length - 1].Type.Name != "CancellationToken")
        {
            return null;
        }

        ITypeSymbol data = response.TypeArguments[0];
        string? requestType = null;
        string? path;
        if (parameters.Length == 3)
        {
            if (parameters[0].Type is not INamedTypeSymbol request)
            {
                return null;
            }

            requestType = request.ToDisplayString(SourceWriter.TypeFormat);
            path = EndpointPath(request);
        }
        else
        {
            path = EndpointPath(ElementTypeOrSelf(data));
        }

        if (path is null)
        {
            return null;
        }

        ImmutableArray<ParameterSpec>.Builder specs = ImmutableArray.CreateBuilder<ParameterSpec>(parameters.Length);
        foreach (IParameterSymbol parameter in parameters)
        {
            specs.Add(new ParameterSpec(parameter.Type.ToDisplayString(SourceWriter.TypeFormat), parameter.Name, SourceWriter.DefaultValue(parameter)));
        }

        return new PassThroughMethod(
            method.Name,
            method.ReturnType.ToDisplayString(SourceWriter.TypeFormat),
            requestType,
            data.ToDisplayString(SourceWriter.TypeFormat),
            path,
            new EquatableArray<ParameterSpec>(specs.ToImmutable()));
    }

    private static string? EndpointPath(ITypeSymbol type)
    {
        foreach (AttributeData attribute in type.GetAttributes())
        {
            if (attribute.AttributeClass?.ToDisplayString() == EndpointAttribute
                && attribute.ConstructorArguments.Length == 1
                && attribute.ConstructorArguments[0].Value is string path)
            {
                return path.Trim().TrimStart('/');
            }
        }

        return null;
    }

    /// <summary>The element type of a collection (<c>IReadOnlyList&lt;T&gt;</c> and the like), otherwise the type itself.</summary>
    private static ITypeSymbol ElementTypeOrSelf(ITypeSymbol type)
    {
        if (type.SpecialType == SpecialType.System_String)
        {
            return type;
        }

        if (type is INamedTypeSymbol { IsGenericType: true, TypeArguments.Length: 1 } named)
        {
            if (named.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T)
            {
                return named.TypeArguments[0];
            }

            foreach (INamedTypeSymbol @interface in named.AllInterfaces)
            {
                if (@interface.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T)
                {
                    return @interface.TypeArguments[0];
                }
            }
        }

        return type;
    }

    private static void Emit(SourceProductionContext context, ClientTarget target, EndpointSnapshot snapshot)
    {
        if (!target.IsPartial)
        {
            context.ReportDiagnostic(Diagnostic.Create(Diagnostics.NotPartial, target.Location?.ToLocation(), target.TypeName));
            return;
        }

        if (target.ConnectionName is null)
        {
            context.ReportDiagnostic(Diagnostic.Create(Diagnostics.NoConnection, target.Location?.ToLocation(), target.TypeName));
            return;
        }

        if (!snapshot.Found)
        {
            context.ReportDiagnostic(Diagnostic.Create(Diagnostics.SnapshotMissing, target.Location?.ToLocation(), EndpointSnapshot.FileName));
            return;
        }

        if (snapshot.Error is not null)
        {
            context.ReportDiagnostic(Diagnostic.Create(Diagnostics.SnapshotUnreadable, target.Location?.ToLocation(), EndpointSnapshot.FileName, snapshot.Error));
            return;
        }

        // Deprecated endpoints are implemented on purpose; their request types are [Obsolete].
        StringBuilder builder = SourceWriter.Header("CS0618");
        if (target.Namespace.Length > 0)
        {
            builder.Append("namespace ").Append(target.Namespace).AppendLine(";").AppendLine();
        }

        builder.Append("partial class ").AppendLine(target.TypeName);
        builder.AppendLine("{");
        bool first = true;
        foreach (PassThroughMethod method in target.Methods)
        {
            ImmutableArray<string> documented = snapshot.MethodsFor(method.Path);
            if (documented.Length > 1)
            {
                // POST and PATCH on one path: the attribute does not say which one the request type is for, so the method stays hand-written.
                continue;
            }

            if (!first)
            {
                builder.AppendLine();
            }

            first = false;
            string field = "s_" + char.ToLowerInvariant(method.Name[0]) + method.Name.Substring(1) + "Endpoint";
            string lookup = documented.Length == 1
                ? "global::Scott.Mail.Smtp2Go.Transport.EndpointTable.Get(" + SourceWriter.Literal(method.Path) + ", " + SourceWriter.HttpMethodExpression(documented[0]) + ")"
                : "global::Scott.Mail.Smtp2Go.Transport.EndpointTable.Get(" + SourceWriter.Literal(method.Path) + ")";
            builder.Append("    private static readonly global::Scott.Mail.Smtp2Go.Transport.Endpoint ").Append(field).Append(" = ").Append(lookup).AppendLine(";");
            builder.AppendLine();
            builder.Append("    ").AppendLine(SourceWriter.GeneratedCodeAttribute);
            builder.Append("    public ").Append(method.ReturnType).Append(' ').Append(method.Name).Append('(');
            for (int i = 0; i < method.Parameters.Count; i++)
            {
                ParameterSpec parameter = method.Parameters[i];
                if (i > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(parameter.Type).Append(' ').Append(parameter.Name).Append(parameter.Default);
            }

            builder.AppendLine(")");
            builder.AppendLine("    {");
            string options = method.Parameters[method.Parameters.Count - 2].Name;
            string cancellation = method.Parameters[method.Parameters.Count - 1].Name;
            if (method.RequestType is not null)
            {
                string request = method.Parameters[0].Name;
                builder.Append("        global::Scott.Mail.Smtp2Go.Argument.ThrowIfNull(").Append(request).AppendLine(");");
                builder.Append("        return ").Append(target.ConnectionName).Append(".SendAsync<").Append(method.RequestType).Append(", ").Append(method.ResponseDataType).Append(">(")
                    .Append(field).Append(", ").Append(request).Append(", ").Append(options).Append(", ").Append(cancellation).AppendLine(");");
            }
            else
            {
                builder.Append("        return ").Append(target.ConnectionName).Append(".SendAsync<").Append(JsonElement).Append(", ").Append(method.ResponseDataType).Append(">(")
                    .Append(field).Append(", default, ").Append(options).Append(", ").Append(cancellation).AppendLine(");");
            }

            builder.AppendLine("    }");
        }

        builder.AppendLine("}");
        context.AddSource(target.TypeName + ".PassThrough.g.cs", SourceText.From(builder.ToString(), Encoding.UTF8));
    }
}
