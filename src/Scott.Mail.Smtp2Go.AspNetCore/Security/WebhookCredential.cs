namespace Scott.Mail.Smtp2Go.AspNetCore;

/// <summary>One accepted <c>Authorization</c> value for an endpoint. The expected secret is fetched per request so rotated configuration is honoured.</summary>
internal abstract class WebhookCredential
{
    public const string BasicScheme = "Basic";
    public const string BearerScheme = "Bearer";

    /// <summary>The authentication scheme, <c>Basic</c> or <c>Bearer</c>.</summary>
    public abstract string Scheme { get; }

    /// <summary>The <c>WWW-Authenticate</c> challenge to send with a 401.</summary>
    public abstract string Challenge { get; }

    /// <summary>Whether the header's parameter (the part after the scheme) matches the expected secret; a constant-time comparison.</summary>
    public abstract bool Matches(string parameter, IServiceProvider services);

    public sealed class Basic(Func<IServiceProvider, (string Username, string Password)> credentials) : WebhookCredential
    {
        public override string Scheme => BasicScheme;

        public override string Challenge => "Basic realm=\"smtp2go\"";

        public override bool Matches(string parameter, IServiceProvider services)
        {
            (string username, string password) = credentials(services);
            return WebhookAuthenticator.MatchesBasic(parameter, username, password);
        }
    }

    public sealed class Bearer(Func<IServiceProvider, string> token) : WebhookCredential
    {
        public override string Scheme => BearerScheme;

        public override string Challenge => BearerScheme;

        public override bool Matches(string parameter, IServiceProvider services)
        {
            return WebhookAuthenticator.FixedTimeEquals(parameter, token(services));
        }
    }
}
