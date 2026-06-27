namespace Chatr.Language.Syntax;

public interface ISyntaxNode
{
    void Accept(IVisitor visitor);
    T Accept<T>(IVisitor<T> visitor);
}
