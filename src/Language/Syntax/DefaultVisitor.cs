namespace Chatr.Language.Syntax;

public abstract class DefaultVisitor : IVisitor
{
    protected abstract void DefaultVisit(ISyntaxNode node);

    public void Visit(ISyntaxNode node)
    {
        node.Accept(this);
    }

    public virtual void VisitCompilationUnit(CompilationUnit node)
    {
        DefaultVisit(node);
    }

    public virtual void VisitCreateTableStatement(CreateTableStatement node)
    {
        DefaultVisit(node);
    }

    public virtual void VisitErrorStatement(ErrorStatement node)
    {
        DefaultVisit(node);
    }

    public virtual void VisitTableDeclaration(TableDeclaration node)
    {
        DefaultVisit(node);
    }

    public virtual void VisitColumnDefinition(ColumnDefinition node)
    {
        DefaultVisit(node);
    }
}

public abstract class DefaultVisitor<T> : IVisitor<T>
{
    protected abstract T DefaultVisit(ISyntaxNode node);

    public T Visit(ISyntaxNode node)
    {
        return node.Accept(this);
    }

    public virtual T VisitCompilationUnit(CompilationUnit node)
    {
        return DefaultVisit(node);
    }

    public virtual T VisitCreateTableStatement(CreateTableStatement node)
    {
        return DefaultVisit(node);
    }

    public virtual T VisitErrorStatement(ErrorStatement node)
    {
        return DefaultVisit(node);
    }

    public virtual T VisitTableDeclaration(TableDeclaration node)
    {
        return DefaultVisit(node);
    }

    public virtual T VisitColumnDefinition(ColumnDefinition node)
    {
        return DefaultVisit(node);
    }
}
