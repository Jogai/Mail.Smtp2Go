using Scott.Mail.Smtp2Go.SpecHarvester;

namespace Scott.Mail.Smtp2Go.Tests.Contract;

/// <summary>
/// The webhook callback parameters documented on the webhooks overview (<c>callbacks/email</c> and <c>callbacks/sms</c> in <c>endpoints.json</c>)
/// against the wire names of the <c>WebhookEvent</c> hierarchy. Until plan 04 adds that hierarchy, both pseudo-paths are allow-listed as a whole.
/// </summary>
public class CallbackModelTests
{
    private static readonly HashSet<string> s_eventWireNames = new(ModelDiscovery.WebhookEventTypes(Spec.Core).SelectMany(ModelDiscovery.WireNames), StringComparer.Ordinal);

    public static TheoryData<string, string> CallbackParameters()
    {
        TheoryData<string, string> data = [];
        foreach (CallbackSummary callback in Spec.Endpoints.Callbacks)
        {
            foreach (CallbackParameter parameter in callback.Parameters)
            {
                data.Add(callback.Path, parameter.Name);
            }
        }

        return data;
    }

    [Fact]
    public void Callback_tables_were_harvested()
    {
        Spec.Endpoints.Callbacks.Select(c => c.Path).Should().BeEquivalentTo([CallbackTables.EmailPath, CallbackTables.SmsPath]);
        CallbackSummary email = Spec.Endpoints.Callbacks.Single(c => c.Path == CallbackTables.EmailPath);
        email.Parameters.Select(p => p.Name).Should().Contain(["event", "email_id", "message-id", "rcpt", "bounce", "geoip-country"]);
        email.EventValues.Should().Contain(["processed", "delivered", "open", "click", "bounce", "spam", "unsubscribe", "resubscribe", "reject"]);
        CallbackSummary sms = Spec.Endpoints.Callbacks.Single(c => c.Path == CallbackTables.SmsPath);
        sms.Parameters.Select(p => p.Name).Should().Contain(["event", "destination_number", "message_id", "status_code"]);
        sms.EventValues.Should().Contain(["sms_delivered", "sms_failed"]);
    }

    [Theory]
    [MemberData(nameof(CallbackParameters))]
    public void Every_callback_parameter_is_modelled_or_allow_listed(string path, string parameter)
    {
        if (s_eventWireNames.Contains(parameter))
        {
            return;
        }

        bool allowListed = Spec.Known.ForCallbackField(path, parameter) is not null || Spec.Known.ForOperation(path, "CALLBACK") is not null;
        allowListed.Should().BeTrue(because: "{0} documents parameter '{1}' but no WebhookEvent type has a property with that wire name and known-unmodelled.json does not allow-list it", path, parameter);
    }

    [Fact]
    public void Whole_callback_allow_list_entries_are_deleted_once_the_hierarchy_exists()
    {
        if (ModelDiscovery.FindWebhookEventBase(Spec.Core) is null)
        {
            Assert.Skip("WebhookEvent is not in the assembly yet (plan 04).");
        }

        Spec.Known.Entries.Where(e => e.Scope is null && e.Endpoint.StartsWith("callbacks/", StringComparison.Ordinal)).Should().BeEmpty(
            because: "WebhookEvent exists now; delete the whole-callback entries from known-unmodelled.json and allow-list individual parameters (scope 'callback') with a reason instead");
    }
}
