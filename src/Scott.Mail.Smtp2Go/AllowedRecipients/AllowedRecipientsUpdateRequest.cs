using System.Text.Json.Serialization;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go;

/// <summary>The body of <c>POST /allowed_recipients/update</c>: the whole list and whether it is applied. <c>subaccount_id</c> is injected from <see cref="RequestOptions.SubaccountId"/>.</summary>
[Smtp2GoEndpoint("allowed_recipients/update")]
public sealed record AllowedRecipientsUpdateRequest : IRequestValidator
{
    /// <summary>The addresses and domains that replace the list.</summary>
    [JsonPropertyName("allowed_recipients")]
    public required IReadOnlyList<string> AllowedRecipients { get; init; }

    /// <summary>Whether the list is taken into account when sending.</summary>
    [JsonPropertyName("enabled")]
    public required bool Enabled { get; init; }

    /// <inheritdoc />
    void IRequestValidator.Validate(Endpoint endpoint, ICollection<string> errors)
    {
        Argument.ThrowIfNull(errors);
        AllowListValidator.ValidateEntries(AllowedRecipients, "allowed_recipients", errors, allowEmpty: true);
    }
}
