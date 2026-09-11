using System.Text.Json.Serialization;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go;

/// <summary>The body of <c>POST /sms/send</c>. The endpoint does not document <c>subaccount_id</c>.</summary>
[Smtp2GoEndpoint("sms/send")]
public sealed record SmsSendRequest : IRequestValidator
{
    /// <summary>The documented maximum number of destinations per call.</summary>
    public const int MaxDestinations = 100;

    /// <summary>The numbers to send to (at most <see cref="MaxDestinations"/>), with country code and an optional leading <c>+</c>, for example <c>+12025550959</c>.</summary>
    [JsonPropertyName("destination")]
    public required IReadOnlyList<string> Destination { get; init; }

    /// <summary>The sending number in E.164 format; omit for the account's default sender. A shared number in the recipient's country is used when the countries differ.</summary>
    [JsonPropertyName("sender")]
    public string? Sender { get; init; }

    /// <summary>The message. More than 160 characters is sent as several units.</summary>
    [JsonPropertyName("content")]
    public required string Content { get; init; }

    /// <inheritdoc />
    void IRequestValidator.Validate(Endpoint endpoint, ICollection<string> errors)
    {
        Argument.ThrowIfNull(errors);
        if (Destination is null || Destination.Count == 0)
        {
            errors.Add("destination must list at least one number.");
        }
        else
        {
            if (Destination.Count > MaxDestinations)
            {
                errors.Add($"destination must list at most {MaxDestinations} numbers.");
            }

            for (int i = 0; i < Destination.Count; i++)
            {
                if (Destination[i] is null || Destination[i].Trim().Length == 0)
                {
                    errors.Add($"destination[{i}] must not be blank.");
                }
            }
        }

        if (string.IsNullOrEmpty(Content))
        {
            errors.Add("content is required.");
        }
    }
}
