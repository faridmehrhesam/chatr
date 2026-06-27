namespace Chatr.Language.Syntax;

public interface IVisitor
{
    void Visit(ISyntaxNode node);
    void VisitCompilationUnit(CompilationUnit node);
    void VisitCreateTableStatement(CreateTableStatement node);
    void VisitErrorStatement(ErrorStatement node);
    void VisitTableDeclaration(TableDeclaration node);
    void VisitColumnDefinition(ColumnDefinition node);
}

public interface IVisitor<out T>
{
    T Visit(ISyntaxNode node);
    T VisitCompilationUnit(CompilationUnit node);
    T VisitCreateTableStatement(CreateTableStatement node);
    T VisitErrorStatement(ErrorStatement node);
    T VisitTableDeclaration(TableDeclaration node);
    T VisitColumnDefinition(ColumnDefinition node);
}
