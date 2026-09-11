namespace Scott.Mail.Smtp2Go.AspNetCore;

/// <summary>
/// Stable event ids of every log message the package writes, all under category <see cref="CategoryName"/>: 500s for the callback lifecycle, 510s for
/// rejected requests, 520s for payload problems, 530s for handler failures, 540s for the source-IP resolver. They continue the dependency-injection package's
/// <c>Smtp2GoEventIds</c> (100 to 405) without overlap.
/// </summary>
public static class Smtp2GoWebhookEventIds
{
    /// <summary>The log category, <c>Scott.Mail.Smtp2Go.AspNetCore</c>.</summary>
    public const string CategoryName = "Scott.Mail.Smtp2Go.AspNetCore";

    /// <summary>Debug: a callback was parsed and is about to be handled.</summary>
    public const int CallbackReceived = 500;

    /// <summary>Information: the handler completed and 200 is being returned.</summary>
    public const int CallbackHandled = 501;

    /// <summary>Debug: no <c>IWebhookEventHandler&lt;TEvent&gt;</c> is registered for the callback's type or any of its base types.</summary>
    public const int NoHandlerRegistered = 502;

    /// <summary>Warning: the request carried no acceptable credentials and was answered with 401.</summary>
    public const int Unauthorized = 510;

    /// <summary>Warning: the request did not come from an address of <c>webhooks.smtp2go.com</c> and was answered with 403.</summary>
    public const int SourceIpRejected = 511;

    /// <summary>Error: the source addresses could not be resolved, so the request was answered with 503.</summary>
    public const int SourceIpResolutionFailed = 512;

    /// <summary>Warning: the request's media type is not in <see cref="Smtp2GoWebhookOptions.AllowedContentTypes"/> and was answered with 415.</summary>
    public const int UnsupportedMediaType = 520;

    /// <summary>Warning: the request body exceeds <see cref="Smtp2GoWebhookOptions.MaxBodyBytes"/> and was answered with 413.</summary>
    public const int PayloadTooLarge = 521;

    /// <summary>Warning: the body could not be parsed as a callback and was answered with 400.</summary>
    public const int PayloadInvalid = 522;

    /// <summary>Error: the handler threw; <see cref="Smtp2GoWebhookOptions.ReturnStatusOnHandlerError"/> is being returned.</summary>
    public const int HandlerFailed = 530;

    /// <summary>Debug: the resolver looked up <c>webhooks.smtp2go.com</c> and refreshed its cache.</summary>
    public const int SourceIpResolved = 540;

    /// <summary>Warning: the lookup failed and the resolver is serving the previous, expired result.</summary>
    public const int SourceIpStaleCacheUsed = 541;
}
