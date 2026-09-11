using System.Text.Json;
using System.Text.Json.Serialization;

namespace Scott.Mail.Smtp2Go;

/// <summary>The <c>data</c> of <c>activity/search</c>: one page of events, the total available and the token for the next page.</summary>
public sealed record ActivitySearchResult
{
    /// <summary>The events of this page.</summary>
    [JsonPropertyName("events")]
    public IReadOnlyList<ActivityEvent>? Events { get; init; }

    /// <summary>The number of events matching the filters, which may exceed the page.</summary>
    [JsonPropertyName("total_events")]
    public long? TotalEvents { get; init; }

    /// <summary>Pass as <see cref="ActivitySearchRequest.ContinueToken"/> to fetch the next page; <see langword="null"/> on the last page.</summary>
    [JsonPropertyName("continue_token")]
    public string? ContinueToken { get; init; }

    /// <summary>Any field this library does not model.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? Extra { get; set; }
}
