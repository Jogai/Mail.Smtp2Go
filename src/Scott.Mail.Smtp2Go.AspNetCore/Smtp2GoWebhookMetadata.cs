namespace Scott.Mail.Smtp2Go.AspNetCore;

/// <summary>
/// Marks an endpoint mapped by <c>MapSmtp2GoWebhook</c> so endpoint filters, middleware and OpenAPI transformers can recognise it through
/// <c>HttpContext.GetEndpoint()?.Metadata.GetMetadata&lt;Smtp2GoWebhookMetadata&gt;()</c>. The endpoint is also excluded from API descriptions by default.
/// </summary>
public sealed class Smtp2GoWebhookMetadata
{
    /// <summary>Creates the metadata.</summary>
    /// <param name="pattern">The route pattern the endpoint was mapped with.</param>
    /// <param name="usesHandlerDispatch"><see langword="true"/> when callbacks go to registered <c>IWebhookEventHandler&lt;TEvent&gt;</c> services, <see langword="false"/> for an inline delegate.</param>
    public Smtp2GoWebhookMetadata(string pattern, bool usesHandlerDispatch)
    {
        Argument.ThrowIfNull(pattern);
        Pattern = pattern;
        UsesHandlerDispatch = usesHandlerDispatch;
    }

    /// <summary>The route pattern the endpoint was mapped with.</summary>
    public string Pattern { get; }

    /// <summary><see langword="true"/> when callbacks are dispatched to registered <c>IWebhookEventHandler&lt;TEvent&gt;</c> services; <see langword="false"/> when an inline delegate handles them.</summary>
    public bool UsesHandlerDispatch { get; }
}
