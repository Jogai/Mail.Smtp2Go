using System.Text;

namespace Scott.Mail.Smtp2Go.SpecHarvester;

/// <summary>Compares two <c>endpoints.json</c> documents and describes the differences in Markdown (the body of the drift pull request).</summary>
public static class SpecDiff
{
    /// <summary>Returns a Markdown report; <see cref="IsEmpty"/> tells whether it reports any change.</summary>
    public static string Markdown(EndpointsDocument before, EndpointsDocument after)
    {
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(after);

        StringBuilder sb = new();
        sb.Append("## SMTP2GO API spec drift\n\n");
        if (!string.Equals(before.ApiVersion, after.ApiVersion, StringComparison.Ordinal))
        {
            sb.Append(Invariant($"API version: `{before.ApiVersion ?? "?"}` -> `{after.ApiVersion ?? "?"}`\n\n"));
        }

        Dictionary<string, OperationSummary> oldOps = before.Operations.ToDictionary(o => o.Key, StringComparer.Ordinal);
        Dictionary<string, OperationSummary> newOps = after.Operations.ToDictionary(o => o.Key, StringComparer.Ordinal);

        List<string> added = newOps.Keys.Except(oldOps.Keys, StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList();
        List<string> removed = oldOps.Keys.Except(newOps.Keys, StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList();
        Section(sb, "Added operations", added.Select(k => Invariant($"`{k}` ({Describe(newOps[k])})")));
        Section(sb, "Removed operations", removed.Select(k => Invariant($"`{k}` ({Describe(oldOps[k])})")));

        List<string> changed = [];
        foreach (string key in oldOps.Keys.Intersect(newOps.Keys, StringComparer.Ordinal).Order(StringComparer.Ordinal))
        {
            List<string> changes = Compare(oldOps[key], newOps[key]);
            if (changes.Count > 0)
            {
                changed.Add(Invariant($"`{key}`\n") + string.Join('\n', changes.Select(c => "  - " + c)));
            }
        }

        Section(sb, "Changed operations", changed);

        List<string> callbackChanges = [];
        Dictionary<string, CallbackSummary> oldCallbacks = before.Callbacks.ToDictionary(c => c.Path, StringComparer.Ordinal);
        foreach (CallbackSummary callback in after.Callbacks)
        {
            if (!oldCallbacks.TryGetValue(callback.Path, out CallbackSummary? old))
            {
                callbackChanges.Add(Invariant($"`{callback.Path}` added"));
                continue;
            }

            callbackChanges.AddRange(ListChanges(callback.Path + " parameters", old.Parameters.Select(p => p.Name), callback.Parameters.Select(p => p.Name)));
            callbackChanges.AddRange(ListChanges(callback.Path + " events", old.Events, callback.Events));
            callbackChanges.AddRange(ListChanges(callback.Path + " event values", old.EventValues, callback.EventValues));
        }

        foreach (string path in oldCallbacks.Keys.Except(after.Callbacks.Select(c => c.Path), StringComparer.Ordinal))
        {
            callbackChanges.Add(Invariant($"`{path}` removed"));
        }

        Section(sb, "Callbacks", callbackChanges);

        List<string> pages = [];
        pages.AddRange(ListChanges("pages without a parsable OpenAPI block", before.UnparsedPages.Select(p => p.Page), after.UnparsedPages.Select(p => p.Page)));
        pages.AddRange(ListChanges("pages without any OpenAPI block", before.PagesWithoutSpec, after.PagesWithoutSpec));
        Section(sb, "Pages", pages);

        if (IsEmpty(sb.ToString()))
        {
            sb.Append("No differences in `endpoints.json`.\n");
        }

        return sb.ToString();
    }

    /// <summary>Whether a report produced by <see cref="Markdown"/> lists no change.</summary>
    public static bool IsEmpty(string report)
    {
        ArgumentNullException.ThrowIfNull(report);
        return !report.Contains("\n### ", StringComparison.Ordinal) && !report.Contains("API version:", StringComparison.Ordinal);
    }

    private static List<string> Compare(OperationSummary before, OperationSummary after)
    {
        List<string> changes = [];
        if (!string.Equals(before.Spec, after.Spec, StringComparison.Ordinal))
        {
            changes.Add(Invariant($"spec: {before.Spec} -> {after.Spec}"));
        }

        if (before.Deprecated != after.Deprecated)
        {
            changes.Add(Invariant($"deprecated: {before.Deprecated} -> {after.Deprecated}"));
        }

        if (before.AcceptsSubaccountId != after.AcceptsSubaccountId)
        {
            changes.Add(Invariant($"accepts subaccount_id: {before.AcceptsSubaccountId} -> {after.AcceptsSubaccountId}"));
        }

        if (!string.Equals(before.RateLimitNote?.Text, after.RateLimitNote?.Text, StringComparison.Ordinal))
        {
            changes.Add(Invariant($"rate limit: {before.RateLimitNote?.Text ?? "none"} -> {after.RateLimitNote?.Text ?? "none"}"));
        }

        if (!string.Equals(before.ResponseShape, after.ResponseShape, StringComparison.Ordinal))
        {
            changes.Add(Invariant($"response shape: {before.ResponseShape ?? "?"} -> {after.ResponseShape ?? "?"}"));
        }

        changes.AddRange(ListChanges("required", before.Required, after.Required));
        changes.AddRange(PropertyChanges("request property", before.RequestProperties, after.RequestProperties));
        changes.AddRange(PropertyChanges("response property", before.ResponseProperties, after.ResponseProperties));

        string? oldExample = before.ResponseExample?.ToJsonString();
        string? newExample = after.ResponseExample?.ToJsonString();
        if (!string.Equals(oldExample, newExample, StringComparison.Ordinal))
        {
            changes.Add("response example changed");
        }

        if (!string.Equals(before.DocsUpdatedAt, after.DocsUpdatedAt, StringComparison.Ordinal) && changes.Count == 0)
        {
            changes.Add(Invariant($"page updated ({before.DocsUpdatedAt ?? "?"} -> {after.DocsUpdatedAt ?? "?"}) without a summary-level change; see `smtp2go-v3.json`"));
        }

        return changes;
    }

    private static IEnumerable<string> PropertyChanges(string label, IReadOnlyList<PropertySummary> before, IReadOnlyList<PropertySummary> after)
    {
        Dictionary<string, PropertySummary> old = before.ToDictionary(p => p.Name, StringComparer.Ordinal);
        Dictionary<string, PropertySummary> current = after.ToDictionary(p => p.Name, StringComparer.Ordinal);
        foreach (string change in ListChanges(label, old.Keys, current.Keys))
        {
            yield return change;
        }

        foreach (string name in old.Keys.Intersect(current.Keys, StringComparer.Ordinal).Order(StringComparer.Ordinal))
        {
            if (old[name].Deprecated != current[name].Deprecated)
            {
                yield return Invariant($"{label} `{name}` deprecated: {old[name].Deprecated} -> {current[name].Deprecated}");
            }

            if (!string.Equals(old[name].Type, current[name].Type, StringComparison.Ordinal))
            {
                yield return Invariant($"{label} `{name}` type: {old[name].Type ?? "?"} -> {current[name].Type ?? "?"}");
            }
        }
    }

    private static IEnumerable<string> ListChanges(string label, IEnumerable<string> before, IEnumerable<string> after)
    {
        HashSet<string> old = new(before, StringComparer.Ordinal);
        HashSet<string> current = new(after, StringComparer.Ordinal);
        foreach (string added in current.Except(old, StringComparer.Ordinal).Order(StringComparer.Ordinal))
        {
            yield return Invariant($"{label} added: `{added}`");
        }

        foreach (string removed in old.Except(current, StringComparer.Ordinal).Order(StringComparer.Ordinal))
        {
            yield return Invariant($"{label} removed: `{removed}`");
        }
    }

    private static string Describe(OperationSummary op)
    {
        return op.Summary ?? op.OperationId ?? op.Page;
    }

    private static void Section(StringBuilder sb, string title, IEnumerable<string> items)
    {
        List<string> list = items.ToList();
        if (list.Count == 0)
        {
            return;
        }

        sb.Append("### ").Append(title).Append("\n\n");
        foreach (string item in list)
        {
            sb.Append("- ").Append(item).Append('\n');
        }

        sb.Append('\n');
    }

    private static string Invariant(FormattableString value) => FormattableString.Invariant(value);
}
