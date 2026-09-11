using System.Text.Json.Serialization;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go;

/// <summary>The body of <c>POST /allowed_senders/add</c>. <c>subaccount_id</c> is injected from <see cref="RequestOptions.SubaccountId"/>.</summary>
[Smtp2GoEndpoint("allowed_senders/add")]
public sealed record AllowedSendersAddRequest : IRequestValidator
{
    /// <summary>The addresses and domains to add.</summary>
    [JsonPropertyName("allowed_senders")]
    public required IReadOnlyList<string> AllowedSenders { get; init; }

    /// <inheritdoc />
    void IRequestValidator.Validate(Endpoint endpoint, ICollection<string> errors)
    {
        Argument.ThrowIfNull(errors);
        AllowListValidator.ValidateEntries(AllowedSenders, "allowed_senders", errors);
    }
}
