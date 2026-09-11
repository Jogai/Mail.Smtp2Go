namespace Scott.Mail.Smtp2Go.Webhooks;

/// <summary><c>open</c> (docs) or <c>opened</c> (live): the recipient opened the email. Engagement and geo fields are present where available. Base of <see cref="EmailClickEvent"/>.</summary>
public record EmailOpenEvent : EmailWebhookEvent
{
    /// <summary><c>user-agent</c> of the opening device.</summary>
    public string? UserAgent { get; init; }

    /// <summary><c>read-secs</c>: seconds the email was open, in five-second steps up to 30.</summary>
    public int? ReadSeconds { get; init; }

    /// <summary><c>client</c>: the reported mail client.</summary>
    public string? Client { get; init; }

    /// <summary><c>client-device</c>: the reported device type.</summary>
    public string? ClientDevice { get; init; }

    /// <summary><c>client-os</c>: the reported operating system.</summary>
    public string? ClientOs { get; init; }

    /// <summary><c>geoip-continent</c>: two-letter continent code.</summary>
    public string? GeoContinent { get; init; }

    /// <summary><c>geoip-country</c>: two-letter country code.</summary>
    public string? GeoCountry { get; init; }

    /// <summary><c>geoip-city</c>.</summary>
    public string? GeoCity { get; init; }
}
