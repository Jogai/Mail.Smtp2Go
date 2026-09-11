using System.Text.Json.Serialization;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go;

/// <summary>The body of <c>POST /subaccount/reopen</c>: changes a closed subaccount back to active.</summary>
[Smtp2GoEndpoint("subaccount/reopen")]
public sealed record SubaccountReopenRequest : IRequestValidator
{
    /// <summary>The id of the subaccount to reopen.</summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>The email address of the subaccount, as the docs list it alongside the id.</summary>
    [JsonPropertyName("email")]
    public string? Email { get; init; }

    /// <inheritdoc />
    void IRequestValidator.Validate(Endpoint endpoint, ICollection<string> errors)
    {
        Argument.ThrowIfNull(errors);
        SubaccountRequestValidator.ValidateRequired(Id, "id", errors);
    }
}
