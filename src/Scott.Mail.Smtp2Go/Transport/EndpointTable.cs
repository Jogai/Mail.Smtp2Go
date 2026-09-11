namespace Scott.Mail.Smtp2Go.Transport;

/// <summary>
/// Registry of known <see cref="Endpoint"/> descriptors, keyed by path and HTTP method: <c>api_keys/edit</c>, <c>users/smtp/edit</c> and
/// <c>ip_auth/edit</c> are documented with both <c>POST</c> (full edit) and <c>PATCH</c> (partial edit), so a path may carry two descriptors.
/// Unknown paths get a conservative default so the raw client can reach endpoints that have no descriptor yet. Each API family adds its rows to
/// <see cref="Seed"/> when its typed client lands.
/// </summary>
public static class EndpointTable
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

    // One Add per endpoint. Family plans append their rows here, grouped by family, in the order of comparison.md section 4.
    private static Dictionary<(string Path, string Method), Endpoint> Seed()
    {
        Dictionary<(string Path, string Method), Endpoint> table = [];

        // Email
        Add(table, new Endpoint("email/send", HttpMethod.Post, Idempotent: false, AcceptsSubaccountId: false, RateLimitClass.None, Endpoint.EmailMaxBodyBytes));
        Add(table, new Endpoint("email/mime", HttpMethod.Post, Idempotent: false, AcceptsSubaccountId: false, RateLimitClass.None, Endpoint.EmailMaxBodyBytes));
        Add(table, new Endpoint("email/batch", HttpMethod.Post, Idempotent: false, AcceptsSubaccountId: false, RateLimitClass.None, Endpoint.EmailMaxBodyBytes));
        Add(table, new Endpoint("email/search", HttpMethod.Post, Idempotent: true, AcceptsSubaccountId: false, RateLimitClass.EmailSearch, Endpoint.DefaultMaxBodyBytes));
        Add(table, new Endpoint("email/scheduled/search", HttpMethod.Post, Idempotent: true, AcceptsSubaccountId: false, RateLimitClass.None, Endpoint.DefaultMaxBodyBytes));
        Add(table, new Endpoint("email/scheduled/remove", HttpMethod.Post, Idempotent: false, AcceptsSubaccountId: false, RateLimitClass.None, Endpoint.DefaultMaxBodyBytes));

        // Webhooks (all four document subaccount_id)
        Add(table, new Endpoint("webhook/view", HttpMethod.Post, Idempotent: true, AcceptsSubaccountId: true, RateLimitClass.None, Endpoint.DefaultMaxBodyBytes));
        Add(table, new Endpoint("webhook/add", HttpMethod.Post, Idempotent: false, AcceptsSubaccountId: true, RateLimitClass.None, Endpoint.DefaultMaxBodyBytes));
        Add(table, new Endpoint("webhook/edit", HttpMethod.Post, Idempotent: false, AcceptsSubaccountId: true, RateLimitClass.None, Endpoint.DefaultMaxBodyBytes));
        Add(table, new Endpoint("webhook/remove", HttpMethod.Post, Idempotent: false, AcceptsSubaccountId: true, RateLimitClass.None, Endpoint.DefaultMaxBodyBytes));

        // Stats (none documents subaccount_id; email_history filters with a subaccounts[] field instead)
        Add(table, new Endpoint("stats/email_summary", HttpMethod.Post, Idempotent: true, AcceptsSubaccountId: false, RateLimitClass.None, Endpoint.DefaultMaxBodyBytes));
        Add(table, new Endpoint("stats/email_cycle", HttpMethod.Post, Idempotent: true, AcceptsSubaccountId: false, RateLimitClass.None, Endpoint.DefaultMaxBodyBytes));
        Add(table, new Endpoint("stats/email_bounces", HttpMethod.Post, Idempotent: true, AcceptsSubaccountId: false, RateLimitClass.None, Endpoint.DefaultMaxBodyBytes));
        Add(table, new Endpoint("stats/email_spam", HttpMethod.Post, Idempotent: true, AcceptsSubaccountId: false, RateLimitClass.None, Endpoint.DefaultMaxBodyBytes));
        Add(table, new Endpoint("stats/email_unsubs", HttpMethod.Post, Idempotent: true, AcceptsSubaccountId: false, RateLimitClass.None, Endpoint.DefaultMaxBodyBytes));
        Add(table, new Endpoint("stats/email_history", HttpMethod.Post, Idempotent: true, AcceptsSubaccountId: false, RateLimitClass.None, Endpoint.DefaultMaxBodyBytes));

        // Activity (filters by a subaccounts[] field, not subaccount_id; 60 requests per minute)
        Add(table, new Endpoint("activity/search", HttpMethod.Post, Idempotent: true, AcceptsSubaccountId: false, RateLimitClass.ActivitySearch, Endpoint.DefaultMaxBodyBytes));

        // Templates (the reference pages are template/add, template/edit, template/delete, template/search, template/view; none documents subaccount_id)
        Add(table, new Endpoint("template/add", HttpMethod.Post, Idempotent: false, AcceptsSubaccountId: false, RateLimitClass.None, Endpoint.DefaultMaxBodyBytes));
        Add(table, new Endpoint("template/edit", HttpMethod.Post, Idempotent: false, AcceptsSubaccountId: false, RateLimitClass.None, Endpoint.DefaultMaxBodyBytes));
        Add(table, new Endpoint("template/delete", HttpMethod.Post, Idempotent: false, AcceptsSubaccountId: false, RateLimitClass.None, Endpoint.DefaultMaxBodyBytes));
        Add(table, new Endpoint("template/search", HttpMethod.Post, Idempotent: true, AcceptsSubaccountId: false, RateLimitClass.None, Endpoint.DefaultMaxBodyBytes));
        Add(table, new Endpoint("template/view", HttpMethod.Post, Idempotent: true, AcceptsSubaccountId: false, RateLimitClass.None, Endpoint.DefaultMaxBodyBytes));

        // Suppressions (all three document subaccount_id)
        Add(table, new Endpoint("suppression/add", HttpMethod.Post, Idempotent: false, AcceptsSubaccountId: true, RateLimitClass.None, Endpoint.DefaultMaxBodyBytes));
        Add(table, new Endpoint("suppression/view", HttpMethod.Post, Idempotent: true, AcceptsSubaccountId: true, RateLimitClass.None, Endpoint.DefaultMaxBodyBytes));
        Add(table, new Endpoint("suppression/remove", HttpMethod.Post, Idempotent: false, AcceptsSubaccountId: true, RateLimitClass.None, Endpoint.DefaultMaxBodyBytes));

        // Archive (neither documents subaccount_id)
        Add(table, new Endpoint("archive/search", HttpMethod.Post, Idempotent: true, AcceptsSubaccountId: false, RateLimitClass.None, Endpoint.DefaultMaxBodyBytes));
        Add(table, new Endpoint("archive/email", HttpMethod.Post, Idempotent: true, AcceptsSubaccountId: false, RateLimitClass.None, Endpoint.DefaultMaxBodyBytes));

        // API keys (every endpoint but permissions documents subaccount_id; add is limited to 5 per minute; edit is documented as POST (full edit) and PATCH (partial edit))
        Add(table, new Endpoint("api_keys/view", HttpMethod.Post, Idempotent: true, AcceptsSubaccountId: true, RateLimitClass.None, Endpoint.DefaultMaxBodyBytes));
        Add(table, new Endpoint("api_keys/add", HttpMethod.Post, Idempotent: false, AcceptsSubaccountId: true, RateLimitClass.ApiKeyAdd, Endpoint.DefaultMaxBodyBytes));
        Add(table, new Endpoint("api_keys/edit", HttpMethod.Post, Idempotent: false, AcceptsSubaccountId: true, RateLimitClass.None, Endpoint.DefaultMaxBodyBytes));
        Add(table, new Endpoint("api_keys/edit", Endpoint.Patch, Idempotent: false, AcceptsSubaccountId: true, RateLimitClass.None, Endpoint.DefaultMaxBodyBytes));
        Add(table, new Endpoint("api_keys/remove", HttpMethod.Post, Idempotent: false, AcceptsSubaccountId: true, RateLimitClass.None, Endpoint.DefaultMaxBodyBytes));
        Add(table, new Endpoint("api_keys/permissions", HttpMethod.Post, Idempotent: true, AcceptsSubaccountId: false, RateLimitClass.None, Endpoint.DefaultMaxBodyBytes));

        // SMTP users (every endpoint documents subaccount_id; edit is documented as POST (full edit) and PATCH (partial edit))
        Add(table, new Endpoint("users/smtp/view", HttpMethod.Post, Idempotent: true, AcceptsSubaccountId: true, RateLimitClass.None, Endpoint.DefaultMaxBodyBytes));
        Add(table, new Endpoint("users/smtp/add", HttpMethod.Post, Idempotent: false, AcceptsSubaccountId: true, RateLimitClass.None, Endpoint.DefaultMaxBodyBytes));
        Add(table, new Endpoint("users/smtp/edit", HttpMethod.Post, Idempotent: false, AcceptsSubaccountId: true, RateLimitClass.None, Endpoint.DefaultMaxBodyBytes));
        Add(table, new Endpoint("users/smtp/edit", Endpoint.Patch, Idempotent: false, AcceptsSubaccountId: true, RateLimitClass.None, Endpoint.DefaultMaxBodyBytes));
        Add(table, new Endpoint("users/smtp/remove", HttpMethod.Post, Idempotent: false, AcceptsSubaccountId: true, RateLimitClass.None, Endpoint.DefaultMaxBodyBytes));

        // IP authentication (every endpoint documents subaccount_id; edit is documented as PATCH only)
        Add(table, new Endpoint("ip_auth/view", HttpMethod.Post, Idempotent: true, AcceptsSubaccountId: true, RateLimitClass.None, Endpoint.DefaultMaxBodyBytes));
        Add(table, new Endpoint("ip_auth/edit", Endpoint.Patch, Idempotent: false, AcceptsSubaccountId: true, RateLimitClass.None, Endpoint.DefaultMaxBodyBytes));
        Add(table, new Endpoint("ip_auth/remove", HttpMethod.Post, Idempotent: false, AcceptsSubaccountId: true, RateLimitClass.None, Endpoint.DefaultMaxBodyBytes));

        return table;
    }

    private static void Add(Dictionary<(string Path, string Method), Endpoint> table, Endpoint endpoint)
    {
        table.Add((endpoint.Path, endpoint.Method.Method.ToUpperInvariant()), endpoint);
    }
}
