namespace Scott.Mail.Smtp2Go.Transport;

/// <summary>
/// Links a model to the API operation it is the body (or the <c>data</c>) of, so the contract tests and the spec harvester's coverage report
/// can find every model by reflection instead of by naming convention. Put it on the request record of an endpoint; for endpoints without a
/// request body put it on the response data record. The value is the path relative to the v3 base URL, for example <c>email/send</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class Smtp2GoEndpointAttribute : Attribute
{
    /// <summary>Creates the attribute for <paramref name="path"/>.</summary>
    /// <param name="path">The endpoint path relative to the v3 base URL, without a leading slash, for example <c>webhook/add</c>.</param>
    public Smtp2GoEndpointAttribute(string path)
    {
        Argument.ThrowIfNullOrWhiteSpace(path);
        Path = EndpointTable.Normalize(path);
    }

    /// <summary>The endpoint path relative to the v3 base URL, without a leading slash.</summary>
    public string Path { get; }
}
