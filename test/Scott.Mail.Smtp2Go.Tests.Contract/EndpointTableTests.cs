using Scott.Mail.Smtp2Go.SpecHarvester;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go.Tests.Contract;

/// <summary>
/// <c>EndpointTable</c> against <c>endpoints.json</c>: every documented operation has a descriptor (or an allow-list entry naming why not yet),
/// every descriptor is documented, and the descriptor's flags match what the docs say.
/// </summary>
public class EndpointTableTests
{
    /// <summary>What each <see cref="RateLimitClass"/> promises. A new enum member must be added here, or <see cref="Every_rate_limit_class_is_mapped_here"/> fails.</summary>
    private static readonly Dictionary<RateLimitClass, (int Limit, string Per)> s_rateLimits = new()
    {
        [RateLimitClass.ActivitySearch] = (60, "minute"),
        [RateLimitClass.ApiKeyAdd] = (5, "minute"),
        [RateLimitClass.SubaccountAdd] = (50, "hour"),
        [RateLimitClass.EmailSearch] = (20, "minute"),
    };

    public static TheoryData<string> Operations() => Spec.OperationKeys();

    public static TheoryData<string> DescriptorPaths()
    {
        TheoryData<string> data = [];
        foreach (Endpoint endpoint in EndpointTable.All.OrderBy(e => e.Path, StringComparer.Ordinal))
        {
            data.Add(endpoint.Method.Method + " " + endpoint.Path);
        }

        return data;
    }

    [Fact]
    public void Snapshot_lists_the_whole_reference_section()
    {
        // comparison.md section 4 counts about 80 endpoints; the llms.txt reference section documents 69 operations on 69 pages (2026-09).
        Spec.Endpoints.Operations.Should().HaveCountGreaterThanOrEqualTo(69);
        Spec.Endpoints.Operations.Select(o => o.Path.Split('/')[0]).Distinct().Should().Contain(
            ["email", "stats", "webhook", "activity", "allowed_senders", "allowed_recipients", "api_keys", "dedicated_ips", "archive", "ip_auth", "domain", "single_sender_emails", "sms", "users", "subaccount", "subaccounts", "suppression", "template"]);
        Spec.Endpoints.Operations.Where(o => !o.IsParsed).Should().BeEmpty(because: "every page's OpenAPI block parsed at the last harvest; an unparsed page needs a look (see UnparsedPages in endpoints.json)");
    }

    [Theory]
    [MemberData(nameof(Operations))]
    public void Every_operation_has_a_descriptor_or_is_allow_listed(string key)
    {
        OperationSummary op = Spec.Operation(key);
        Endpoint? descriptor = Spec.Descriptor(op.Path, op.Method);
        KnownUnmodelledEntry? entry = Spec.Known.ForOperation(op.Path, op.Method);

        if (descriptor is null)
        {
            entry.Should().NotBeNull(because: "{0} is documented but EndpointTable has no descriptor for it; add one in EndpointTable.Seed() or an entry in docs/api-spec/known-unmodelled.json with a reason", key);
            entry!.Reason.Should().NotBeNullOrWhiteSpace();
        }
    }

    [Theory]
    [MemberData(nameof(DescriptorPaths))]
    public void Every_descriptor_is_documented(string key)
    {
        string[] parts = key.Split(' ', 2);
        Endpoint endpoint = EndpointTable.All.Single(e => e.Path == parts[1] && e.Method.Method == parts[0]);

        bool documented = Spec.Endpoints.Operations.Any(o => o.Path == endpoint.Path && string.Equals(o.Method, endpoint.Method.Method, StringComparison.OrdinalIgnoreCase));
        if (!documented)
        {
            Spec.Known.ForOperation(endpoint.Path, endpoint.Method.Method).Should().NotBeNull(
                because: "EndpointTable registers {0} but the docs do not list it; remove the descriptor or allow-list it with a reason", key);
        }
    }

    [Theory]
    [MemberData(nameof(Operations))]
    public void Descriptor_flags_match_the_docs(string key)
    {
        OperationSummary op = Spec.Operation(key);
        Endpoint? descriptor = Spec.Descriptor(op.Path, op.Method);
        if (descriptor is null)
        {
            Assert.Skip($"{key} has no descriptor yet (allow-listed).");
        }

        descriptor.Method.Method.Should().Be(op.Method, because: "the docs say {0} is {1}", op.Path, op.Method);
        descriptor.AcceptsSubaccountId.Should().Be(op.AcceptsSubaccountId, because: "the docs {0} list subaccount_id as a request property of {1}", op.AcceptsSubaccountId ? "do" : "do not", key);

        if (op.RateLimitNote is null)
        {
            descriptor.RateLimit.Should().Be(RateLimitClass.None, because: "the docs state no rate limit for {0}", key);
        }
        else
        {
            s_rateLimits.Should().ContainKey(descriptor.RateLimit, because: "the docs say {0} is '{1}' and the descriptor must carry a matching class", key, op.RateLimitNote.Text);
            s_rateLimits[descriptor.RateLimit].Should().Be((op.RateLimitNote.Limit, op.RateLimitNote.Per), because: "the docs say {0} is '{1}'", key, op.RateLimitNote.Text);
        }

        if (op.Path.StartsWith("email/", StringComparison.Ordinal))
        {
            // 50 MB for the sending endpoints (the ones that carry a message), 1 MB for search and remove (plan 03 deviation 7).
            bool carriesMessage = op.RequestProperties.Any(p => p.Name is "mime_email" or "emails" or "html_body");
            descriptor.MaxBodyBytes.Should().Be(carriesMessage ? Endpoint.EmailMaxBodyBytes : Endpoint.DefaultMaxBodyBytes, because: "{0} {1} a message body", key, carriesMessage ? "carries" : "does not carry");
        }
        else
        {
            descriptor.MaxBodyBytes.Should().Be(Endpoint.DefaultMaxBodyBytes, because: "the documented body limit outside email/* is 1 MB");
        }
    }

    [Fact]
    public void Every_rate_limit_class_is_mapped_here()
    {
        Enum.GetValues<RateLimitClass>().Where(c => c != RateLimitClass.None).Should().BeSubsetOf(s_rateLimits.Keys, because: "each RateLimitClass must state its limit in this test so Descriptor_flags_match_the_docs can compare it with the docs");
    }
}
