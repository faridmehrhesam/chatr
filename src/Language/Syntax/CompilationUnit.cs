namespace Chatr.Language.Syntax;

public sealed class CompilationUnit : ISyntaxNode
{
    public required ReadOnlyMemory<char> Source { get; init; }
    public required IReadOnlyList<IStatement> Statements { get; init; }

    public void Accept(IVisitor visitor)
    {
        visitor.VisitCompilationUnit(this);
    }

    public T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitCompilationUnit(this);
    }
}
