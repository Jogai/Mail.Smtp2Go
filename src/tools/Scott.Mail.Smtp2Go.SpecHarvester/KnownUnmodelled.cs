using System.Text.Json.Serialization;

namespace Scott.Mail.Smtp2Go.SpecHarvester;

/// <summary>
/// The content of <c>docs/api-spec/known-unmodelled.json</c>: the documented things the library deliberately does not model yet, each with a reason.
/// An entry without <see cref="KnownUnmodelledEntry.Scope"/> covers a whole operation (no descriptor, no models); an entry with a scope covers one
/// request property, one response property, or one callback parameter. <c>*</c> as the endpoint or field is a wildcard.
/// </summary>
public sealed record KnownUnmodelledDocument
{
    /// <summary>Free-form notes for readers of the file.</summary>
    [JsonPropertyName("$comment")]
    public string? Comment { get; init; }

    /// <summary>The allow-list.</summary>
    public IReadOnlyList<KnownUnmodelledEntry> Entries { get; init; } = [];

    /// <summary>The entry allow-listing the operation as a whole (an entry without a scope, matching the path and, when given, the method), if any.</summary>
    public KnownUnmodelledEntry? ForOperation(string path, string method)
    {
        return Entries.FirstOrDefault(e => e.Scope is null && e.MatchesEndpoint(path) && e.MatchesMethod(method));
    }

    /// <summary>The entry allow-listing a request property, if any.</summary>
    public KnownUnmodelledEntry? ForRequestField(string path, string field) => ForField(path, KnownUnmodelledScope.Request, field);

    /// <summary>The entry allow-listing a response property, if any.</summary>
    public KnownUnmodelledEntry? ForResponseField(string path, string field) => ForField(path, KnownUnmodelledScope.Response, field);

    /// <summary>The entry allow-listing a callback parameter, if any.</summary>
    public KnownUnmodelledEntry? ForCallbackField(string path, string field) => ForField(path, KnownUnmodelledScope.Callback, field);

    /// <summary>The entry that points at a live fixture to test instead of the documented response example, if any.</summary>
    public KnownUnmodelledEntry? ResponseFixture(string path)
    {
        return Entries.FirstOrDefault(e => e.Scope == KnownUnmodelledScope.Response && e.Fixture is not null && e.MatchesEndpoint(path));
    }

    private KnownUnmodelledEntry? ForField(string path, KnownUnmodelledScope scope, string field)
    {
        return Entries.FirstOrDefault(e => e.Scope == scope && e.MatchesEndpoint(path) && e.MatchesField(field));
    }
}

/// <summary>What part of an operation an allow-list entry covers.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<KnownUnmodelledScope>))]
public enum KnownUnmodelledScope
{
    /// <summary>A request body property.</summary>
    Request,

    /// <summary>A property of the response <c>data</c> (or an unexpected field in the documented example).</summary>
    Response,

    /// <summary>A webhook callback parameter.</summary>
    Callback,
}

/// <summary>One allow-list entry.</summary>
public sealed record KnownUnmodelledEntry
{
    /// <summary>The operation path (no leading slash), a callback pseudo-path, or <c>*</c> for every operation.</summary>
    public required string Endpoint { get; init; }

    /// <summary>Restricts an operation-level entry to one HTTP method (needed where a path is documented with both <c>POST</c> and <c>PATCH</c>).</summary>
    public string? Method { get; init; }

    /// <summary>Which part of the operation the entry covers; <see langword="null"/> for the operation as a whole.</summary>
    public KnownUnmodelledScope? Scope { get; init; }

    /// <summary>The wire name of the property or parameter, or <c>*</c> for all of them; <see langword="null"/> for the operation as a whole.</summary>
    public string? Field { get; init; }

    /// <summary>Why it is not modelled. Required; the tests fail on an empty reason.</summary>
    public required string Reason { get; init; }

    /// <summary>For response entries: a fixture under <c>test/Scott.Mail.Smtp2Go.Tests.Shared/Fixtures</c> to deserialise instead of the documented example (used where the docs example is known to be wrong).</summary>
    public string? Fixture { get; init; }

    /// <summary>Whether the entry applies to <paramref name="path"/>.</summary>
    public bool MatchesEndpoint(string path)
    {
        return Endpoint == "*" || string.Equals(Endpoint.TrimStart('/'), path.TrimStart('/'), StringComparison.Ordinal);
    }

    /// <summary>Whether the entry applies to <paramref name="method"/> (any method when <see cref="Method"/> is not set).</summary>
    public bool MatchesMethod(string method)
    {
        return Method is null || string.Equals(Method, method, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Whether the entry applies to <paramref name="field"/>.</summary>
    public bool MatchesField(string field)
    {
        return Field == "*" || string.Equals(Field, field, StringComparison.Ordinal);
    }

    /// <summary>A short label for messages.</summary>
    public override string ToString()
    {
        string target = Scope is null ? Endpoint : $"{Endpoint} {Scope.Value.ToString().ToUpperInvariant()} {Field}";
        return Method is null ? target : $"{Method} {target}";
    }
}
