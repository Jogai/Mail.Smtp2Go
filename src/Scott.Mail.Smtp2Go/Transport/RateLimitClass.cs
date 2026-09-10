namespace Scott.Mail.Smtp2Go.Transport;

/// <summary>Documented request-rate limits, keyed by endpoint so a resilience pipeline can throttle per class.</summary>
public enum RateLimitClass
{
    /// <summary>No documented limit.</summary>
    None = 0,

    /// <summary><c>activity/search</c>: 60 requests per minute.</summary>
    ActivitySearch = 1,

    /// <summary><c>api_keys/add</c>: 5 requests per minute.</summary>
    ApiKeyAdd = 2,

    /// <summary><c>subaccounts/add</c>: 50 requests per hour.</summary>
    SubaccountAdd = 3,

    /// <summary><c>email/search</c> (deprecated endpoint): 20 requests per minute.</summary>
    EmailSearch = 4,
}
