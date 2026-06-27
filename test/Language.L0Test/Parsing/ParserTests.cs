using Chatr.Language.Diagnostics;
using Chatr.Language.Lexing;
using Chatr.Language.Parsing;
using Chatr.Language.Syntax;

namespace Chatr.Language.L0Test.Parsing;

public class ParserTests
{
    private static (CompilationUnit Unit, DiagnosticEngine Diags) Parse(string src)
    {
        var lex = Lexer.Tokenize(src.AsMemory());
        var diags = new DiagnosticEngine();
        return (Parser.Parse(lex, diags), diags);
    }

    [Fact]
    public void TestParser_WhenSimpleCreateTable_ParsesSuccessfully()
    {
        var (unit, diags) = Parse("create table foo(x: string);");
        Assert.False(diags.HasErrors);
        Assert.Single(unit.Statements);
        var stmt = Assert.IsType<CreateTableStatement>(unit.Statements[0]);
        Assert.False(stmt.Table.IsMutable);
        Assert.Single(stmt.Table.Columns);
        Assert.Equal(TypeKind.String, stmt.Table.Columns[0].Type);
    }

    [Fact]
    public void TestParser_WhenCreateMutTable_IsMutIsTrue()
    {
        var (unit, diags) = Parse("create mut table foo(x: string);");
        Assert.False(diags.HasErrors);
        var stmt = Assert.IsType<CreateTableStatement>(unit.Statements[0]);
        Assert.True(stmt.Table.IsMutable);
    }

    [Fact]
    public void TestParser_WhenMultipleColumns_AllColumnsParsed()
    {
        var (unit, diags) = Parse("create table foo(x: string, y: string);");
        Assert.False(diags.HasErrors);
        var stmt = Assert.IsType<CreateTableStatement>(unit.Statements[0]);
        Assert.Equal(2, stmt.Table.Columns.Count);
    }

    [Fact]
    public void TestParser_WhenMultipleStatements_AllStatementsParsed()
    {
        var (unit, diags) = Parse("create table a(x: string); create table b(y: string);");
        Assert.False(diags.HasErrors);
        Assert.Equal(2, unit.Statements.Count);
        Assert.IsType<CreateTableStatement>(unit.Statements[0]);
        Assert.IsType<CreateTableStatement>(unit.Statements[1]);
    }

    [Fact]
    public void TestParser_WhenEmptyColumnList_ParsesWithoutError()
    {
        // Parser allows empty lists; semantic analyzer enforces non-empty.
        var (unit, diags) = Parse("create table foo();");
        Assert.False(diags.HasErrors);
        var stmt = Assert.IsType<CreateTableStatement>(unit.Statements[0]);
        Assert.Empty(stmt.Table.Columns);
    }

    [Fact]
    public void TestParser_WhenTableNameMissing_ProducesError()
    {
        var (_, diags) = Parse("create table (x: string);");
        Assert.True(diags.HasErrors);
    }

    [Fact]
    public void TestParser_WhenOpenParenMissing_ProducesError()
    {
        var (_, diags) = Parse("create table foo x: string);");
        Assert.True(diags.HasErrors);
    }

    [Fact]
    public void TestParser_WhenSemicolonMissing_ProducesError()
    {
        var (_, diags) = Parse("create table foo(x: string)");
        Assert.True(diags.HasErrors);
    }

    [Fact]
    public void TestParser_WhenBadTypeUsed_ProducesError()
    {
        var (unit, diags) = Parse("create table foo(x: badtype);");
        Assert.True(diags.HasErrors);
        var stmt = Assert.IsType<CreateTableStatement>(unit.Statements[0]);
        Assert.Equal(TypeKind.Error, stmt.Table.Columns[0].Type);
    }

    [Fact]
    public void TestParser_WhenUnknownTokenAtStatementLevel_ProducesErrorStmt()
    {
        var (unit, diags) = Parse("blah; create table foo(x: string);");
        Assert.True(diags.HasErrors);
        Assert.IsType<ErrorStatement>(unit.Statements[0]);
        Assert.IsType<CreateTableStatement>(unit.Statements[1]);
    }

    [Fact]
    public void TestParser_WhenTableParsed_TableNameSpanIsCorrect()
    {
        var src = "create table foo(x: string);";
        var (unit, _) = Parse(src);
        var stmt = Assert.IsType<CreateTableStatement>(unit.Statements[0]);
        var name = src.Substring(stmt.Table.Name.Start, stmt.Table.Name.Length);
        Assert.Equal("foo", name);
    }

    [Fact]
    public void TestParser_WhenTableParsed_ColumnNameSpanIsCorrect()
    {
        var src = "create table foo(x: string);";
        var (unit, _) = Parse(src);
        var stmt = Assert.IsType<CreateTableStatement>(unit.Statements[0]);
        var col = stmt.Table.Columns[0];
        var colName = src.Substring(col.Name.Start, col.Name.Length);
        Assert.Equal("x", colName);
    }

    [Fact]
    public void TestParser_WhenBadTypeFollowedBySemicolon_ProducesErrors()
    {
        var (unit, diags) = Parse("create table foo(id: 123; bar: string);");
        Assert.Equal(3, diags.ErrorCount);
        var stmt = Assert.IsType<CreateTableStatement>(unit.Statements[0]);
        Assert.Single(stmt.Table.Columns);
        Assert.Equal(TypeKind.Error, stmt.Table.Columns[0].Type);
    }

    [Fact]
    public void TestParser_WhenColumnMissingColon_ErrorRecoveryParsesRemainingColumns()
    {
        // "x string" has no colon — ParseColumnDefinition returns null, triggering the
        // else-recovery branch in ParseColumnList before continuing to parse "y: string".
        var (unit, diags) = Parse("create table foo(x string, y: string);");
        Assert.True(diags.HasErrors);
        var stmt = Assert.IsType<CreateTableStatement>(unit.Statements[0]);
        var col = Assert.Single(stmt.Table.Columns);
        Assert.Equal(TypeKind.String, col.Type);
    }
}
