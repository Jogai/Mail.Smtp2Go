using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Scott.Mail.Smtp2Go.Generators;

/// <summary>A source location in a form that is cheap to compare, so pipeline records that carry one stay cacheable.</summary>
internal sealed record LocationInfo(string FilePath, TextSpan Span, LinePositionSpan Lines)
{
    public static LocationInfo? From(SyntaxNode? node)
    {
        if (node is null)
        {
            return null;
        }

        FileLinePositionSpan lineSpan = node.GetLocation().GetLineSpan();
        return new LocationInfo(node.SyntaxTree.FilePath, node.Span, lineSpan.Span);
    }

    public static LocationInfo? From(ISymbol symbol)
    {
        return From(symbol.DeclaringSyntaxReferences.Length > 0 ? symbol.DeclaringSyntaxReferences[0].GetSyntax() : null);
    }

    public Location ToLocation()
    {
        return Location.Create(FilePath, Span, Lines);
    }
}
