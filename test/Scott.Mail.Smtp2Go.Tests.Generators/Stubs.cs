namespace Scott.Mail.Smtp2Go.Tests.Generators;

/// <summary>
/// The smallest stand-in for the library the generated code compiles against: the types the emitted sources name
/// (<c>Endpoint</c>, <c>EndpointTable</c>, <c>Smtp2GoConnection</c>, <c>ApiResponse</c>, <c>EnumMember</c>, <c>TolerantEnumConverter</c>).
/// Tests add their own interfaces, models and marked classes on top.
/// </summary>
public static class Stubs
{
    public const string Library = """
        #nullable enable
        using System;
        using System.Collections.Generic;
        using System.Net.Http;
        using System.Threading;
        using System.Threading.Tasks;

        namespace Scott.Mail.Smtp2Go
        {
            public sealed class ApiResponse<TData> { public TData? Data { get; init; } }
            public sealed record RequestOptions;
            internal static class Argument { public static void ThrowIfNull(object? value) { if (value is null) throw new ArgumentNullException(nameof(value)); } }
        }

        namespace Scott.Mail.Smtp2Go.Transport
        {
            public enum RateLimitClass { None = 0, ActivitySearch = 1, ApiKeyAdd = 2 }

            public sealed record Endpoint(string Path, HttpMethod Method, bool Idempotent, bool AcceptsSubaccountId, RateLimitClass RateLimit, long MaxBodyBytes)
            {
                public const long DefaultMaxBodyBytes = 1L * 1024 * 1024;
                public const long EmailMaxBodyBytes = 50L * 1024 * 1024;
                public static HttpMethod Patch { get; } = new("PATCH");
            }

            [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
            public sealed class Smtp2GoEndpointAttribute(string path) : Attribute { public string Path { get; } = path; }

            public sealed class Smtp2GoConnection
            {
                public Task<ApiResponse<TResponse>> SendAsync<TRequest, TResponse>(Endpoint endpoint, TRequest? body, RequestOptions? options, CancellationToken cancellationToken)
                    => Task.FromResult(new ApiResponse<TResponse>());
            }

            // The stub never calls the generated Seed(); the table tests read the generated source and compile it.
            public static partial class EndpointTable
            {
                public static Endpoint Get(string path) => new(path, HttpMethod.Post, false, false, RateLimitClass.None, Endpoint.DefaultMaxBodyBytes);
                public static Endpoint Get(string path, HttpMethod method) => new(path, method, false, false, RateLimitClass.None, Endpoint.DefaultMaxBodyBytes);
            }
        }

        namespace Scott.Mail.Smtp2Go.Json
        {
            internal readonly record struct EnumMember<TEnum>(TEnum Value, string Name, string WireName) where TEnum : struct, Enum;
        }

        namespace Scott.Mail.Smtp2Go.Json.Converters
        {
            public sealed class TolerantEnumConverter<TEnum> : System.Text.Json.Serialization.JsonConverter<TEnum> where TEnum : struct, Enum
            {
                public override TEnum Read(ref System.Text.Json.Utf8JsonReader reader, Type typeToConvert, System.Text.Json.JsonSerializerOptions options) => default;
                public override void Write(System.Text.Json.Utf8JsonWriter writer, TEnum value, System.Text.Json.JsonSerializerOptions options) { }
            }
        }
        """;

    /// <summary>A snapshot with four operations: a plain POST, a rate-limited search, and a path documented as both POST and PATCH.</summary>
    public const string Snapshot = """
        {
          "operations": [
            { "path": "email/send", "method": "POST", "acceptsSubaccountId": false },
            { "path": "activity/search", "method": "POST", "acceptsSubaccountId": false, "rateLimitNote": { "limit": 60, "per": "minute" } },
            { "path": "api_keys/edit", "method": "POST", "acceptsSubaccountId": true },
            { "path": "api_keys/edit", "method": "PATCH", "acceptsSubaccountId": true },
            { "path": "webhook/view", "method": "POST", "acceptsSubaccountId": true }
          ]
        }
        """;
}
