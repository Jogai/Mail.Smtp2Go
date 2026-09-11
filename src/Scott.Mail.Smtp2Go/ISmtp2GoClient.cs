namespace Scott.Mail.Smtp2Go;

/// <summary>The SMTP2GO v3 API. One property per API family plus <see cref="Raw"/> for everything else.</summary>
public interface ISmtp2GoClient
{
    // Family clients are added here by their plans, one property per API family, in the order of architecture.md section 3.1:
    // Email, Activity, Stats, Webhooks, Templates, Suppressions, AllowedSenders, AllowedRecipients, ApiKeys, SmtpUsers, IpAuth,
    // Domains, SingleSenders, Subaccounts, DedicatedIps, Archive, Sms. Until a family lands, reach its endpoints through Raw.

    /// <summary>Sending: <c>email/send</c>, <c>email/mime</c>, <c>email/batch</c>, scheduled search and remove.</summary>
    IEmailClient Email { get; }

    /// <summary>Calls any endpoint by path; the escape hatch for endpoints without a typed client.</summary>
    IRawClient Raw { get; }
}
