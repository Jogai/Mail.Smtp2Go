using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Scott.Mail.Smtp2Go.Generators;

/// <summary>One member of a wire-named enum.</summary>
internal sealed record EnumMemberSpec(string Name, string WireName);

/// <summary>An enum read with <c>TolerantEnumConverter&lt;T&gt;</c>: its fully qualified name and members.</summary>
internal sealed record EnumSpec(string TypeName, EquatableArray<EnumMemberSpec> Members);

/// <summary>The partial class marked <c>[GeneratedEnumWireNames]</c>.</summary>
internal sealed record EnumRegistryTarget(string TypeName, string Namespace, bool IsPartial, LocationInfo? Location);

/// <summary>
/// Generates <c>EnumWireNames.Get&lt;TEnum&gt;()</c>: a static member table for every enum in the compilation that carries
/// <c>[JsonConverter(typeof(TolerantEnumConverter&lt;Self&gt;))]</c>, with the wire name <c>[JsonStringEnumMemberName]</c> states or the
/// snake_case form the serializer context would apply. Enums from other assemblies (the tests declare some) are not here and fall back to reflection.
/// </summary>
internal static class EnumWireNamesPipeline
{
    private const string JsonConverterAttribute = "System.Text.Json.Serialization.JsonConverterAttribute";
    private const string MemberNameAttribute = "System.Text.Json.Serialization.JsonStringEnumMemberNameAttribute";
    private const string ConverterName = "TolerantEnumConverter";
    private const string MemberType = "global::Scott.Mail.Smtp2Go.Json.EnumMember";

    public static void Register(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<EnumRegistryTarget> targets = context.SyntaxProvider.ForAttributeWithMetadataName(
            GenerationAttributes.GeneratedEnumWireNames,
            static (node, _) => node is ClassDeclarationSyntax,
            static (context, _) => new EnumRegistryTarget(
                context.TargetSymbol.Name,
                context.TargetSymbol.ContainingNamespace.IsGlobalNamespace ? string.Empty : context.TargetSymbol.ContainingNamespace.ToDisplayString(),
                ((ClassDeclarationSyntax)context.TargetNode).Modifiers.Any(m => m.ValueText == "partial"),
                LocationInfo.From(context.TargetNode)));

        IncrementalValueProvider<ImmutableArray<EnumSpec>> enums = context.SyntaxProvider.CreateSyntaxProvider(
            static (node, _) => node is EnumDeclarationSyntax { AttributeLists.Count: > 0 },
            static (context, cancellationToken) => Describe(context, cancellationToken))
            .Where(static spec => spec is not null)
            .Select(static (spec, _) => spec!)
            .Collect();

        context.RegisterSourceOutput(targets.Combine(enums), static (context, pair) => Emit(context, pair.Left, pair.Right));
    }

    private static EnumSpec? Describe(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.SemanticModel.GetDeclaredSymbol((EnumDeclarationSyntax)context.Node, cancellationToken) is not INamedTypeSymbol enumType || !IsTolerant(enumType))
        {
            return null;
        }

        ImmutableArray<EnumMemberSpec>.Builder members = ImmutableArray.CreateBuilder<EnumMemberSpec>();
        foreach (ISymbol member in enumType.GetMembers())
        {
            if (member is not IFieldSymbol { IsConst: true } field)
            {
                continue;
            }

            string? explicitName = null;
            foreach (AttributeData attribute in field.GetAttributes())
            {
                if (attribute.AttributeClass?.ToDisplayString() == MemberNameAttribute && attribute.ConstructorArguments.Length == 1 && attribute.ConstructorArguments[0].Value is string name)
                {
                    explicitName = name;
                }
            }

            members.Add(new EnumMemberSpec(field.Name, explicitName ?? SnakeCase.Convert(field.Name)));
        }

        return new EnumSpec(enumType.ToDisplayString(SourceWriter.TypeFormat), new EquatableArray<EnumMemberSpec>(members.ToImmutable()));
    }

    /// <summary>Whether the enum is annotated <c>[JsonConverter(typeof(TolerantEnumConverter&lt;Self&gt;))]</c>.</summary>
    private static bool IsTolerant(INamedTypeSymbol enumType)
    {
        foreach (AttributeData attribute in enumType.GetAttributes())
        {
            if (attribute.AttributeClass?.ToDisplayString() == JsonConverterAttribute
                && attribute.ConstructorArguments.Length == 1
                && attribute.ConstructorArguments[0].Value is INamedTypeSymbol { Name: ConverterName, TypeArguments.Length: 1 } converter
                && SymbolEqualityComparer.Default.Equals(converter.TypeArguments[0], enumType))
            {
                return true;
            }
        }

        return false;
    }

    private static void Emit(SourceProductionContext context, EnumRegistryTarget target, ImmutableArray<EnumSpec> enums)
    {
        if (!target.IsPartial)
        {
            context.ReportDiagnostic(Diagnostic.Create(Diagnostics.NotPartial, target.Location?.ToLocation(), target.TypeName));
            return;
        }

        ImmutableArray<EnumSpec> ordered = enums.Sort(static (a, b) => string.CompareOrdinal(a.TypeName, b.TypeName));
        StringBuilder builder = SourceWriter.Header();
        if (target.Namespace.Length > 0)
        {
            builder.Append("namespace ").Append(target.Namespace).AppendLine(";").AppendLine();
        }

        builder.Append("partial class ").AppendLine(target.TypeName);
        builder.AppendLine("{");
        foreach (EnumSpec spec in ordered)
        {
            builder.Append("    private static readonly ").Append(MemberType).Append('<').Append(spec.TypeName).Append(">[] ").Append(FieldName(spec)).AppendLine(" =");
            builder.AppendLine("    [");
            foreach (EnumMemberSpec member in spec.Members)
            {
                builder.Append("        new(").Append(spec.TypeName).Append('.').Append(member.Name).Append(", ").Append(SourceWriter.Literal(member.Name)).Append(", ").Append(SourceWriter.Literal(member.WireName)).AppendLine("),");
            }

            builder.AppendLine("    ];");
            builder.AppendLine();
        }

        builder.Append("    /// <summary>The members of <typeparamref name=\"TEnum\"/> with their wire names, or null when the enum is not one of the ").Append(ordered.Length).AppendLine(" generated here.</summary>");
        builder.Append("    ").AppendLine(SourceWriter.GeneratedCodeAttribute);
        builder.Append("    public static ").Append(MemberType).AppendLine("<TEnum>[]? Get<TEnum>()");
        builder.AppendLine("        where TEnum : struct, global::System.Enum");
        builder.AppendLine("    {");
        foreach (EnumSpec spec in ordered)
        {
            builder.Append("        if (typeof(TEnum) == typeof(").Append(spec.TypeName).AppendLine("))");
            builder.AppendLine("        {");
            builder.Append("            return (").Append(MemberType).Append("<TEnum>[])(object)").Append(FieldName(spec)).AppendLine(";");
            builder.AppendLine("        }");
            builder.AppendLine();
        }

        builder.AppendLine("        return null;");
        builder.AppendLine("    }");
        builder.AppendLine("}");
        context.AddSource(target.TypeName + ".g.cs", SourceText.From(builder.ToString(), Encoding.UTF8));
    }

    private static string FieldName(EnumSpec spec)
    {
        int dot = spec.TypeName.LastIndexOf('.');
        return "s_" + (dot < 0 ? spec.TypeName : spec.TypeName.Substring(dot + 1));
    }
}
