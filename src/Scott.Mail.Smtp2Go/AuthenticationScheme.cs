namespace Scott.Mail.Smtp2Go;

/// <summary>How the API key is presented to the SMTP2GO API. The key is always sent in a header, never in the request body.</summary>
public enum AuthenticationScheme
{
    /// <summary>Send the key in the <c>X-Smtp2go-Api-Key</c> header. This is the default.</summary>
    ApiKeyHeader = 0,

    /// <summary>Send the key as <c>Authorization: Bearer &lt;key&gt;</c>.</summary>
    Bearer = 1,
}
