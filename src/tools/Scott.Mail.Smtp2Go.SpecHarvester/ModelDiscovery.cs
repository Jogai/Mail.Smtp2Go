using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go.SpecHarvester;

/// <summary>Whether an annotated model is the request body or the response <c>data</c> of its operation.</summary>
public enum ModelRole
{
    /// <summary>The request body.</summary>
    Request,

    /// <summary>The response <c>data</c> (used for endpoints without a request body).</summary>
    Response,
}

/// <summary>A model carrying <see cref="Smtp2GoEndpointAttribute"/>, with what reflection could learn about it.</summary>
/// <param name="Type">The model type.</param>
/// <param name="Path">The endpoint path from the attribute.</param>
/// <param name="Role">Whether the type is the request body or the response data.</param>
/// <param name="ResponseDataType">The <c>TData</c> of the <c>ApiResponse&lt;TData&gt;</c> the client method returns, when a client interface method was found (or the type itself for a response-role model).</param>
/// <param name="ClientMethod">The client interface method that takes or returns the model, if any.</param>
public sealed record AnnotatedModel(Type Type, string Path, ModelRole Role, Type? ResponseDataType, MethodInfo? ClientMethod);

/// <summary>
/// Finds the models that carry <see cref="Smtp2GoEndpointAttribute"/> in the core assembly and works out, from the public client interfaces,
/// which response type belongs to each. Shared by the coverage report and the contract tests so both see the library the same way.
/// </summary>
public static class ModelDiscovery
{
    /// <summary>The name of the abstract base of the webhook callback models (architecture.md section 4).</summary>
    public const string WebhookEventBaseName = "WebhookEvent";

    /// <summary>Every annotated model in <paramref name="assembly"/>, sorted by path then type name.</summary>
    public static IReadOnlyList<AnnotatedModel> Find(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        List<MethodInfo> clientMethods = ClientMethods(assembly);
        List<AnnotatedModel> models = [];

        foreach (Type type in assembly.GetTypes())
        {
            Smtp2GoEndpointAttribute? attribute = type.GetCustomAttribute<Smtp2GoEndpointAttribute>(inherit: false);
            if (attribute is null)
            {
                continue;
            }

            MethodInfo? taking = clientMethods.FirstOrDefault(m => m.GetParameters().Any(p => p.ParameterType == type));
            MethodInfo? returning = clientMethods.FirstOrDefault(m => ResponseDataType(m) is Type data && ElementTypeOrSelf(data) == type);

            if (taking is not null)
            {
                models.Add(new AnnotatedModel(type, attribute.Path, ModelRole.Request, ResponseDataType(taking), taking));
            }
            else if (returning is not null)
            {
                models.Add(new AnnotatedModel(type, attribute.Path, ModelRole.Response, ResponseDataType(returning), returning));
            }
            else if (type.Name.EndsWith("Request", StringComparison.Ordinal))
            {
                models.Add(new AnnotatedModel(type, attribute.Path, ModelRole.Request, null, null));
            }
            else
            {
                models.Add(new AnnotatedModel(type, attribute.Path, ModelRole.Response, type, null));
            }
        }

        return models.OrderBy(m => m.Path, StringComparer.Ordinal).ThenBy(m => m.Type.Name, StringComparer.Ordinal).ToList();
    }

    /// <summary>
    /// The wire names of <paramref name="modelType"/>'s serialised properties: <see cref="JsonPropertyNameAttribute"/> when present, otherwise
    /// the snake_case form the library's serializer context applies. Ignored and extension-data properties are excluded.
    /// </summary>
    public static IReadOnlyList<string> WireNames(Type modelType)
    {
        return SerializedProperties(modelType).Select(WireName).ToList();
    }

    /// <summary>The public instance properties that take part in serialisation, in declaration order.</summary>
    public static IReadOnlyList<PropertyInfo> SerializedProperties(Type modelType)
    {
        ArgumentNullException.ThrowIfNull(modelType);
        return modelType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.GetIndexParameters().Length == 0)
            .Where(p => p.GetCustomAttribute<JsonIgnoreAttribute>() is not { Condition: JsonIgnoreCondition.Always })
            .Where(p => p.GetCustomAttribute<JsonExtensionDataAttribute>() is null)
            .OrderBy(p => p.MetadataToken)
            .ToList();
    }

    /// <summary>The wire name of one property.</summary>
    public static string WireName(PropertyInfo property)
    {
        ArgumentNullException.ThrowIfNull(property);
        return property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? JsonNamingPolicy.SnakeCaseLower.ConvertName(property.Name);
    }

    /// <summary>Whether the property is a C# <c>required</c> member (or carries <see cref="JsonRequiredAttribute"/>).</summary>
    public static bool IsRequiredMember(PropertyInfo property)
    {
        ArgumentNullException.ThrowIfNull(property);
        return property.GetCustomAttribute<RequiredMemberAttribute>() is not null || property.GetCustomAttribute<JsonRequiredAttribute>() is not null;
    }

    /// <summary>The <c>[JsonExtensionData]</c> property of <paramref name="modelType"/>, if it has one.</summary>
    public static PropertyInfo? ExtensionDataProperty(Type modelType)
    {
        ArgumentNullException.ThrowIfNull(modelType);
        return modelType.GetProperties(BindingFlags.Public | BindingFlags.Instance).FirstOrDefault(p => p.GetCustomAttribute<JsonExtensionDataAttribute>() is not null);
    }

    /// <summary>For a collection type, its element type; otherwise the type itself. Strings are not collections here.</summary>
    public static Type ElementTypeOrSelf(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        if (type == typeof(string))
        {
            return type;
        }

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
        {
            return type.GetGenericArguments()[0];
        }

        Type? enumerable = type.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        return enumerable is null ? type : enumerable.GetGenericArguments()[0];
    }

    /// <summary>Whether the type is a JSON object model (not a primitive, string, JSON node or collection).</summary>
    public static bool IsObjectModel(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return !type.IsPrimitive
            && type != typeof(string)
            && type != typeof(decimal)
            && type != typeof(JsonElement)
            && !typeof(System.Text.Json.Nodes.JsonNode).IsAssignableFrom(type)
            && ElementTypeOrSelf(type) == type
            && !type.IsEnum;
    }

    /// <summary>The abstract base of the webhook callback models, once plan 04 adds it; <see langword="null"/> until then.</summary>
    public static Type? FindWebhookEventBase(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        return assembly.GetTypes().FirstOrDefault(t => string.Equals(t.Name, WebhookEventBaseName, StringComparison.Ordinal) && !t.IsNested);
    }

    /// <summary>Every concrete or abstract type derived from (or equal to) the webhook event base.</summary>
    public static IReadOnlyList<Type> WebhookEventTypes(Assembly assembly)
    {
        Type? baseType = FindWebhookEventBase(assembly);
        return baseType is null ? [] : assembly.GetTypes().Where(baseType.IsAssignableFrom).OrderBy(t => t.Name, StringComparer.Ordinal).ToList();
    }

    /// <summary>A readable type name: <c>IReadOnlyList&lt;ScheduledEmail&gt;</c> rather than <c>IReadOnlyList`1</c>.</summary>
    public static string FriendlyName(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        if (!type.IsGenericType)
        {
            return type.Name;
        }

        string name = type.Name[..type.Name.IndexOf('`', StringComparison.Ordinal)];
        return name + "<" + string.Join(", ", type.GetGenericArguments().Select(FriendlyName)) + ">";
    }

    /// <summary>The <c>TData</c> of a method returning <c>Task&lt;ApiResponse&lt;TData&gt;&gt;</c>, or <see langword="null"/>.</summary>
    public static Type? ResponseDataType(MethodInfo method)
    {
        ArgumentNullException.ThrowIfNull(method);
        Type returnType = method.ReturnType;
        if (returnType.IsGenericType && (returnType.GetGenericTypeDefinition() == typeof(Task<>) || returnType.GetGenericTypeDefinition() == typeof(ValueTask<>)))
        {
            returnType = returnType.GetGenericArguments()[0];
        }

        return returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(ApiResponse<>) ? returnType.GetGenericArguments()[0] : null;
    }

    private static List<MethodInfo> ClientMethods(Assembly assembly)
    {
        return assembly.GetTypes()
            .Where(t => t.IsInterface && t.IsPublic && t.Name.EndsWith("Client", StringComparison.Ordinal))
            .OrderBy(t => t.FullName, StringComparer.Ordinal)
            .SelectMany(t => t.GetMethods())
            .Where(m => ResponseDataType(m) is not null)
            .ToList();
    }
}
