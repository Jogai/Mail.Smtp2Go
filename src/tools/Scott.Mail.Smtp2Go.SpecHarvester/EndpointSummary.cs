using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Scott.Mail.Smtp2Go.SpecHarvester;

/// <summary>The content of <c>docs/api-spec/endpoints.json</c>: one entry per documented operation plus the webhook callback tables.</summary>
public sealed record EndpointsDocument
{
    /// <summary>Where the pages were harvested from.</summary>
    public required string Source { get; init; }

    /// <summary>The <c>info.version</c> of the OpenAPI fragments (all fragments agree today; otherwise the versions are joined with commas).</summary>
    public string? ApiVersion { get; init; }

    /// <summary>Every operation, sorted by path then method.</summary>
    public required IReadOnlyList<OperationSummary> Operations { get; init; }

    /// <summary>The webhook callback payloads from the webhooks overview, under the pseudo-paths <c>callbacks/email</c> and <c>callbacks/sms</c>.</summary>
    public required IReadOnlyList<CallbackSummary> Callbacks { get; init; }

    /// <summary>Reference pages that carried an OpenAPI block the tool could not parse. Their operations, if the path could be guessed, are also in <see cref="Operations"/> with <c>spec: unparsed</c>.</summary>
    public required IReadOnlyList<PageNote> UnparsedPages { get; init; }

    /// <summary>Reference pages without any OpenAPI block (guides such as authentication and the changelog).</summary>
    public required IReadOnlyList<string> PagesWithoutSpec { get; init; }

    /// <summary>Finds the operations documented under <paramref name="path"/> (usually one; <c>POST</c> and <c>PATCH</c> for the three patch paths).</summary>
    public IEnumerable<OperationSummary> ForPath(string path)
    {
        string normalized = path.TrimStart('/');
        return Operations.Where(o => string.Equals(o.Path, normalized, StringComparison.Ordinal));
    }
}

/// <summary>One documented API operation.</summary>
public sealed record OperationSummary
{
    /// <summary>Path relative to the v3 base URL, without a leading slash.</summary>
    public required string Path { get; init; }

    /// <summary>Upper-case HTTP method.</summary>
    public required string Method { get; init; }

    /// <summary>The OpenAPI <c>operationId</c>.</summary>
    public string? OperationId { get; init; }

    /// <summary>The reference page slug the operation came from.</summary>
    public required string Page { get; init; }

    /// <summary>The OpenAPI <c>summary</c> (the page title).</summary>
    public string? Summary { get; init; }

    /// <summary>The page's <c>updatedAt</c> front-matter value.</summary>
    public string? DocsUpdatedAt { get; init; }

    /// <summary><c>parsed</c> when the fragment parsed, <c>unparsed</c> when it did not (see <see cref="Error"/> and <see cref="Description"/>).</summary>
    public required string Spec { get; init; }

    /// <summary>Whether the operation is marked deprecated.</summary>
    public bool Deprecated { get; init; }

    /// <summary>The request body's <c>required</c> list.</summary>
    public IReadOnlyList<string> Required { get; init; } = [];

    /// <summary>The top-level request body properties, in documented order.</summary>
    public IReadOnlyList<PropertySummary> RequestProperties { get; init; } = [];

    /// <summary>Whether <c>subaccount_id</c> is a documented request property.</summary>
    public bool AcceptsSubaccountId { get; init; }

    /// <summary>The rate limit parsed from the description, if the description mentions one.</summary>
    public RateLimitNote? RateLimitNote { get; init; }

    /// <summary>The JSON type of the 200 response's <c>data</c> (<c>object</c>, <c>array</c>, <c>string</c>), when the schema says.</summary>
    public string? ResponseShape { get; init; }

    /// <summary>The properties of the 200 response's <c>data</c> object (or of its array items), in documented order.</summary>
    public IReadOnlyList<PropertySummary> ResponseProperties { get; init; } = [];

    /// <summary>The first documented 200 example, the whole envelope.</summary>
    public JsonNode? ResponseExample { get; init; }

    /// <summary>The page prose, kept only when the fragment did not parse.</summary>
    public string? Description { get; init; }

    /// <summary>The parse error, when the fragment did not parse.</summary>
    public string? Error { get; init; }

    /// <summary>Whether the fragment parsed.</summary>
    [JsonIgnore]
    public bool IsParsed => string.Equals(Spec, "parsed", StringComparison.Ordinal);

    /// <summary>The operation's identity for messages, for example <c>POST email/send</c>.</summary>
    [JsonIgnore]
    public string Key => Method + " " + Path;
}

/// <summary>One request or response property.</summary>
public sealed record PropertySummary
{
    /// <summary>The wire name.</summary>
    public required string Name { get; init; }

    /// <summary>The JSON schema type, when given.</summary>
    public string? Type { get; init; }

    /// <summary>Whether the property is marked deprecated.</summary>
    public bool Deprecated { get; init; }
}

/// <summary>A rate limit stated in an operation description.</summary>
public sealed record RateLimitNote
{
    /// <summary>The number of requests.</summary>
    public required int Limit { get; init; }

    /// <summary>The period: <c>minute</c>, <c>hour</c>, <c>second</c> or <c>day</c>.</summary>
    public required string Per { get; init; }

    /// <summary>The matched text.</summary>
    public required string Text { get; init; }
}

/// <summary>The parameters of one webhook callback payload.</summary>
public sealed record CallbackSummary
{
    /// <summary>The pseudo-path: <c>callbacks/email</c> or <c>callbacks/sms</c>.</summary>
    public required string Path { get; init; }

    /// <summary>The event names from the events table (as documented, for example <c>Processed</c>).</summary>
    public IReadOnlyList<string> Events { get; init; } = [];

    /// <summary>The wire values of the <c>event</c> parameter quoted in its description (for example <c>processed</c>, <c>sms_delivered</c>).</summary>
    public IReadOnlyList<string> EventValues { get; init; } = [];

    /// <summary>The documented parameters, in table order.</summary>
    public IReadOnlyList<CallbackParameter> Parameters { get; init; } = [];
}

/// <summary>One webhook callback parameter.</summary>
public sealed record CallbackParameter
{
    /// <summary>The wire name, for example <c>message-id</c>.</summary>
    public required string Name { get; init; }

    /// <summary>The documented description.</summary>
    public string? Description { get; init; }
}

/// <summary>A reference page whose OpenAPI block could not be parsed.</summary>
public sealed record PageNote
{
    /// <summary>The page slug.</summary>
    public required string Page { get; init; }

    /// <summary>The page title from the index.</summary>
    public string? Title { get; init; }

    /// <summary>The page prose.</summary>
    public string? Description { get; init; }

    /// <summary>The parse error.</summary>
    public required string Error { get; init; }
}
