using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go;

/// <summary>The (empty) body of <c>api_keys/permissions</c>; the attribute ties the operation to <see cref="IApiKeyClient.GetPermissionsAsync"/> for the coverage report.</summary>
[Smtp2GoEndpoint("api_keys/permissions")]
internal sealed record ApiKeyPermissionsRequest;
