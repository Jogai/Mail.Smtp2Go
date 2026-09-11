namespace Scott.Mail.Smtp2Go;

/// <summary>The SMTP2GO v3 API. One property per API family plus <see cref="Raw"/> for everything else.</summary>
public interface ISmtp2GoClient
{
    // Family clients are added here by their plans, one property per API family, in the order of architecture.md section 3.1:
    // Email, Activity, Stats, Webhooks, Templates, Suppressions, AllowedSenders, AllowedRecipients, ApiKeys, SmtpUsers, IpAuth,
    // Domains, SingleSenders, Subaccounts, DedicatedIps, Archive, Sms. Until a family lands, reach its endpoints through Raw.

    /// <summary>Sending: <c>email/send</c>, <c>email/mime</c>, <c>email/batch</c>, scheduled search and remove.</summary>
    IEmailClient Email { get; }

    /// <summary>Activity search: <c>activity/search</c>, every event of every email.</summary>
    IActivityClient Activity { get; }

    /// <summary>Webhook management: <c>webhook/view</c>, <c>webhook/add</c>, <c>webhook/edit</c>, <c>webhook/remove</c>.</summary>
    IWebhookClient Webhooks { get; }

    /// <summary>Email templates: <c>template/add</c>, <c>template/edit</c>, <c>template/delete</c>, <c>template/search</c>, <c>template/view</c>.</summary>
    ITemplateClient Templates { get; }

    /// <summary>Suppressions: <c>suppression/add</c>, <c>suppression/view</c>, <c>suppression/remove</c>.</summary>
    ISuppressionClient Suppressions { get; }

    /// <summary>API keys: <c>api_keys/view</c>, <c>add</c>, <c>edit</c> (POST and PATCH), <c>remove</c>, <c>permissions</c>.</summary>
    IApiKeyClient ApiKeys { get; }

    /// <summary>SMTP users: <c>users/smtp/view</c>, <c>add</c>, <c>edit</c> (POST and PATCH), <c>remove</c>.</summary>
    ISmtpUserClient SmtpUsers { get; }

    /// <summary>Authenticated IPs: <c>ip_auth/view</c>, <c>edit</c> (PATCH), <c>remove</c>.</summary>
    IIpAuthClient IpAuth { get; }

    /// <summary>Sender domains: <c>domain/view</c>, <c>add</c>, <c>verify</c>, <c>remove</c>, <c>tracking</c>, <c>returnpath</c>, <c>subaccount_access</c>.</summary>
    IDomainClient Domains { get; }

    /// <summary>Single sender emails: <c>single_sender_emails/view</c>, <c>add</c>, <c>remove</c>.</summary>
    ISingleSenderClient SingleSenders { get; }

    /// <summary>Email archive: <c>archive/search</c>, <c>archive/email</c> and downloading originals.</summary>
    IArchiveClient Archive { get; }

    /// <summary>Statistics: <c>stats/email_summary</c>, <c>email_cycle</c>, <c>email_bounces</c>, <c>email_spam</c>, <c>email_unsubs</c>, <c>email_history</c>.</summary>
    IStatsClient Stats { get; }

    /// <summary>Calls any endpoint by path; the escape hatch for endpoints without a typed client.</summary>
    IRawClient Raw { get; }
}
