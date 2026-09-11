using System.Text.Json.Serialization;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go;

/// <summary>The body of <c>POST /allowed_recipients/add</c>. <c>subaccount_id</c> is injected from <see cref="RequestOptions.SubaccountId"/>.</summary>
[Smtp2GoEndpoint("allowed_recipients/add")]
public sealed record AllowedRecipientsAddRequest : IRequestValidator
{
    /// <summary>The addresses and domains to add.</summary>
    [JsonPropertyName("allowed_recipients")]
    public required IReadOnlyList<string> AllowedRecipients { get; init; }

    /// <summary>Whether the list is taken into account when sending; omit to leave the setting as it is.</summary>
    [JsonPropertyName("enabled")]
    public bool? Enabled { get; init; }

    /// <inheritdoc />
    void IRequestValidator.Validate(Endpoint endpoint, ICollection<string> errors)
    {
        Argument.ThrowIfNull(errors);
        AllowListValidator.ValidateEntries(AllowedRecipients, "allowed_recipients", errors);
    }
}
