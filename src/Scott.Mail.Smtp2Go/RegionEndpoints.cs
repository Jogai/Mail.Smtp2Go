namespace Scott.Mail.Smtp2Go;

/// <summary>The base URLs of the SMTP2GO v3 API per <see cref="Region"/>.</summary>
public static class RegionEndpoints
{
    /// <summary>Base URL for <see cref="Region.Global"/>.</summary>
    public static Uri Global { get; } = new("https://api.smtp2go.com/v3/");

    /// <summary>Base URL for <see cref="Region.US"/>.</summary>
    public static Uri US { get; } = new("https://us-api.smtp2go.com/v3/");

    /// <summary>Base URL for <see cref="Region.EU"/>.</summary>
    public static Uri EU { get; } = new("https://eu-api.smtp2go.com/v3/");

    /// <summary>Base URL for <see cref="Region.AU"/>.</summary>
    public static Uri AU { get; } = new("https://au-api.smtp2go.com/v3/");

    /// <summary>Returns the base URL for <paramref name="region"/>.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="region"/> is not a defined value.</exception>
    public static Uri GetBaseUrl(Region region)
    {
        return region switch
        {
            Region.Global => Global,
            Region.US => US,
            Region.EU => EU,
            Region.AU => AU,
            _ => throw new ArgumentOutOfRangeException(nameof(region), region, "Unknown region."),
        };
    }
}
