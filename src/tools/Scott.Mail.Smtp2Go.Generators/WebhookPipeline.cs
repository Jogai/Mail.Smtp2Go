using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Scott.Mail.Smtp2Go.Generators;

/// <summary>A concrete event type and its base chain up to (and including) the base the partial method takes, most derived first.</summary>
internal sealed record EventType(string TypeName, EquatableArray<string> Chain);

/// <summary>A partial method marked for generation and the type that declares it.</summary>
internal sealed record WebhookMethod(
    string TypeName,
    string Namespace,
    bool TypeIsPartial,
    string Accessibility,
    bool IsStatic,
    string ReturnType,
    string Name,
    EquatableArray<ParameterSpec> Parameters,
    EquatableArray<EventType> Events,
    LocationInfo? Location);

/// <summary>
/// Generates the two type switches over the closed webhook event hierarchy: the handler dispatch in the ASP.NET Core package
/// (<c>[GeneratedWebhookDispatch]</c>) and the record update in the parser (<c>[GeneratedWebhookUpdate]</c>). Both are derived from the
/// types deriving from the method's first parameter type in that type's assembly, so adding an event record updates both.
/// </summary>
internal static class WebhookPipeline
{
    public static void Register(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<WebhookMethod?> dispatch = context.SyntaxProvider.ForAttributeWithMetadataName(
            GenerationAttributes.GeneratedWebhookDispatch,
            static (node, _) => node is MethodDeclarationSyntax,
            static (context, _) => Describe(context, requireProperties: false));
        context.RegisterSourceOutput(dispatch, static (context, method) => Emit(context, method, EmitDispatch, ".Dispatch"));

        IncrementalValuesProvider<WebhookMethod?> update = context.SyntaxProvider.ForAttributeWithMetadataName(
            GenerationAttributes.GeneratedWebhookUpdate,
            static (node, _) => node is MethodDeclarationSyntax,
            static (context, _) => Describe(context, requireProperties: true));
        context.RegisterSourceOutput(update, static (context, method) => Emit(context, method, EmitUpdate, ".Update"));
    }

    private static WebhookMethod? Describe(GeneratorAttributeSyntaxContext context, bool requireProperties)
    {
        if (context.TargetSymbol is not IMethodSymbol { Parameters.Length: >= 1 } method || method.Parameters[0].Type is not INamedTypeSymbol baseType)
        {
            return null;
        }

        INamedTypeSymbol type = method.ContainingType;
        bool typeIsPartial = false;
        foreach (SyntaxReference reference in type.DeclaringSyntaxReferences)
        {
            if (reference.GetSyntax() is TypeDeclarationSyntax declaration && declaration.Modifiers.Any(m => m.ValueText == "partial"))
            {
                typeIsPartial = true;
            }
        }

        ImmutableArray<ParameterSpec>.Builder parameters = ImmutableArray.CreateBuilder<ParameterSpec>(method.Parameters.Length);
        foreach (IParameterSymbol parameter in method.Parameters)
        {
            parameters.Add(new ParameterSpec(parameter.Type.ToDisplayString(SourceWriter.TypeFormat), parameter.Name, string.Empty));
        }

        // The property each trailing parameter sets, for the update shape: extra -> Extra.
        List<string> properties = [];
        for (int i = 1; i < method.Parameters.Length; i++)
        {
            string name = method.Parameters[i].Name;
            properties.Add(char.ToUpperInvariant(name[0]) + name.Substring(1));
        }

        List<(INamedTypeSymbol Type, int Depth)> concrete = [];
        Collect(baseType.ContainingAssembly.GlobalNamespace, baseType, concrete);
        concrete.Sort((a, b) => b.Depth != a.Depth ? b.Depth.CompareTo(a.Depth) : string.CompareOrdinal(a.Type.Name, b.Type.Name));

        ImmutableArray<EventType>.Builder events = ImmutableArray.CreateBuilder<EventType>();
        foreach ((INamedTypeSymbol eventType, _) in concrete)
        {
            if (requireProperties && !HasSettableProperties(eventType, properties))
            {
                continue;
            }

            ImmutableArray<string>.Builder chain = ImmutableArray.CreateBuilder<string>();
            for (INamedTypeSymbol? current = eventType; current is not null; current = current.BaseType)
            {
                chain.Add(current.ToDisplayString(SourceWriter.TypeFormat));
                if (SymbolEqualityComparer.Default.Equals(current, baseType))
                {
                    break;
                }
            }

            events.Add(new EventType(eventType.ToDisplayString(SourceWriter.TypeFormat), new EquatableArray<string>(chain.ToImmutable())));
        }

        return new WebhookMethod(
            type.Name,
            type.ContainingNamespace.IsGlobalNamespace ? string.Empty : type.ContainingNamespace.ToDisplayString(),
            typeIsPartial,
            SyntaxFacts.GetText(method.DeclaredAccessibility),
            method.IsStatic,
            method.ReturnType.ToDisplayString(SourceWriter.TypeFormat),
            method.Name,
            new EquatableArray<ParameterSpec>(parameters.ToImmutable()),
            new EquatableArray<EventType>(events.ToImmutable()),
            LocationInfo.From(context.TargetNode));
    }

    /// <summary>Every non-abstract type in <paramref name="scope"/> that derives from <paramref name="baseType"/>, with its distance from it.</summary>
    private static void Collect(INamespaceSymbol scope, INamedTypeSymbol baseType, List<(INamedTypeSymbol, int)> result)
    {
        foreach (INamespaceOrTypeSymbol member in scope.GetMembers())
        {
            if (member is INamespaceSymbol nested)
            {
                Collect(nested, baseType, result);
            }
            else if (member is INamedTypeSymbol { IsAbstract: false, TypeKind: TypeKind.Class } type)
            {
                int depth = 0;
                for (INamedTypeSymbol? current = type.BaseType; current is not null; current = current.BaseType)
                {
                    depth++;
                    if (SymbolEqualityComparer.Default.Equals(current, baseType))
                    {
                        result.Add((type, depth));
                        break;
                    }
                }
            }
        }
    }

    private static bool HasSettableProperties(INamedTypeSymbol type, List<string> properties)
    {
        foreach (string name in properties)
        {
            bool found = false;
            for (INamedTypeSymbol? current = type; current is not null && !found; current = current.BaseType)
            {
                foreach (ISymbol member in current.GetMembers(name))
                {
                    if (member is IPropertySymbol { SetMethod: not null })
                    {
                        found = true;
                    }
                }
            }

            if (!found)
            {
                return false;
            }
        }

        return true;
    }

    private static void Emit(SourceProductionContext context, WebhookMethod? method, Action<StringBuilder, WebhookMethod> body, string suffix)
    {
        if (method is null)
        {
            return;
        }

        if (!method.TypeIsPartial)
        {
            context.ReportDiagnostic(Diagnostic.Create(Diagnostics.NotPartial, method.Location?.ToLocation(), method.TypeName));
            return;
        }

        StringBuilder builder = SourceWriter.Header();
        if (method.Namespace.Length > 0)
        {
            builder.Append("namespace ").Append(method.Namespace).AppendLine(";").AppendLine();
        }

        builder.Append("partial class ").AppendLine(method.TypeName);
        builder.AppendLine("{");
        builder.Append("    ").AppendLine(SourceWriter.GeneratedCodeAttribute);
        builder.Append("    ").Append(method.Accessibility).Append(method.IsStatic ? " static" : string.Empty).Append(" partial ").Append(method.ReturnType).Append(' ').Append(method.Name).Append('(');
        for (int i = 0; i < method.Parameters.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(", ");
            }

            builder.Append(method.Parameters[i].Type).Append(' ').Append(method.Parameters[i].Name);
        }

        builder.AppendLine(")");
        builder.AppendLine("    {");
        body(builder, method);
        builder.AppendLine("    }");
        builder.AppendLine("}");
        context.AddSource(method.TypeName + suffix + ".g.cs", SourceText.From(builder.ToString(), Encoding.UTF8));
    }

    /// <summary>Most derived first, so a subtype's handlers run before its base's and the base pattern never shadows it.</summary>
    private static void EmitDispatch(StringBuilder builder, WebhookMethod method)
    {
        string subject = method.Parameters[0].Name;
        string cancellation = method.Parameters[method.Parameters.Count - 1].Name;
        builder.Append("        return ").Append(subject).AppendLine(" switch");
        builder.AppendLine("        {");
        foreach (EventType eventType in method.Events)
        {
            builder.Append("            ").Append(eventType.TypeName).Append(" e => RunAsync(").Append(cancellation);
            foreach (string link in eventType.Chain)
            {
                builder.Append(", Handlers<").Append(link).Append(">(e)");
            }

            builder.AppendLine("),");
        }

        builder.Append("            _ => RunAsync(").Append(cancellation).Append(", Handlers<").Append(method.Parameters[0].Type).Append(">(").Append(subject).AppendLine(")),");
        builder.AppendLine("        };");
    }

    private static void EmitUpdate(StringBuilder builder, WebhookMethod method)
    {
        string subject = method.Parameters[0].Name;
        builder.Append("        return ").Append(subject).AppendLine(" switch");
        builder.AppendLine("        {");
        foreach (EventType eventType in method.Events)
        {
            builder.Append("            ").Append(eventType.TypeName).Append(" value => value with { ");
            for (int i = 1; i < method.Parameters.Count; i++)
            {
                string name = method.Parameters[i].Name;
                if (i > 1)
                {
                    builder.Append(", ");
                }

                builder.Append(char.ToUpperInvariant(name[0])).Append(name.Substring(1)).Append(" = ").Append(name);
            }

            builder.AppendLine(" },");
        }

        builder.Append("            _ => ").Append(subject).AppendLine(",");
        builder.AppendLine("        };");
    }
}
