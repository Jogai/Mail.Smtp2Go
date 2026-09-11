using System.Collections;
using System.Reflection;
using System.Text.Json;
using Scott.Mail.Smtp2Go.Json;
using Scott.Mail.Smtp2Go.SpecHarvester;

namespace Scott.Mail.Smtp2Go.Tests.Contract;

/// <summary>The model-versus-spec comparisons, as functions returning problems, so the tests can assert on them and a self-test can prove they bite.</summary>
internal static class ContractChecks
{
    /// <summary>Model wire names that the docs do not list as request properties of the path (catches <c>output</c> versus <c>output_format</c>).</summary>
    public static IReadOnlyList<string> UndocumentedRequestProperties(Type model, string path, EndpointsDocument endpoints)
    {
        HashSet<string> documented = DocumentedRequestProperties(path, endpoints);
        return ModelDiscovery.WireNames(model)
            .Where(name => !documented.Contains(name))
            .Select(name => $"{model.Name}.{name} is not a documented request property of {path} (documented: {string.Join(", ", documented.Order(StringComparer.Ordinal))})")
            .ToList();
    }

    /// <summary>Documented request properties of the path that the model lacks and the allow-list does not cover.</summary>
    public static IReadOnlyList<string> UnmodelledRequestProperties(Type model, string path, EndpointsDocument endpoints, KnownUnmodelledDocument known)
    {
        HashSet<string> modelled = new(ModelDiscovery.WireNames(model), StringComparer.Ordinal);
        return DocumentedRequestProperties(path, endpoints)
            .Where(name => !modelled.Contains(name) && known.ForRequestField(path, name) is null)
            .Order(StringComparer.Ordinal)
            .Select(name => $"{path} documents request property '{name}' but {model.Name} has no property with that wire name and known-unmodelled.json has no request entry for it")
            .ToList();
    }

    /// <summary>Documented required properties that the model has but not as <c>required</c> members.</summary>
    public static IReadOnlyList<string> OptionalRequiredProperties(Type model, string path, EndpointsDocument endpoints, KnownUnmodelledDocument known)
    {
        HashSet<string> required = new(endpoints.ForPath(path).SelectMany(o => o.Required), StringComparer.Ordinal);
        return ModelDiscovery.SerializedProperties(model)
            .Where(p => required.Contains(ModelDiscovery.WireName(p)) && !ModelDiscovery.IsRequiredMember(p) && known.ForRequestField(path, ModelDiscovery.WireName(p)) is null)
            .Select(p => $"{path} documents '{ModelDiscovery.WireName(p)}' as required but {model.Name}.{p.Name} is not a required member")
            .ToList();
    }

    /// <summary>Documented response properties (of <c>data</c>, or of its items) that the response model lacks and the allow-list does not cover.</summary>
    public static IReadOnlyList<string> UnmodelledResponseProperties(Type responseDataType, string path, EndpointsDocument endpoints, KnownUnmodelledDocument known)
    {
        Type model = ModelDiscovery.ElementTypeOrSelf(responseDataType);
        if (!ModelDiscovery.IsObjectModel(model))
        {
            return [];
        }

        HashSet<string> modelled = new(ModelDiscovery.WireNames(model), StringComparer.Ordinal);
        return endpoints.ForPath(path)
            .SelectMany(o => o.ResponseProperties)
            .Select(p => p.Name)
            .Distinct(StringComparer.Ordinal)
            .Where(name => !modelled.Contains(name) && known.ForResponseField(path, name) is null)
            .Order(StringComparer.Ordinal)
            .Select(name => $"{path} documents response property '{name}' but {model.Name} has no property with that wire name and known-unmodelled.json has no response entry for it")
            .ToList();
    }

    /// <summary>Deserialises <paramref name="json"/> as <c>ApiResponse&lt;responseDataType&gt;</c> through the library's serializer context.</summary>
    public static object DeserializeEnvelope(string json, Type responseDataType)
    {
        Type envelope = typeof(ApiResponse<>).MakeGenericType(responseDataType);
        return JsonSerializer.Deserialize(json, envelope, Smtp2GoJsonContext.Default)
            ?? throw new InvalidOperationException("The example deserialised to null.");
    }

    /// <summary>Fields that landed in an extension-data bag (envelope or data) and are not allow-listed.</summary>
    public static IReadOnlyList<string> UnexpectedExtraFields(object envelope, string path, KnownUnmodelledDocument known)
    {
        List<string> problems = [];
        Type envelopeType = envelope.GetType();
        PropertyInfo extra = envelopeType.GetProperty(nameof(ApiResponse<int>.Extra))!;
        foreach (string key in Keys(extra.GetValue(envelope)))
        {
            if (known.ForResponseField(path, key) is null)
            {
                problems.Add($"{path}: the documented example has top-level field '{key}' that ApiResponse does not model");
            }
        }

        object? data = envelopeType.GetProperty(nameof(ApiResponse<int>.Data))!.GetValue(envelope);
        IEnumerable items = data is IEnumerable enumerable and not string ? enumerable : new[] { data };
        foreach (object? item in items)
        {
            if (item is null || !ModelDiscovery.IsObjectModel(item.GetType()))
            {
                continue;
            }

            PropertyInfo? bag = ModelDiscovery.ExtensionDataProperty(item.GetType());
            if (bag is null)
            {
                continue;
            }

            foreach (string key in Keys(bag.GetValue(item)))
            {
                if (known.ForResponseField(path, key) is null)
                {
                    problems.Add($"{path}: the documented example has field '{key}' that {item.GetType().Name} does not model (it landed in {bag.Name})");
                }
            }
        }

        return problems;
    }

    private static HashSet<string> DocumentedRequestProperties(string path, EndpointsDocument endpoints)
    {
        return new HashSet<string>(endpoints.ForPath(path).SelectMany(o => o.RequestProperties).Select(p => p.Name), StringComparer.Ordinal);
    }

    private static IEnumerable<string> Keys(object? extensionData)
    {
        return extensionData is IDictionary<string, JsonElement> dictionary ? dictionary.Keys : [];
    }
}
