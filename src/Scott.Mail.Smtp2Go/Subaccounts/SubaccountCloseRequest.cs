using System.Text.Json.Serialization;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go;

/// <summary>The body of <c>POST /subaccount/close</c>: changes an active subaccount to closed.</summary>
[Smtp2GoEndpoint("subaccount/close")]
public sealed record SubaccountCloseRequest : IRequestValidator
{
    /// <summary>The id of the subaccount to close.</summary>
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
