using Scott.Mail.Smtp2Go.Generation;

namespace Scott.Mail.Smtp2Go.Transport;

/// <summary>
/// Registry of known <see cref="Endpoint"/> descriptors, keyed by path and HTTP method: <c>api_keys/edit</c> and <c>users/smtp/edit</c> are
/// documented with both <c>POST</c> (full edit) and <c>PATCH</c> (partial edit), so a path may carry two descriptors. Unknown paths get a
/// conservative default so the raw client can reach endpoints that have no descriptor yet.
/// </summary>
/// <remarks>
/// The rows are generated at build time from <c>docs/api-spec/endpoints.json</c> (see <c>Seed()</c> in the generated part): every documented
/// operation gets a descriptor with its path, HTTP method and <c>subaccount_id</c> support taken from the snapshot. The reference does not
/// state idempotency, rate-limit classes or body limits, so those come from the <see cref="EndpointTraitsAttribute"/> list below, applied in
/// order; a pattern that matches nothing fails the build. Re-harvesting the snapshot therefore changes this table without a code change,
/// which the contract tests check against the same snapshot.
/// </remarks>
// Idempotent: the read-only endpoints. The default is "not idempotent", so a newly documented operation is never retried until someone says so.
[EndpointTraits("*/view", Idempotent = true)]
[EndpointTraits("*/search", Idempotent = true)]
[EndpointTraits("sms/view-*", Idempotent = true)]
[EndpointTraits("sms/summary", Idempotent = true)]
[EndpointTraits("stats/*", Idempotent = true)]
[EndpointTraits("archive/email", Idempotent = true)]
[EndpointTraits("api_keys/permissions", Idempotent = true)]
// Rate limits: one class per documented limit (RateLimitClass states the numbers; the contract tests compare them with the docs).
[EndpointTraits("activity/search", RateLimit = RateLimitClass.ActivitySearch)]
[EndpointTraits("api_keys/add", RateLimit = RateLimitClass.ApiKeyAdd)]
[EndpointTraits("subaccount/add", RateLimit = RateLimitClass.SubaccountAdd)]
[EndpointTraits("email/search", RateLimit = RateLimitClass.EmailSearch)]
// Body limits: 50 MB for the endpoints that carry a message, 1 MB (the default) everywhere else, including email/search and the scheduled endpoints.
[EndpointTraits("email/send", MaxBodyBytes = Endpoint.EmailMaxBodyBytes)]
[EndpointTraits("email/mime", MaxBodyBytes = Endpoint.EmailMaxBodyBytes)]
[EndpointTraits("email/batch", MaxBodyBytes = Endpoint.EmailMaxBodyBytes)]
public static partial class EndpointTable
{
    private static readonly Dictionary<(string Path, string Method), Endpoint> s_endpoints = Seed();

    /// <summary>Every registered descriptor.</summary>
    public static IReadOnlyCollection<Endpoint> All => s_endpoints.Values;

    /// <summary>
    /// Returns the descriptor for <paramref name="path"/>: the <c>POST</c> one when the path is registered with several methods, otherwise the only
    /// registered one, or <see cref="CreateDefault"/> when the path is not registered.
    /// </summary>
    public static Endpoint Get(string path)
    {
        return TryGet(path, out Endpoint? endpoint) ? endpoint : CreateDefault(path);
    }

    /// <summary>Returns the descriptor registered for <paramref name="path"/> and <paramref name="method"/>, or <see cref="CreateDefault"/> with that method when none is.</summary>
    public static Endpoint Get(string path, HttpMethod method)
    {
        Argument.ThrowIfNull(method);
        return TryGet(path, method, out Endpoint? endpoint) ? endpoint : CreateDefault(path) with { Method = method };
    }

    /// <summary>Returns the registered descriptor for <paramref name="path"/>, if any: the <c>POST</c> one when several methods are registered, otherwise the only one.</summary>
    public static bool TryGet(string path, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out Endpoint? endpoint)
    {
        Argument.ThrowIfNullOrWhiteSpace(path);
        string normalized = Normalize(path);
        if (s_endpoints.TryGetValue((normalized, HttpMethod.Post.Method), out endpoint))
        {
            return true;
        }

        foreach (KeyValuePair<(string Path, string Method), Endpoint> entry in s_endpoints)
        {
            if (string.Equals(entry.Key.Path, normalized, StringComparison.Ordinal))
            {
                endpoint = entry.Value;
                return true;
            }
        }

        endpoint = null;
        return false;
    }

    /// <summary>Returns the descriptor registered for <paramref name="path"/> and <paramref name="method"/>, if any.</summary>
    public static bool TryGet(string path, HttpMethod method, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out Endpoint? endpoint)
    {
        Argument.ThrowIfNullOrWhiteSpace(path);
        Argument.ThrowIfNull(method);
        return s_endpoints.TryGetValue((Normalize(path), method.Method.ToUpperInvariant()), out endpoint);
    }

    /// <summary>
    /// The descriptor used for unregistered paths: <c>POST</c>, not idempotent, no <c>subaccount_id</c>, <see cref="RateLimitClass.None"/>,
    /// and the body limit for the path's family (50 MB under <c>email/</c>, 1 MB otherwise).
    /// </summary>
    public static Endpoint CreateDefault(string path)
    {
        Argument.ThrowIfNullOrWhiteSpace(path);
        string normalized = Normalize(path);
        long maxBody = normalized.StartsWith("email/", StringComparison.Ordinal) ? Endpoint.EmailMaxBodyBytes : Endpoint.DefaultMaxBodyBytes;
        return new Endpoint(normalized, HttpMethod.Post, Idempotent: false, AcceptsSubaccountId: false, RateLimitClass.None, maxBody);
    }

    /// <summary>Strips a leading slash so <c>/email/send</c> and <c>email/send</c> resolve to the same descriptor.</summary>
    internal static string Normalize(string path)
    {
        string trimmed = path.Trim();
        return trimmed.Length > 0 && trimmed[0] == '/' ? trimmed.Substring(1) : trimmed;
    }
}
