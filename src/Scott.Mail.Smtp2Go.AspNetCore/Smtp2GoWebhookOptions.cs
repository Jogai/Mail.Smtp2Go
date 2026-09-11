using Microsoft.AspNetCore.Http;

namespace Scott.Mail.Smtp2Go.AspNetCore;

/// <summary>
/// Settings for endpoints mapped with <c>MapSmtp2GoWebhook</c>. Register defaults through <c>AddSmtp2GoWebhooks(configure)</c> or
/// <c>services.Configure&lt;Smtp2GoWebhookOptions&gt;(...)</c>; each mapped endpoint takes a copy that
/// <see cref="Smtp2GoWebhookEndpointConventionBuilder.WithOptions"/> can adjust on its own.
/// </summary>
public sealed class Smtp2GoWebhookOptions
{
    /// <summary>The default <see cref="MaxBodyBytes"/>: 1 MiB. A callback is a few hundred bytes; the limit only bounds what a stranger can make the endpoint buffer.</summary>
    public const long DefaultMaxBodyBytes = 1024 * 1024;

    /// <summary>Largest request body the endpoint reads, in bytes. A longer body is answered with 413 before any handler runs. Default <see cref="DefaultMaxBodyBytes"/>.</summary>
    public long MaxBodyBytes { get; set; } = DefaultMaxBodyBytes;

    /// <summary>
    /// The media types the endpoint accepts (case-insensitive, parameters such as <c>charset</c> ignored). Anything else is answered with 415.
    /// Default: <c>application/json</c> (<c>output_format: json</c>), <c>application/x-www-form-urlencoded</c> (the server default) and <c>multipart/form-data</c>.
    /// Clear the list and add one entry to accept a single format.
    /// </summary>
    public ICollection<string> AllowedContentTypes { get; } = ["application/json", "application/x-www-form-urlencoded", "multipart/form-data"];

    /// <summary>
    /// The status returned when the handler throws. Default 500, which makes SMTP2GO retry the callback (up to 35 times over 48 hours); set 200 to acknowledge
    /// a callback whose handling failed and drop it. The exception is logged either way.
    /// </summary>
    public int ReturnStatusOnHandlerError { get; set; } = StatusCodes.Status500InternalServerError;

    /// <summary>
    /// The custom header names the webhook was registered with (its <c>headers</c> setting). Matching payload fields land in <c>EmailWebhookEvent.CustomHeaders</c>
    /// under the declared name instead of <c>WebhookEvent.Extra</c>. Passed to the core parser's <c>WebhookParserOptions.KnownCustomHeaders</c>.
    /// </summary>
    public ICollection<string> KnownCustomHeaders { get; } = [];

    internal Smtp2GoWebhookOptions Clone()
    {
        Smtp2GoWebhookOptions copy = new() { MaxBodyBytes = MaxBodyBytes, ReturnStatusOnHandlerError = ReturnStatusOnHandlerError };
        copy.AllowedContentTypes.Clear();
        foreach (string contentType in AllowedContentTypes)
        {
            copy.AllowedContentTypes.Add(contentType);
        }

        foreach (string header in KnownCustomHeaders)
        {
            copy.KnownCustomHeaders.Add(header);
        }

        return copy;
    }

    internal void Validate()
    {
        if (MaxBodyBytes <= 0)
        {
            throw new InvalidOperationException($"{nameof(Smtp2GoWebhookOptions)}.{nameof(MaxBodyBytes)} must be positive but is {MaxBodyBytes}.");
        }

        if (ReturnStatusOnHandlerError is < 200 or > 599)
        {
            throw new InvalidOperationException($"{nameof(Smtp2GoWebhookOptions)}.{nameof(ReturnStatusOnHandlerError)} must be an HTTP status between 200 and 599 but is {ReturnStatusOnHandlerError}.");
        }
    }
}
