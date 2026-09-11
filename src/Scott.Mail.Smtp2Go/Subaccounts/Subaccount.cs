using System.Text.Json;
using System.Text.Json.Serialization;
using Scott.Mail.Smtp2Go.Json;

namespace Scott.Mail.Smtp2Go;

/// <summary>
/// One subaccount as returned by <c>subaccount/add</c>, <c>subaccount/edit</c> and <c>subaccounts/search</c>. The search rows carry the name,
/// email, id, plan figures, state and dedicated-IP flag; add and edit also return the archiving, 2FA and SMS settings.
/// </summary>
public sealed record Subaccount
{
    /// <summary>The subaccount's id, used as <c>subaccount_id</c> on other endpoints.</summary>
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    /// <summary>The full name.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    /// <summary>The email address of the first team member (search rows only).</summary>
    [JsonPropertyName("email")]
    public string? Email { get; init; }

    /// <summary>Emails allowed per billing cycle.</summary>
    [JsonPropertyName("plan_size")]
    public long? PlanSize { get; init; }

    /// <summary>Emails sent this cycle.</summary>
    [JsonPropertyName("plan_used")]
    public long? PlanUsed { get; init; }

    /// <summary>Emails left this cycle.</summary>
    [JsonPropertyName("plan_remaining")]
    public long? PlanRemaining { get; init; }

    /// <summary>The <c>state</c> exactly as returned (<c>Active</c>, <c>Closed</c>, ...).</summary>
    [JsonPropertyName("state")]
    public string? StateRaw { get; init; }

    /// <summary><see cref="StateRaw"/> as a <see cref="SubaccountState"/>; <see cref="SubaccountState.Unknown"/> for values this library does not know.</summary>
    [JsonIgnore]
    public SubaccountState State => StateRaw is not null && EnumNameTable<SubaccountState>.TryParse(StateRaw, out SubaccountState parsed) ? parsed : SubaccountState.Unknown;

    /// <summary>Whether a dedicated IP is assigned.</summary>
    [JsonPropertyName("dedicated_ip")]
    public bool? DedicatedIp { get; init; }

    /// <summary>Whether the subaccount may enable archiving.</summary>
    [JsonPropertyName("archiving")]
    public bool? Archiving { get; init; }

    /// <summary>Whether team members must use two-factor authentication.</summary>
    [JsonPropertyName("enforce_2fa")]
    public bool? Enforce2fa { get; init; }

    /// <summary>Whether SMS messaging is enabled.</summary>
    [JsonPropertyName("sms_enabled")]
    public bool? SmsEnabled { get; init; }

    /// <summary>The monthly SMS limit.</summary>
    [JsonPropertyName("sms_limit")]
    public long? SmsLimit { get; init; }

    /// <summary>Any field this library does not model.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? Extra { get; set; }
}
