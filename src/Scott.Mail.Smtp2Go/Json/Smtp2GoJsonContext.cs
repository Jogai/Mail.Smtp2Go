using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Scott.Mail.Smtp2Go.Json.Converters;

namespace Scott.Mail.Smtp2Go.Json;

/// <summary>
/// Source-generated serializer context for every request and response model. snake_case wire names, nulls omitted, case-insensitive
/// reads, comments skipped, numbers readable from strings, and the SMTP2GO timestamp converter. Every model type must be listed here
/// with <see cref="JsonSerializableAttribute"/>; the transport refuses types that are not, which keeps the library trim- and AOT-safe.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    PropertyNameCaseInsensitive = true,
    ReadCommentHandling = JsonCommentHandling.Skip,
    NumberHandling = JsonNumberHandling.AllowReadingFromString,
    Converters = new[] { typeof(Smtp2GoDateTimeOffsetConverter), typeof(WebhookListConverter) })]

// Raw payloads usable with IRawClient for endpoints that have no typed model yet.
[JsonSerializable(typeof(JsonElement))]
[JsonSerializable(typeof(JsonNode))]
[JsonSerializable(typeof(JsonObject))]
[JsonSerializable(typeof(JsonArray))]
[JsonSerializable(typeof(ApiResponse<JsonElement>))]
[JsonSerializable(typeof(ApiResponse<JsonNode>))]
[JsonSerializable(typeof(ApiResponse<JsonObject>))]
[JsonSerializable(typeof(ApiResponse<JsonArray>))]

// Primitives and containers that may appear as template_data values (object-typed dictionary entries are resolved by runtime type).
[JsonSerializable(typeof(Dictionary<string, object>))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(string[]))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(long))]
[JsonSerializable(typeof(double))]
[JsonSerializable(typeof(decimal))]

// Email
[JsonSerializable(typeof(EmailSendRequest))]
[JsonSerializable(typeof(ApiResponse<EmailSendResult>))]
[JsonSerializable(typeof(EmailMimeRequest))]
[JsonSerializable(typeof(EmailBatchRequest))]
[JsonSerializable(typeof(ApiResponse<IReadOnlyList<EmailBatchItem>>))]
[JsonSerializable(typeof(ScheduledEmailSearchRequest))]
[JsonSerializable(typeof(ApiResponse<IReadOnlyList<ScheduledEmail>>))]
[JsonSerializable(typeof(ScheduledEmailRemoveRequest))]
#pragma warning disable CS0618 // The deprecated email/search endpoint is still served and its models must stay serialisable.
[JsonSerializable(typeof(EmailSearchRequest))]
[JsonSerializable(typeof(ApiResponse<EmailSearchResult>))]
#pragma warning restore CS0618

// Webhooks (management)
[JsonSerializable(typeof(WebhookAddRequest))]
[JsonSerializable(typeof(WebhookEditRequest))]
[JsonSerializable(typeof(WebhookRemoveRequest))]
[JsonSerializable(typeof(ApiResponse<Webhook>))]
[JsonSerializable(typeof(ApiResponse<IReadOnlyList<Webhook>>))]

// Stats
[JsonSerializable(typeof(StatsUsernameRequest))]
[JsonSerializable(typeof(EmailHistoryRequest))]
[JsonSerializable(typeof(ApiResponse<EmailSummary>))]
[JsonSerializable(typeof(ApiResponse<EmailCycle>))]
[JsonSerializable(typeof(ApiResponse<EmailBounces>))]
[JsonSerializable(typeof(ApiResponse<EmailSpam>))]
[JsonSerializable(typeof(ApiResponse<EmailUnsubscribes>))]
[JsonSerializable(typeof(ApiResponse<EmailHistory>))]

// Activity
[JsonSerializable(typeof(ActivitySearchRequest))]
[JsonSerializable(typeof(ApiResponse<ActivitySearchResult>))]

// Templates
[JsonSerializable(typeof(TemplateAddRequest))]
[JsonSerializable(typeof(TemplateUpdateRequest))]
[JsonSerializable(typeof(TemplateIdRequest))]
[JsonSerializable(typeof(TemplateSearchRequest))]
[JsonSerializable(typeof(ApiResponse<Template>))]
[JsonSerializable(typeof(ApiResponse<TemplateSearchResult>))]
[JsonSerializable(typeof(ApiResponse<string>))]

// Suppressions
[JsonSerializable(typeof(SuppressionAddRequest))]
[JsonSerializable(typeof(SuppressionViewRequest))]
[JsonSerializable(typeof(SuppressionRemoveRequest))]
[JsonSerializable(typeof(ApiResponse<SuppressionAddResult>))]
[JsonSerializable(typeof(ApiResponse<SuppressionViewResult>))]
[JsonSerializable(typeof(ApiResponse<SuppressionRemoveResult>))]

// Family plans add their request models and ApiResponse<TData> instantiations below, grouped by family.
internal sealed partial class Smtp2GoJsonContext : JsonSerializerContext
{
}
