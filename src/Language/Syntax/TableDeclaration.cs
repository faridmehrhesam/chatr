namespace Chatr.Language.Syntax;

public sealed class TableDeclaration : ISyntaxNode
{
    public required Span Name { get; init; }
    public required bool IsMutable { get; init; }
    public required IReadOnlyList<ColumnDefinition> Columns { get; init; }

    public void Accept(IVisitor visitor)
    {
        visitor.VisitTableDeclaration(this);
    }

    public T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitTableDeclaration(this);
    }
}
