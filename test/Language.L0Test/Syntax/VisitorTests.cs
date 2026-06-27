using Chatr.Language.Diagnostics;
using Chatr.Language.Lexing;
using Chatr.Language.Parsing;
using Chatr.Language.Syntax;

namespace Chatr.Language.L0Test.Syntax;

public sealed class VisitorTests
{
    private sealed class RecordingVisitor : DefaultVisitor<string>
    {
        protected override string DefaultVisit(ISyntaxNode node)
        {
            return "default";
        }
    }

    private static CompilationUnit Parse(string src)
    {
        var lex = Lexer.Tokenize(src.AsMemory());
        var diags = new DiagnosticEngine();
        return Parser.Parse(lex, diags);
    }

    [Fact]
    public void Visit_DispatchesToCorrectMethod()
    {
        var unit = Parse("create table Foo (x: string);");
        var visitor = new RecordingVisitor();

        Assert.Equal("default", visitor.Visit(unit));
    }

    [Fact]
    public void VisitCompilationUnit_FallsBackToDefault()
    {
        var unit = Parse("create table Foo (x: string);");
        var visitor = new RecordingVisitor();

        Assert.Equal("default", visitor.VisitCompilationUnit(unit));
    }

    [Fact]
    public void VisitCreateTableStatement_FallsBackToDefault()
    {
        var unit = Parse("create table Foo (x: string);");
        var stmt = Assert.IsType<CreateTableStatement>(unit.Statements[0]);
        var visitor = new RecordingVisitor();

        Assert.Equal("default", visitor.VisitCreateTableStatement(stmt));
    }

    [Fact]
    public void VisitErrorStatement_FallsBackToDefault()
    {
        var unit = Parse("bad;");
        var stmt = Assert.IsType<ErrorStatement>(unit.Statements[0]);
        var visitor = new RecordingVisitor();

        Assert.Equal("default", visitor.VisitErrorStatement(stmt));
    }

    [Fact]
    public void VisitTableDeclaration_FallsBackToDefault()
    {
        var unit = Parse("create table Foo (x: string);");
        var tableDecl = Assert.IsType<CreateTableStatement>(unit.Statements[0]).Table;
        var visitor = new RecordingVisitor();

        Assert.Equal("default", visitor.VisitTableDeclaration(tableDecl));
    }

    [Fact]
    public void VisitColumnDefinition_FallsBackToDefault()
    {
        var unit = Parse("create table Foo (x: string);");
        var col = Assert.IsType<CreateTableStatement>(unit.Statements[0]).Table.Columns[0];
        var visitor = new RecordingVisitor();

        Assert.Equal("default", visitor.VisitColumnDefinition(col));
    }
}
