using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Scott.Mail.Smtp2Go.Generators;

/// <summary>One <c>[EndpointTraits]</c> attribute: the pattern and the traits it sets (null means "leave as is").</summary>
internal sealed record TraitSpec(string Pattern, bool? Idempotent, string? RateLimitMember, long? MaxBodyBytes, LocationInfo? Location);

/// <summary>The partial <c>EndpointTable</c> class and its trait overrides.</summary>
internal sealed record TableTarget(string TypeName, string Namespace, bool IsPartial, EquatableArray<TraitSpec> Traits, LocationInfo? Location);

/// <summary>
/// Generates <c>EndpointTable.Seed()</c>: one descriptor per operation in <c>endpoints.json</c> (path, HTTP method and <c>subaccount_id</c>
/// support come from the snapshot), with idempotency, rate-limit class and body limit from the <c>[EndpointTraits]</c> attributes on the class.
/// </summary>
internal static class EndpointTablePipeline
{
    private const string EndpointType = "global::Scott.Mail.Smtp2Go.Transport.Endpoint";
    private const string RateLimitType = "global::Scott.Mail.Smtp2Go.Transport.RateLimitClass";
    private const long DefaultMaxBodyBytes = 1L * 1024 * 1024;

    public static void Register(IncrementalGeneratorInitializationContext context, IncrementalValueProvider<EndpointSnapshot> snapshot)
    {
        IncrementalValuesProvider<TableTarget> targets = context.SyntaxProvider.ForAttributeWithMetadataName(
            GenerationAttributes.EndpointTraits,
            static (node, _) => node is ClassDeclarationSyntax,
            static (context, _) => Describe(context));

        context.RegisterSourceOutput(targets.Combine(snapshot), static (context, pair) => Emit(context, pair.Left, pair.Right));
    }

    private static TableTarget Describe(GeneratorAttributeSyntaxContext context)
    {
        INamedTypeSymbol type = (INamedTypeSymbol)context.TargetSymbol;
        bool isPartial = ((ClassDeclarationSyntax)context.TargetNode).Modifiers.Any(m => m.ValueText == "partial");

        ImmutableArray<TraitSpec>.Builder traits = ImmutableArray.CreateBuilder<TraitSpec>();
        foreach (AttributeData attribute in context.Attributes)
        {
            if (attribute.ConstructorArguments.Length != 1 || attribute.ConstructorArguments[0].Value is not string pattern)
            {
                continue;
            }

            bool? idempotent = null;
            string? rateLimit = null;
            long? maxBody = null;
            foreach (KeyValuePair<string, TypedConstant> named in attribute.NamedArguments)
            {
                switch (named.Key)
                {
                    case "Idempotent":
                        idempotent = named.Value.Value as bool?;
                        break;
                    case "RateLimit":
                        rateLimit = EnumMemberName(named.Value);
                        break;
                    case "MaxBodyBytes":
                        maxBody = named.Value.Value is long bytes ? bytes : null;
                        break;
                }
            }

            traits.Add(new TraitSpec(pattern.Trim().TrimStart('/'), idempotent, rateLimit, maxBody, LocationInfo.From(attribute.ApplicationSyntaxReference?.GetSyntax())));
        }

        return new TableTarget(type.Name, type.ContainingNamespace.IsGlobalNamespace ? string.Empty : type.ContainingNamespace.ToDisplayString(), isPartial, new EquatableArray<TraitSpec>(traits.ToImmutable()), LocationInfo.From(context.TargetNode));
    }

    /// <summary>The declared member name for an enum-typed attribute argument, or the numeric value when no member has it.</summary>
    private static string? EnumMemberName(TypedConstant constant)
    {
        if (constant.Type is not INamedTypeSymbol { TypeKind: TypeKind.Enum } enumType || constant.Value is null)
        {
            return null;
        }

        foreach (ISymbol member in enumType.GetMembers())
        {
            if (member is IFieldSymbol { HasConstantValue: true } field && Equals(field.ConstantValue, constant.Value))
            {
                return field.Name;
            }
        }

        return Convert.ToString(constant.Value, System.Globalization.CultureInfo.InvariantCulture);
    }

    private static void Emit(SourceProductionContext context, TableTarget target, EndpointSnapshot snapshot)
    {
        if (!target.IsPartial)
        {
            context.ReportDiagnostic(Diagnostic.Create(Diagnostics.NotPartial, target.Location?.ToLocation(), target.TypeName));
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

        bool[] matched = new bool[target.Traits.Count];
        StringBuilder builder = SourceWriter.Header();
        if (target.Namespace.Length > 0)
        {
            builder.Append("namespace ").Append(target.Namespace).AppendLine(";").AppendLine();
        }

        builder.Append("partial class ").AppendLine(target.TypeName);
        builder.AppendLine("{");
        builder.AppendLine("    /// <summary>");
        builder.Append("    /// One descriptor per operation in docs/api-spec/endpoints.json (").Append(snapshot.Operations.Count).AppendLine(" at the last harvest): path, HTTP method and");
        builder.AppendLine("    /// <c>subaccount_id</c> support come from the snapshot; idempotency, rate-limit class and body limit from the [EndpointTraits] attributes on this class.");
        builder.AppendLine("    /// </summary>");
        builder.Append("    ").AppendLine(SourceWriter.GeneratedCodeAttribute);
        builder.Append("    private static global::System.Collections.Generic.Dictionary<(string Path, string Method), ").Append(EndpointType).AppendLine("> Seed()");
        builder.AppendLine("    {");
        builder.Append("        global::System.Collections.Generic.Dictionary<(string Path, string Method), ").Append(EndpointType).Append("> table = new(").Append(snapshot.Operations.Count).AppendLine(");");

        foreach (OperationInfo operation in snapshot.Operations)
        {
            bool idempotent = false;
            string rateLimit = "None";
            long maxBody = DefaultMaxBodyBytes;
            for (int i = 0; i < target.Traits.Count; i++)
            {
                TraitSpec trait = target.Traits[i];
                if (!Glob.IsMatch(trait.Pattern, operation.Path))
                {
                    continue;
                }

                matched[i] = true;
                idempotent = trait.Idempotent ?? idempotent;
                rateLimit = trait.RateLimitMember ?? rateLimit;
                maxBody = trait.MaxBodyBytes ?? maxBody;
            }

            builder.Append("        table.Add((").Append(SourceWriter.Literal(operation.Path)).Append(", ").Append(SourceWriter.Literal(operation.Method)).Append("), new ").Append(EndpointType).Append('(')
                .Append(SourceWriter.Literal(operation.Path)).Append(", ")
                .Append(SourceWriter.HttpMethodExpression(operation.Method)).Append(", ")
                .Append("Idempotent: ").Append(idempotent ? "true" : "false").Append(", ")
                .Append("AcceptsSubaccountId: ").Append(operation.AcceptsSubaccountId ? "true" : "false").Append(", ")
                .Append(RateLimitType).Append('.').Append(rateLimit).Append(", ")
                .Append(SourceWriter.Literal(maxBody)).AppendLine("));");
        }

        builder.AppendLine("        return table;");
        builder.AppendLine("    }");
        builder.AppendLine("}");

        for (int i = 0; i < matched.Length; i++)
        {
            if (!matched[i])
            {
                context.ReportDiagnostic(Diagnostic.Create(Diagnostics.TraitsPatternUnmatched, target.Traits[i].Location?.ToLocation(), target.Traits[i].Pattern));
            }
        }

        context.AddSource(target.TypeName + ".Seed.g.cs", SourceText.From(builder.ToString(), Encoding.UTF8));
    }
}
