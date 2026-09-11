using Microsoft.Extensions.Configuration;

namespace Scott.Mail.Smtp2Go.Tests.Integration.Live;

/// <summary>
/// Settings for the manual, live-account tests (<c>[Trait("Category", "Live")]</c>): a real (not sandbox) API key with Email Archiving, a verified sender and a recipient.
/// Read from <c>SMTP2GO_LIVE_API_KEY</c>, <c>SMTP2GO_SENDER</c> and <c>SMTP2GO_RECIPIENT</c>, or the user secrets <c>Smtp2Go:ApiKey:Live</c>, <c>Smtp2Go:Live:Sender</c> and
/// <c>Smtp2Go:Live:Recipient</c>. The tests skip when any is missing. Set <c>SMTP2GO_CAPTURE=1</c> to write captured callbacks into <c>Fixtures/Webhooks/Live/</c>.
/// </summary>
public static class LiveEnvironment
{
    public const string SkipReason = "No live settings: set SMTP2GO_LIVE_API_KEY, SMTP2GO_SENDER and SMTP2GO_RECIPIENT (or the Smtp2Go:ApiKey:Live, Smtp2Go:Live:Sender, Smtp2Go:Live:Recipient user secrets).";

    private static readonly Lazy<IConfigurationRoot> s_configuration = new(() => new ConfigurationBuilder().AddUserSecrets(typeof(LiveEnvironment).Assembly, optional: true).Build());

    public static string? ApiKey => Get("SMTP2GO_LIVE_API_KEY", "Smtp2Go:ApiKey:Live");

    public static string? Sender => Get("SMTP2GO_SENDER", "Smtp2Go:Live:Sender");

    public static string? Recipient => Get("SMTP2GO_RECIPIENT", "Smtp2Go:Live:Recipient");

    public static bool Capture => Environment.GetEnvironmentVariable("SMTP2GO_CAPTURE") is "1" or "true";

    public static bool IsAvailable => !string.IsNullOrWhiteSpace(ApiKey) && !string.IsNullOrWhiteSpace(Sender) && !string.IsNullOrWhiteSpace(Recipient);

    public static Smtp2GoClient CreateClientOrSkip()
    {
        Assert.SkipUnless(IsAvailable, SkipReason);
        return new Smtp2GoClient(ApiKey!);
    }

    /// <summary>The checked-in <c>Fixtures/Webhooks/Live</c> folder of the shared test project, found by walking up from the test assembly.</summary>
    public static string LiveFixtureDirectory()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Scott.Mail.Smtp2Go.slnx")))
        {
            directory = directory.Parent;
        }

        if (directory is null)
        {
            throw new InvalidOperationException("Could not locate the repository root from " + AppContext.BaseDirectory);
        }

        return Path.Combine(directory.FullName, "test", "Scott.Mail.Smtp2Go.Tests.Shared", "Fixtures", "Webhooks", "Live");
    }

    private static string? Get(string variable, string secret)
    {
        string? value = Environment.GetEnvironmentVariable(variable);
        return !string.IsNullOrWhiteSpace(value) ? value : s_configuration.Value[secret];
    }
}
