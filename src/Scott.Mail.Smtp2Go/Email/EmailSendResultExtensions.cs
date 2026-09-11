namespace Scott.Mail.Smtp2Go;

/// <summary>Convenience over the <c>/email/send</c> and <c>/email/mime</c> envelope.</summary>
public static class EmailSendResultExtensions
{
    /// <summary>Returns <see cref="ApiResponse{TData}.Data"/>, or throws <see cref="Smtp2GoSendException"/> carrying the failures and the request id.</summary>
    /// <exception cref="Smtp2GoSendException">The server reported failures.</exception>
    public static EmailSendResult EnsureAccepted(this ApiResponse<EmailSendResult> response)
    {
        Argument.ThrowIfNull(response);
        return response.Data.EnsureAccepted(response.RequestId);
    }
}
