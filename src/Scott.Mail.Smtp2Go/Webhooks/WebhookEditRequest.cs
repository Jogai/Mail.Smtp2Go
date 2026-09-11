using System.Text.Json.Serialization;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go;

/// <summary>The body of <c>POST /webhook/edit</c>: <see cref="Id"/> plus any of the <c>webhook/add</c> fields to change. Omitted fields are left as they are.</summary>
public sealed record WebhookEditRequest : IRequestValidator
{
    /// <summary>The id of the webhook to change.</summary>
    [JsonPropertyName("id")]
    public required long Id { get; init; }

    /// <summary>The new URL.</summary>
    [JsonPropertyName("url")]
    public string? Url { get; init; }

    /// <summary>The new email event list.</summary>
    [JsonPropertyName("events")]
    public IReadOnlyList<WebhookEmailEvent>? Events { get; init; }

    /// <summary>The new SMS event list.</summary>
    [JsonPropertyName("sms_events")]
    public IReadOnlyList<WebhookSmsEvent>? SmsEvents { get; init; }

    /// <summary>The new custom header list.</summary>
    [JsonPropertyName("headers")]
    public IReadOnlyList<string>? Headers { get; init; }

    /// <summary>The new username restriction.</summary>
    [JsonPropertyName("usernames")]
    public IReadOnlyList<string>? Usernames { get; init; }

    /// <summary>The new callback body encoding.</summary>
    [JsonPropertyName("output_format")]
    public WebhookOutputFormat? OutputFormat { get; init; }

    /// <summary>The new <c>Authorization</c> scheme; <see cref="WebhookAuthHeaderType.None"/> sends the documented empty string to clear it.</summary>
    [JsonPropertyName("auth_header_type")]
    public WebhookAuthHeaderType? AuthHeaderType { get; init; }

    /// <summary>The new <c>Authorization</c> value.</summary>
    [JsonPropertyName("auth_header_value")]
    public string? AuthHeaderValue { get; init; }

    /// <summary>Creates an edit request carrying every field of <paramref name="request"/> for the webhook <paramref name="id"/>.</summary>
    public static WebhookEditRequest From(long id, WebhookAddRequest request)
    {
        Argument.ThrowIfNull(request);
        return new WebhookEditRequest
        {
            Id = id,
            Url = request.Url,
            Events = request.Events,
            SmsEvents = request.SmsEvents,
            Headers = request.Headers,
            Usernames = request.Usernames,
            OutputFormat = request.OutputFormat,
            AuthHeaderType = request.AuthHeaderType,
            AuthHeaderValue = request.AuthHeaderValue,
        };
    }

    /// <inheritdoc />
    void IRequestValidator.Validate(Endpoint endpoint, ICollection<string> errors)
    {
        Argument.ThrowIfNull(errors);
        if (Id <= 0)
        {
            errors.Add("id must be a positive webhook id.");
        }

        WebhookRequestValidator.Validate(Url, Events, SmsEvents, OutputFormat, AuthHeaderType, AuthHeaderValue, urlRequired: false, errors);
    }
}
