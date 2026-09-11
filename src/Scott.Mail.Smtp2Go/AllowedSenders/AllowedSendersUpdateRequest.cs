using System.Text.Json.Serialization;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go;

/// <summary>The body of <c>POST /allowed_senders/update</c>: the whole list and its mode. <c>subaccount_id</c> is injected from <see cref="RequestOptions.SubaccountId"/>.</summary>
[Smtp2GoEndpoint("allowed_senders/update")]
public sealed record AllowedSendersUpdateRequest : IRequestValidator
{
    /// <summary>The addresses and domains that replace the list.</summary>
    [JsonPropertyName("allowed_senders")]
    public required IReadOnlyList<string> AllowedSenders { get; init; }

    /// <summary>How the list is interpreted. Whitelist and blacklist disable the Sender Domains and Single Sender Emails features.</summary>
    [JsonPropertyName("mode")]
    public required AllowedSendersMode Mode { get; init; }

    /// <inheritdoc />
    void IRequestValidator.Validate(Endpoint endpoint, ICollection<string> errors)
    {
        Argument.ThrowIfNull(errors);
        AllowListValidator.ValidateEntries(AllowedSenders, "allowed_senders", errors, allowEmpty: true);
        if (Mode == AllowedSendersMode.Unknown)
        {
            errors.Add("mode must not be AllowedSendersMode.Unknown.");
        }
    }
}
