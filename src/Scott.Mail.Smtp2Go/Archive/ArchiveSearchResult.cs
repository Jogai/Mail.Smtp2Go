using System.Text.Json;
using System.Text.Json.Serialization;

namespace Scott.Mail.Smtp2Go;

/// <summary>The <c>data</c> of <c>archive/search</c>.</summary>
public sealed record ArchiveSearchResult
{
    /// <summary>The number of emails returned.</summary>
    [JsonPropertyName("email_count")]
    public long? EmailCount { get; init; }

    /// <summary>The emails.</summary>
    [JsonPropertyName("emails")]
    public IReadOnlyList<ArchivedEmail>? Emails { get; init; }

    /// <summary>
    /// The token for the next page. The request documents <c>continue_token</c> but the response schema does not; this property reads it if the server sends it and is
    /// <see langword="null"/> otherwise, in which case <see cref="IArchiveClient.SearchAllAsync"/> stops after one page.
    /// </summary>
    [JsonPropertyName("continue_token")]
    public string? ContinueToken { get; init; }

    /// <summary>Any field this library does not model.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? Extra { get; set; }
}
