namespace Scott.Mail.Smtp2Go.Json;

/// <summary>One enum member with its wire name, as the generated <see cref="EnumWireNames"/> tables list them.</summary>
/// <param name="Value">The member.</param>
/// <param name="Name">The member's C# name.</param>
/// <param name="WireName">The name the API uses: <see cref="System.Text.Json.Serialization.JsonStringEnumMemberNameAttribute"/> when present, otherwise the member name in snake_case.</param>
internal readonly record struct EnumMember<TEnum>(TEnum Value, string Name, string WireName)
    where TEnum : struct, Enum;
