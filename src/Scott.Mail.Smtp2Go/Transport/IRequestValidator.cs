namespace Scott.Mail.Smtp2Go.Transport;

/// <summary>
/// Implemented by request models that carry documented limits (for example 100 recipients per To/CC/BCC, 1,000 emails per batch).
/// The transport calls <see cref="Validate"/> before serialisation when <see cref="Smtp2GoClientOptions.ClientSideValidation"/> is on and throws
/// <see cref="Smtp2GoValidationException"/> with every message the model reported, before any network activity.
/// </summary>
public interface IRequestValidator
{
    /// <summary>Adds one message per problem to <paramref name="errors"/>; add nothing when the request is valid.</summary>
    /// <param name="endpoint">The endpoint the request is about to be sent to.</param>
    /// <param name="errors">The collection to report problems into.</param>
    void Validate(Endpoint endpoint, ICollection<string> errors);
}
