using System.Text.Json;
using System.Text.Json.Serialization;
using Scott.Mail.Smtp2Go.Tests.Shared;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go.Tests.Unit;

internal static class TestClient
{
    public const string ApiKey = "api-0123456789ABCDEFGHIJKLMNOPQRSTUV";

    public static Smtp2GoClient Create(FakeHttpMessageHandler handler, Action<Smtp2GoClientOptions>? configure = null, ISmtp2GoDiagnostics? diagnostics = null)
    {
        Smtp2GoClientOptions options = new() { ApiKey = ApiKey };
        configure?.Invoke(options);
        return new Smtp2GoClient(new HttpClient(handler), options, diagnostics);
    }

    public static Smtp2GoConnection CreateConnection(FakeHttpMessageHandler handler, Action<Smtp2GoClientOptions>? configure = null, ISmtp2GoDiagnostics? diagnostics = null)
    {
        Smtp2GoClientOptions options = new() { ApiKey = ApiKey };
        configure?.Invoke(options);
        return new Smtp2GoConnection(new HttpClient(handler), options, diagnostics);
    }

    public static JsonElement Json(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}

/// <summary>A caller-supplied request model with a validation hook, registered through AdditionalJsonTypeInfoResolver.</summary>
public sealed record ProbeRequest(string? Name) : IRequestValidator
{
    public void Validate(Endpoint endpoint, ICollection<string> errors)
    {
        if (string.IsNullOrEmpty(Name))
        {
            errors.Add($"name is required for {endpoint.Path}.");
        }
    }
}

public sealed record ProbeData(string? Echo);

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(ProbeRequest))]
[JsonSerializable(typeof(ApiResponse<ProbeData>))]
public sealed partial class ProbeJsonContext : JsonSerializerContext
{
}
