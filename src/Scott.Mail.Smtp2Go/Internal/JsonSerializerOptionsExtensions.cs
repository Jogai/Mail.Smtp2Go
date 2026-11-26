#if !NET11_0_OR_GREATER
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Scott.Mail.Smtp2Go;

/// <summary>
/// <c>JsonSerializerOptions.GetTypeInfo&lt;T&gt;()</c> for the targets whose System.Text.Json predates it. Version 11 adds the generic
/// overload (in-box on net11.0, where CA2263 asks for it); netstandard2.0 and net8.0 use the 10.0.x package and net10.0 its in-box 10,
/// so they still need the cast. This file compiles for every target but net11.0.
/// </summary>
internal static class JsonSerializerOptionsExtensions
{
    public static JsonTypeInfo<T> GetTypeInfo<T>(this JsonSerializerOptions options) => (JsonTypeInfo<T>)options.GetTypeInfo(typeof(T));
}
#endif
