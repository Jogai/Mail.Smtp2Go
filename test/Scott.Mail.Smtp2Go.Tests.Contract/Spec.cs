using System.Reflection;
using Scott.Mail.Smtp2Go.SpecHarvester;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go.Tests.Contract;

/// <summary>The harvested snapshot (<c>docs/api-spec</c>, copied to <c>api-spec/</c> next to the test assembly) and what reflection finds in the core assembly.</summary>
internal static class Spec
{
    /// <summary>Where the snapshot files were copied to.</summary>
    public static string Directory { get; } = Path.Combine(AppContext.BaseDirectory, "api-spec");

    /// <summary>The parsed <c>endpoints.json</c>.</summary>
    public static EndpointsDocument Endpoints { get; } = SpecFiles.ReadEndpoints(Directory);

    /// <summary>The parsed <c>known-unmodelled.json</c>.</summary>
    public static KnownUnmodelledDocument Known { get; } = SpecFiles.ReadKnownUnmodelled(Directory);

    /// <summary>The core assembly under test.</summary>
    public static Assembly Core { get; } = typeof(EndpointTable).Assembly;

    /// <summary>Every model carrying <see cref="Smtp2GoEndpointAttribute"/>.</summary>
    public static IReadOnlyList<AnnotatedModel> Models { get; } = ModelDiscovery.Find(Core);

    /// <summary>The descriptor registered for <paramref name="path"/> and <paramref name="method"/>, if any. Matches on both because the three patch paths are documented twice.</summary>
    public static Endpoint? Descriptor(string path, string method)
    {
        return EndpointTable.All.FirstOrDefault(e => string.Equals(e.Path, path, StringComparison.Ordinal) && string.Equals(e.Method.Method, method, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>The operation with the given <see cref="OperationSummary.Key"/> (for example <c>POST email/send</c>).</summary>
    public static OperationSummary Operation(string key)
    {
        return Endpoints.Operations.Single(o => string.Equals(o.Key, key, StringComparison.Ordinal));
    }

    /// <summary>The annotated model with the given full type name.</summary>
    public static AnnotatedModel Model(string typeFullName)
    {
        return Models.Single(m => string.Equals(m.Type.FullName, typeFullName, StringComparison.Ordinal));
    }

    /// <summary>Theory data: the key of every parsed operation.</summary>
    public static TheoryData<string> OperationKeys()
    {
        TheoryData<string> data = [];
        foreach (OperationSummary op in Endpoints.Operations.Where(o => o.IsParsed))
        {
            data.Add(op.Key);
        }

        return data;
    }

    /// <summary>Theory data: the full name of every annotated model with the given role.</summary>
    public static TheoryData<string> ModelNames(ModelRole role)
    {
        TheoryData<string> data = [];
        foreach (AnnotatedModel model in Models.Where(m => m.Role == role))
        {
            data.Add(model.Type.FullName!);
        }

        return data;
    }

    /// <summary>Theory data: the full name of every annotated model that has a known response data type.</summary>
    public static TheoryData<string> ModelsWithResponses()
    {
        TheoryData<string> data = [];
        foreach (AnnotatedModel model in Models.Where(m => m.ResponseDataType is not null))
        {
            data.Add(model.Type.FullName!);
        }

        return data;
    }
}
