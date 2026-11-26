#if NET8_0_OR_GREATER && !NET11_0_OR_GREATER
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Scott.Mail.Smtp2Go;

/// <summary>
/// <c>JsonSerializerOptions.GetTypeInfo&lt;T&gt;()</c> for the targets whose System.Text.Json predates it. Version 11 adds the generic
/// overload (in-box on net11.0, the package on netstandard2.0, where CA2263 then asks for it); net8.0 (package 10.0.x) and net10.0 (in-box 10)
/// still need the cast. Where the instance method exists it takes precedence, so this file compiles only for those two targets.
/// </summary>
internal static class JsonSerializerOptionsExtensions
{
    public static JsonTypeInfo<T> GetTypeInfo<T>(this JsonSerializerOptions options) => (JsonTypeInfo<T>)options.GetTypeInfo(typeof(T));
}
#endif
