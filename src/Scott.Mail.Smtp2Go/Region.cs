namespace Scott.Mail.Smtp2Go;

/// <summary>The regional SMTP2GO API endpoint to send requests to.</summary>
public enum Region
{
    /// <summary><c>https://api.smtp2go.com/v3/</c>, routed to the nearest region by SMTP2GO.</summary>
    Global = 0,

    /// <summary><c>https://us-api.smtp2go.com/v3/</c>.</summary>
    US = 1,

    /// <summary><c>https://eu-api.smtp2go.com/v3/</c>.</summary>
    EU = 2,

    /// <summary><c>https://au-api.smtp2go.com/v3/</c>.</summary>
    AU = 3,
}
