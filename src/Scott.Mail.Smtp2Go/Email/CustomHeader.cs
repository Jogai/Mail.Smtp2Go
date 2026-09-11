using System.Text.Json.Serialization;

namespace Scott.Mail.Smtp2Go;

/// <summary>A custom header on an outgoing email. <c>Content-Type</c>, <c>Content-Transfer-Encoding</c> and <c>MIME-Version</c> are not allowed by the API.</summary>
/// <param name="Header">The header name, for example <c>X-Campaign</c>.</param>
/// <param name="Value">The header value.</param>
public sealed record CustomHeader(
    [property: JsonPropertyName("header")] string Header,
    [property: JsonPropertyName("value")] string Value);
