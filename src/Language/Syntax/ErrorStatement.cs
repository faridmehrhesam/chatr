namespace Chatr.Language.Syntax;

public sealed class ErrorStatement : IStatement
{
    public void Accept(IVisitor visitor)
    {
        visitor.VisitErrorStatement(this);
    }

    public T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitErrorStatement(this);
    }
}
