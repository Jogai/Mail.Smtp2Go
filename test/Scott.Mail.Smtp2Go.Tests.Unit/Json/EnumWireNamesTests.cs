using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Scott.Mail.Smtp2Go.Json;
using Scott.Mail.Smtp2Go.Json.Converters;

namespace Scott.Mail.Smtp2Go.Tests.Unit.Json;

/// <summary>
/// The generated <see cref="EnumWireNames"/> tables against what reflection and the real <see cref="JsonNamingPolicy.SnakeCaseLower"/> say:
/// the generator re-implements the naming policy, so every library enum is compared member by member.
/// </summary>
public class EnumWireNamesTests
{
    private static IEnumerable<Type> TolerantEnums => typeof(Smtp2GoClient).Assembly.GetTypes().Where(IsTolerantEnum).OrderBy(t => t.FullName, StringComparer.Ordinal);

    public static TheoryData<Type> LibraryEnums()
    {
        TheoryData<Type> data = [];
        foreach (Type type in TolerantEnums)
        {
            data.Add(type);
        }

        return data;
    }

    [Fact]
    public void Every_tolerant_enum_of_the_library_has_a_generated_table()
    {
        TolerantEnums.Should().NotBeEmpty();
        foreach (Type type in TolerantEnums)
        {
            Get(type).Should().NotBeNull(because: "{0} is read with TolerantEnumConverter and must be in the generated tables", type.Name);
        }
    }

    [Theory]
    [MemberData(nameof(LibraryEnums))]
    public void Generated_table_matches_reflection_and_the_naming_policy(Type enumType)
    {
        Array table = Get(enumType)!;
        List<(object Value, string Name, string WireName)> expected = [];
        foreach (FieldInfo field in enumType.GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            string wireName = field.GetCustomAttribute<JsonStringEnumMemberNameAttribute>()?.Name ?? JsonNamingPolicy.SnakeCaseLower.ConvertName(field.Name);
            expected.Add((field.GetValue(null)!, field.Name, wireName));
        }

        List<(object Value, string Name, string WireName)> actual = [];
        foreach (object member in table)
        {
            Type memberType = member.GetType();
            actual.Add((memberType.GetProperty("Value")!.GetValue(member)!, (string)memberType.GetProperty("Name")!.GetValue(member)!, (string)memberType.GetProperty("WireName")!.GetValue(member)!));
        }

        actual.Should().Equal(expected);
    }

    [Fact]
    public void Enums_from_other_assemblies_are_not_in_the_tables()
    {
        EnumWireNames.Get<ProbeEvent>().Should().BeNull();
        EnumWireNames.Get<DayOfWeek>().Should().BeNull();
    }

    private static bool IsTolerantEnum(Type type)
    {
        return type.IsEnum
            && type.GetCustomAttribute<JsonConverterAttribute>()?.ConverterType is { IsGenericType: true } converter
            && converter.GetGenericTypeDefinition() == typeof(TolerantEnumConverter<>)
            && converter.GetGenericArguments()[0] == type;
    }

    private static Array? Get(Type enumType)
    {
        return (Array?)typeof(EnumWireNames).GetMethod(nameof(EnumWireNames.Get))!.MakeGenericMethod(enumType).Invoke(null, null);
    }
}
