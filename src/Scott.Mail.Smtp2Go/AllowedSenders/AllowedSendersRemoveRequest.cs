using System.Text.Json.Serialization;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go;

/// <summary>The body of <c>POST /allowed_senders/remove</c>. <c>subaccount_id</c> is injected from <see cref="RequestOptions.SubaccountId"/>.</summary>
[Smtp2GoEndpoint("allowed_senders/remove")]
public sealed record AllowedSendersRemoveRequest : IRequestValidator
{
    /// <summary>The addresses and domains to remove.</summary>
    [JsonPropertyName("allowed_senders")]
    public required IReadOnlyList<string> AllowedSenders { get; init; }

    /// <inheritdoc />
    void IRequestValidator.Validate(Endpoint endpoint, ICollection<string> errors)
    {
        Argument.ThrowIfNull(errors);
        AllowListValidator.ValidateEntries(AllowedSenders, "allowed_senders", errors);
    }
}
