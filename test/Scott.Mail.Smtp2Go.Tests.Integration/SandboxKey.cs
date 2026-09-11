using Microsoft.Extensions.Configuration;

namespace Scott.Mail.Smtp2Go.Tests.Integration;

/// <summary>
/// Resolves the SMTP2GO sandbox API key from the <c>SMTP2GO_SANDBOX_API_KEY</c> environment variable or the user secret
/// <c>Smtp2Go:ApiKey:Sandbox</c> (<c>dotnet user-secrets set "Smtp2Go:ApiKey:Sandbox" api-... --project test/Scott.Mail.Smtp2Go.Tests.Integration</c>).
/// Sandbox tests skip when neither is present. The sandbox accepts requests without delivering anything.
/// </summary>
public static class SandboxKey
{
    public const string EnvironmentVariable = "SMTP2GO_SANDBOX_API_KEY";
    public const string SecretName = "Smtp2Go:ApiKey:Sandbox";
    public const string SkipReason = "No sandbox API key: set SMTP2GO_SANDBOX_API_KEY or the user secret Smtp2Go:ApiKey:Sandbox.";

    private static readonly Lazy<string?> s_value = new(Resolve);

    public static string? Value => s_value.Value;

    public static bool IsAvailable => !string.IsNullOrWhiteSpace(Value);

    /// <summary>Returns the key or skips the calling test (xunit v3 dynamic skip).</summary>
    public static string GetOrSkip()
    {
        Assert.SkipUnless(IsAvailable, SkipReason);
        return Value!;
    }

    public static Smtp2GoClient CreateClient(Action<Smtp2GoClientOptions>? configure = null)
    {
        Smtp2GoClientOptions options = new() { ApiKey = GetOrSkip() };
        configure?.Invoke(options);
        return new Smtp2GoClient(options);
    }

    private static string? Resolve()
    {
        string? fromEnvironment = Environment.GetEnvironmentVariable(EnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(fromEnvironment))
        {
            return fromEnvironment;
        }

        IConfigurationRoot configuration = new ConfigurationBuilder().AddUserSecrets(typeof(SandboxKey).Assembly, optional: true).Build();
        return configuration[SecretName];
    }
}
