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
    Converters = new[] { typeof(Smtp2GoDateTimeOffsetConverter), typeof(WebhookListConverter), typeof(ResultsListConverter<SmtpUser>), typeof(ResultsListConverter<AuthenticatedIp>) })]

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

// Archive
[JsonSerializable(typeof(ArchiveSearchRequest))]
[JsonSerializable(typeof(ArchiveEmailRequest))]
[JsonSerializable(typeof(ApiResponse<ArchiveSearchResult>))]
[JsonSerializable(typeof(ApiResponse<ArchivedEmail>))]

// API keys
[JsonSerializable(typeof(ApiKeyViewRequest))]
[JsonSerializable(typeof(ApiKeyAddRequest))]
[JsonSerializable(typeof(ApiKeyEditRequest))]
[JsonSerializable(typeof(ApiKeyPatchRequest))]
[JsonSerializable(typeof(ApiKeyRemoveRequest))]
[JsonSerializable(typeof(ApiKeyPermissionsRequest))]
[JsonSerializable(typeof(ApiResponse<IReadOnlyList<ApiKey>>))]
[JsonSerializable(typeof(ApiResponse<IReadOnlyList<string>>))]

// SMTP users (ResultsListConverter<SmtpUser> reads the {results: [...]} wrapper of add, edit (POST) and remove as well as the bare array of edit (PATCH))
[JsonSerializable(typeof(SmtpUserViewRequest))]
[JsonSerializable(typeof(SmtpUserAddRequest))]
[JsonSerializable(typeof(SmtpUserEditRequest))]
[JsonSerializable(typeof(SmtpUserPatchRequest))]
[JsonSerializable(typeof(SmtpUserRemoveRequest))]
[JsonSerializable(typeof(ApiResponse<SmtpUserViewResult>))]
[JsonSerializable(typeof(ApiResponse<IReadOnlyList<SmtpUser>>))]

// IP authentication (ResultsListConverter<AuthenticatedIp> reads the bare array of edit (PATCH) and any {results: [...]} wrapper)
[JsonSerializable(typeof(AuthenticatedIpViewRequest))]
[JsonSerializable(typeof(AuthenticatedIpPatchRequest))]
[JsonSerializable(typeof(AuthenticatedIpRemoveRequest))]
[JsonSerializable(typeof(ApiResponse<AuthenticatedIpViewResult>))]
[JsonSerializable(typeof(ApiResponse<IReadOnlyList<AuthenticatedIp>>))]

// Sender domains
[JsonSerializable(typeof(DomainViewRequest))]
[JsonSerializable(typeof(DomainAddRequest))]
[JsonSerializable(typeof(DomainVerifyRequest))]
[JsonSerializable(typeof(DomainRemoveRequest))]
[JsonSerializable(typeof(DomainTrackingRequest))]
[JsonSerializable(typeof(DomainReturnPathRequest))]
[JsonSerializable(typeof(DomainSubaccountAccessRequest))]
[JsonSerializable(typeof(ApiResponse<DomainViewResult>))]
[JsonSerializable(typeof(ApiResponse<DomainSubaccountAccessResult>))]

// Single sender emails (add returns ApiResponse<JsonElement>, remove ApiResponse<string>; both are registered above)
[JsonSerializable(typeof(SingleSenderViewRequest))]
[JsonSerializable(typeof(SingleSenderAddRequest))]
[JsonSerializable(typeof(SingleSenderRemoveRequest))]
[JsonSerializable(typeof(ApiResponse<SingleSenderViewResult>))]

// Allowed senders
[JsonSerializable(typeof(AllowedSendersAddRequest))]
[JsonSerializable(typeof(AllowedSendersRemoveRequest))]
[JsonSerializable(typeof(AllowedSendersUpdateRequest))]
[JsonSerializable(typeof(ApiResponse<AllowedSendersList>))]

// Allowed recipients
[JsonSerializable(typeof(AllowedRecipientsAddRequest))]
[JsonSerializable(typeof(AllowedRecipientsRemoveRequest))]
[JsonSerializable(typeof(AllowedRecipientsUpdateRequest))]
[JsonSerializable(typeof(ApiResponse<AllowedRecipientsList>))]

// Subaccounts (close and reopen return ApiResponse<string>, registered above)
[JsonSerializable(typeof(SubaccountSearchRequest))]
[JsonSerializable(typeof(SubaccountAddRequest))]
[JsonSerializable(typeof(SubaccountEditRequest))]
[JsonSerializable(typeof(SubaccountCloseRequest))]
[JsonSerializable(typeof(SubaccountReopenRequest))]
[JsonSerializable(typeof(ApiResponse<Subaccount>))]
[JsonSerializable(typeof(ApiResponse<SubaccountSearchResult>))]

// Family plans add their request models and ApiResponse<TData> instantiations below, grouped by family.
internal sealed partial class Smtp2GoJsonContext : JsonSerializerContext
{
}
