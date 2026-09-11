namespace Scott.Mail.Smtp2Go.DependencyInjection;

/// <summary>Creates <see cref="ISmtp2GoClient"/> instances for registrations made with <c>AddSmtp2Go</c>, each over a factory-managed <c>HttpClient</c>.</summary>
public interface ISmtp2GoClientFactory
{
    /// <summary>
    /// Creates a client for the registration named <paramref name="name"/> (<see cref="Microsoft.Extensions.Options.Options.DefaultName"/> for the unnamed one).
    /// Clients are cheap; create one per unit of work rather than caching them, so options reloads and handler rotation take effect.
    /// </summary>
    /// <exception cref="Microsoft.Extensions.Options.OptionsValidationException">The options for <paramref name="name"/> are not valid.</exception>
    ISmtp2GoClient Create(string name);
}
