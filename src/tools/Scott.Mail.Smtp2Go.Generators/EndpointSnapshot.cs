using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Scott.Mail.Smtp2Go.Generators;

/// <summary>One documented operation from <c>endpoints.json</c>: what the generators need of it.</summary>
internal sealed record OperationInfo(string Path, string Method, bool AcceptsSubaccountId);

/// <summary>
/// The harvested endpoint snapshot (<c>docs/api-spec/endpoints.json</c>, passed to the compiler as an additional file), or why it is unavailable.
/// <see cref="Operations"/> is empty when <see cref="Error"/> is set; <see cref="Found"/> is false when no additional file has the expected name.
/// </summary>
internal sealed record EndpointSnapshot(bool Found, string? Error, EquatableArray<OperationInfo> Operations)
{
    public const string FileName = "endpoints.json";

    public static readonly EndpointSnapshot Missing = new(false, null, EquatableArray<OperationInfo>.Empty);

    /// <summary>The additional file named <c>endpoints.json</c>, parsed. Cached by the pipeline on the file's content.</summary>
    public static IncrementalValueProvider<EndpointSnapshot> Provide(IncrementalGeneratorInitializationContext context)
    {
        return context.AdditionalTextsProvider
            .Where(static text => string.Equals(System.IO.Path.GetFileName(text.Path), FileName, StringComparison.OrdinalIgnoreCase))
            .Select(static (text, cancellationToken) => text.GetText(cancellationToken)?.ToString())
            .Collect()
            .Select(static (texts, _) => texts.Length == 0 ? Missing : Parse(texts[0]));
    }

    /// <summary>The methods documented for <paramref name="path"/>, in snapshot order.</summary>
    public ImmutableArray<string> MethodsFor(string path)
    {
        ImmutableArray<string>.Builder methods = ImmutableArray.CreateBuilder<string>();
        foreach (OperationInfo operation in Operations)
        {
            if (string.Equals(operation.Path, path, StringComparison.Ordinal))
            {
                methods.Add(operation.Method);
            }
        }

        return methods.ToImmutable();
    }

    public static EndpointSnapshot Parse(string? json)
    {
        if (json is null)
        {
            return new EndpointSnapshot(true, "the file is empty", EquatableArray<OperationInfo>.Empty);
        }

        try
        {
            if (MiniJson.Parse(json) is not Dictionary<string, object?> root
                || !root.TryGetValue("operations", out object? operationsNode)
                || operationsNode is not List<object?> operations)
            {
                return new EndpointSnapshot(true, "the document has no 'operations' array", EquatableArray<OperationInfo>.Empty);
            }

            ImmutableArray<OperationInfo>.Builder result = ImmutableArray.CreateBuilder<OperationInfo>(operations.Count);
            foreach (object? node in operations)
            {
                if (node is not Dictionary<string, object?> operation
                    || !operation.TryGetValue("path", out object? pathNode) || pathNode is not string path
                    || !operation.TryGetValue("method", out object? methodNode) || methodNode is not string method)
                {
                    return new EndpointSnapshot(true, "an operation lacks 'path' or 'method'", EquatableArray<OperationInfo>.Empty);
                }

                bool acceptsSubaccountId = operation.TryGetValue("acceptsSubaccountId", out object? flag) && flag is true;
                result.Add(new OperationInfo(path.Trim().TrimStart('/'), method.ToUpperInvariant(), acceptsSubaccountId));
            }

            return new EndpointSnapshot(true, null, new EquatableArray<OperationInfo>(result.ToImmutable()));
        }
        catch (FormatException exception)
        {
            return new EndpointSnapshot(true, exception.Message, EquatableArray<OperationInfo>.Empty);
        }
    }
}
