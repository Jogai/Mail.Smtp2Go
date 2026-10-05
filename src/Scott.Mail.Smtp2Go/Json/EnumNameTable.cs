using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Scott.Mail.Smtp2Go.Json;

/// <summary>
/// Wire names of an enum's members: the <see cref="JsonStringEnumMemberNameAttribute"/> value when present, otherwise the member name in
/// snake_case. Shared by <see cref="Converters.TolerantEnumConverter{TEnum}"/> and the error-code parser. The library's own enums come from the
/// generated <see cref="EnumWireNames"/> tables; an enum from another assembly is read by reflection, which is why <typeparamref name="TEnum"/>
/// keeps its trimming annotation.
/// </summary>
internal static class EnumNameTable<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] TEnum>
    where TEnum : struct, Enum
{
    private const string UnknownMemberName = "Unknown";

    private static readonly Dictionary<TEnum, string> s_wireNames = new();
    private static readonly Dictionary<string, TEnum> s_byName = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, TEnum> s_byNormalizedName = new(StringComparer.OrdinalIgnoreCase);
    private static readonly TEnum? s_unknown = Build();

    /// <summary>The member named <c>Unknown</c>, if the enum declares one.</summary>
    public static TEnum? Unknown => s_unknown;

    public static bool IsUnknown(TEnum value)
    {
        return s_unknown.HasValue && EqualityComparer<TEnum>.Default.Equals(value, s_unknown.Value);
    }

    public static string GetWireName(TEnum value)
    {
        return s_wireNames.TryGetValue(value, out string? name) ? name : value.ToString();
    }

    public static bool TryParse(string text, out TEnum value)
    {
        string trimmed = text.Trim();
        return s_byName.TryGetValue(trimmed, out value) || s_byNormalizedName.TryGetValue(Normalize(trimmed), out value);
    }

    /// <summary>Strips underscores, hyphens and spaces so <c>spam_complaint</c>, <c>SpamComplaint</c> and <c>spam-complaint</c> compare equal.</summary>
    private static string Normalize(string text)
    {
        char[] buffer = new char[text.Length];
        int length = 0;
        foreach (char c in text)
        {
            if (c != '_' && c != '-' && c != ' ')
            {
                buffer[length++] = c;
            }
        }

        return new string(buffer, 0, length);
    }

    private static TEnum? Build()
    {
        TEnum? unknown = null;
        if (EnumWireNames.Get<TEnum>() is { } generated)
        {
            foreach (EnumMember<TEnum> member in generated)
            {
                Register(member.Value, member.Name, member.WireName, ref unknown);
            }

            return unknown;
        }

        foreach (FieldInfo field in typeof(TEnum).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            TEnum value = (TEnum)field.GetValue(null)!;
            string wireName = field.GetCustomAttribute<JsonStringEnumMemberNameAttribute>()?.Name ?? JsonNamingPolicy.SnakeCaseLower.ConvertName(field.Name);
            Register(value, field.Name, wireName, ref unknown);
        }

        return unknown;
    }

    private static void Register(TEnum value, string name, string wireName, ref TEnum? unknown)
    {
        s_wireNames[value] = wireName;
        s_byName[wireName] = value;
        s_byName[name] = value;
        s_byNormalizedName[Normalize(wireName)] = value;
        s_byNormalizedName[Normalize(name)] = value;
        if (string.Equals(name, UnknownMemberName, StringComparison.Ordinal))
        {
            unknown = value;
        }
    }
}
