namespace Chatr.Language.Syntax;

public sealed class ColumnDefinition : ISyntaxNode
{
    public required Span Name { get; init; }
    public required TypeKind Type { get; init; }

    public void Accept(IVisitor visitor)
    {
        visitor.VisitColumnDefinition(this);
    }

    public T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitColumnDefinition(this);
    }
}
