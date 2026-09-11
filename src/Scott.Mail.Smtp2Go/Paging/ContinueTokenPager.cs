using System.Runtime.CompilerServices;

namespace Scott.Mail.Smtp2Go;

/// <summary>Walks a <c>continue_token</c> paged endpoint: fetches a page, yields its items, and repeats with the returned token until the token comes back null, empty or unchanged.</summary>
internal static class ContinueTokenPager
{
    /// <summary>A page: its items and the token for the next page (<see langword="null"/> or empty when this was the last page).</summary>
    public readonly record struct Page<TItem>(IReadOnlyList<TItem>? Items, string? ContinueToken);

    public static async IAsyncEnumerable<TItem> EnumerateAsync<TItem>(
        string? initialToken,
        Func<string?, CancellationToken, Task<Page<TItem>>> fetchPage,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        string? token = initialToken;
        while (true)
        {
            Page<TItem> page = await fetchPage(token, cancellationToken).ConfigureAwait(false);
            foreach (TItem item in page.Items ?? [])
            {
                yield return item;
            }

            string? next = page.ContinueToken;
            if (string.IsNullOrEmpty(next) || string.Equals(next, token, StringComparison.Ordinal))
            {
                yield break;
            }

            token = next;
        }
    }
}
