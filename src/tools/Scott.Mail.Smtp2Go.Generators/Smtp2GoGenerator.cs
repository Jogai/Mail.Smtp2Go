using Microsoft.CodeAnalysis;

namespace Scott.Mail.Smtp2Go.Generators;

/// <summary>
/// The one generator the library projects reference. It adds the marker attributes to the compilation and runs the pipelines:
/// the endpoint table, the pass-through client methods, the enum wire names and the webhook event tables. Each pipeline only
/// produces output where its marker is used, so a project that uses none of them gets just the attributes.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class Smtp2GoGenerator : IIncrementalGenerator
{
    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        GenerationAttributes.Register(context);
        IncrementalValueProvider<EndpointSnapshot> snapshot = EndpointSnapshot.Provide(context);
        EndpointTablePipeline.Register(context, snapshot);
        ClientPipeline.Register(context, snapshot);
        EnumWireNamesPipeline.Register(context);
    }
}
