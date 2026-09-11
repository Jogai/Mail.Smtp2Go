namespace Scott.Mail.Smtp2Go;

/// <summary>The <c>dedicated_ips/*</c> family: the dedicated IP pools of the account. The endpoint does not take <c>subaccount_id</c>.</summary>
public interface IDedicatedIpClient
{
    /// <summary><c>POST /dedicated_ips/view</c>: every pool with its addresses. Pool ids are what <c>ip_pool</c> takes on API keys, SMTP users and authenticated IPs.</summary>
    /// <exception cref="Smtp2GoApiException">The API answered with a non-success status.</exception>
    Task<ApiResponse<IReadOnlyList<DedicatedIpPool>>> ViewAsync(RequestOptions? options = null, CancellationToken cancellationToken = default);
}
