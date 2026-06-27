namespace Chatr.Language.Syntax;

public sealed class CreateTableStatement : IStatement
{
    public required TableDeclaration Table { get; init; }

    public void Accept(IVisitor visitor)
    {
        visitor.VisitCreateTableStatement(this);
    }

    public T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitCreateTableStatement(this);
    }
}
