namespace Scott.Mail.Smtp2Go;

/// <summary>
/// The <c>subaccount/*</c> and <c>subaccounts/search</c> family: creating, changing, closing, reopening and listing the subaccounts of a master
/// account. Master-account keys only; none of these endpoints takes <c>subaccount_id</c>.
/// </summary>
public interface ISubaccountClient
{
    /// <summary><c>POST /subaccounts/search</c>: one page of subaccounts matching the filters. Use <see cref="SearchAllAsync"/> to walk every page.</summary>
    /// <exception cref="Smtp2GoValidationException">The request fails client-side validation; nothing was sent.</exception>
    /// <exception cref="Smtp2GoApiException">The API answered with a non-success status.</exception>
    Task<ApiResponse<SubaccountSearchResult>> SearchAsync(SubaccountSearchRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>Walks every page of <c>subaccounts/search</c> by <c>continue_token</c>, one API call per page as the enumeration advances.</summary>
    /// <exception cref="Smtp2GoValidationException">The request fails client-side validation; nothing was sent.</exception>
    /// <exception cref="Smtp2GoApiException">The API answered with a non-success status.</exception>
    IAsyncEnumerable<Subaccount> SearchAllAsync(SubaccountSearchRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary><c>POST /subaccount/add</c> (50 per hour): creates a subaccount and returns it.</summary>
    /// <exception cref="Smtp2GoValidationException">The request fails client-side validation; nothing was sent.</exception>
    /// <exception cref="Smtp2GoApiException">The API answered with a non-success status.</exception>
    Task<ApiResponse<Subaccount>> AddAsync(SubaccountAddRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary><c>POST /subaccount/edit</c>: changes a subaccount's details and returns it.</summary>
    /// <exception cref="Smtp2GoValidationException">The request fails client-side validation; nothing was sent.</exception>
    /// <exception cref="Smtp2GoApiException">The API answered with a non-success status.</exception>
    Task<ApiResponse<Subaccount>> EditAsync(SubaccountEditRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary><c>POST /subaccount/close</c>: closes an active subaccount. The response <c>data</c> is a message such as <c>Successfully closed subaccount test@example.com</c>.</summary>
    /// <exception cref="Smtp2GoValidationException">The request fails client-side validation; nothing was sent.</exception>
    /// <exception cref="Smtp2GoApiException">The API answered with a non-success status.</exception>
    Task<ApiResponse<string>> CloseAsync(SubaccountCloseRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary><c>POST /subaccount/reopen</c>: reopens a closed subaccount. The documented example returns a message string like <c>close</c> does (the schema says an object; see <c>docs/api-notes.md</c>).</summary>
    /// <exception cref="Smtp2GoValidationException">The request fails client-side validation; nothing was sent.</exception>
    /// <exception cref="Smtp2GoApiException">The API answered with a non-success status.</exception>
    Task<ApiResponse<string>> ReopenAsync(SubaccountReopenRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default);
}
