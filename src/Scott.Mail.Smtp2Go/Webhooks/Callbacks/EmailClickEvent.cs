namespace Scott.Mail.Smtp2Go.Webhooks;

/// <summary><c>click</c> (docs) or <c>clicked</c> (live): the recipient clicked a tracked link. Carries the open fields plus the link.</summary>
public sealed record EmailClickEvent : EmailOpenEvent
{
    /// <summary><c>link</c>: the tracking link that was clicked (observed live, not in the docs table).</summary>
    public string? Link { get; init; }

    /// <summary><c>click_url</c>: the destination URL of the clicked link (observed live, not in the docs table).</summary>
    public string? ClickUrl { get; init; }
}
