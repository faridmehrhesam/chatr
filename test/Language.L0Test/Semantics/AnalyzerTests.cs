using Chatr.Language.Diagnostics;
using Chatr.Language.Lexing;
using Chatr.Language.Parsing;
using Chatr.Language.Semantics;

namespace Chatr.Language.L0Test.Semantics;

public class AnalyzerTests
{
    private static DiagnosticEngine Analyze(string src)
    {
        var lex = Lexer.Tokenize(src.AsMemory());
        var parseDiags = new DiagnosticEngine();
        var unit = Parser.Parse(lex, parseDiags);
        Assert.False(parseDiags.HasErrors, $"unexpected parse errors in: {src}");

        var semaDiags = new DiagnosticEngine();
        Analyzer.Analyze(unit, semaDiags);
        return semaDiags;
    }

    [Fact]
    public void TestAnalyzer_WhenValidTable_HasNoErrors()
    {
        var diags = Analyze("create table foo(x: string);");
        Assert.False(diags.HasErrors);
    }

    [Fact]
    public void TestAnalyzer_WhenValidMutTable_HasNoErrors()
    {
        var diags = Analyze("create mut table foo(x: string, y: string);");
        Assert.False(diags.HasErrors);
    }

    [Fact]
    public void TestAnalyzer_WhenDuplicateTableName_EmitsError()
    {
        var diags = Analyze("create table foo(x: string); create table foo(y: string);");
        Assert.True(diags.HasErrors);
        var msg = diags.Diagnostics[0].Message;
        Assert.Contains("Duplicate table name", msg);
        Assert.Contains("foo", msg);
    }

    [Fact]
    public void TestAnalyzer_WhenDuplicateTableName_ReportedOnce()
    {
        var diags = Analyze("create table foo(x: string); create table foo(y: string);");
        Assert.Equal(1, diags.ErrorCount);
    }

    [Fact]
    public void TestAnalyzer_WhenEmptyColumnList_EmitsError()
    {
        // Parser allows empty list; analyzer must reject it.
        const string emptySrc = "create table foo();";
        var parseDiags = new DiagnosticEngine();
        var unit = Parser.Parse(Lexer.Tokenize(emptySrc.AsMemory()), parseDiags);
        // Parser emits no error for empty list — verify that assumption.
        Assert.False(parseDiags.HasErrors);

        var semaDiags = new DiagnosticEngine();
        Analyzer.Analyze(unit, semaDiags);
        Assert.True(semaDiags.HasErrors);
        Assert.Contains("at least one column", semaDiags.Diagnostics[0].Message);
    }

    [Fact]
    public void TestAnalyzer_WhenDuplicateColumnName_EmitsError()
    {
        var diags = Analyze("create table foo(x: string, x: string);");
        Assert.True(diags.HasErrors);
        var msg = diags.Diagnostics[0].Message;
        Assert.Contains("Duplicate column name", msg);
        Assert.Contains("x", msg);
    }

    [Fact]
    public void TestAnalyzer_WhenSameColumnNameInDifferentTables_NoError()
    {
        var diags = Analyze("create table a(x: string); create table b(x: string);");
        Assert.False(diags.HasErrors);
    }

    [Fact]
    public void TestAnalyzer_WhenMultipleUniqueColumns_HasNoErrors()
    {
        var diags = Analyze("create table foo(x: string, y: string, z: string);");
        Assert.False(diags.HasErrors);
    }

    [Fact]
    public void TestAnalyzer_WhenErrorStmtPresent_DoesNotCascadeFalsePositives()
    {
        // Parse a source that has a parse error then a valid table.
        // The analyzer must not emit spurious errors for the ErrorStmt.
        const string blahSrc = "blah; create table foo(x: string);";
        var parseDiags = new DiagnosticEngine();
        var unit = Parser.Parse(Lexer.Tokenize(blahSrc.AsMemory()), parseDiags);
        Assert.True(parseDiags.HasErrors); // parse error expected

        var semaDiags = new DiagnosticEngine();
        Analyzer.Analyze(unit, semaDiags);
        Assert.False(semaDiags.HasErrors); // semantic pass should be clean
    }

    [Fact]
    public void TestAnalyzer_WhenAllColumnsHaveErrorType_EmitsAtLeastOneColumnError()
    {
        // Parser emits error for the unknown type, leaving one TypeKind.Error column.
        // The analyzer must still reject the table — no well-typed columns exist.
        const string src = "create table foo(x: badtype);";
        var parseDiags = new DiagnosticEngine();
        var unit = Parser.Parse(Lexer.Tokenize(src.AsMemory()), parseDiags);
        Assert.True(parseDiags.HasErrors);

        var semaDiags = new DiagnosticEngine();
        Analyzer.Analyze(unit, semaDiags);
        Assert.True(semaDiags.HasErrors);
        Assert.Contains("at least one column", semaDiags.Diagnostics[0].Message);
    }

    [Fact]
    public void TestAnalyzer_WhenErrorTypedColumn_DoesNotCauseSpuriousDuplicateError()
    {
        // x: badtype causes TypeKind.Error; x: string that follows is valid, not a duplicate.
        const string badTypeSrc = "create table foo(x: badtype, x: string);";
        var parseDiags = new DiagnosticEngine();
        var unit = Parser.Parse(Lexer.Tokenize(badTypeSrc.AsMemory()), parseDiags);
        // Parser emits an error for the bad type — expected.
        Assert.True(parseDiags.HasErrors);

        var semaDiags = new DiagnosticEngine();
        Analyzer.Analyze(unit, semaDiags);
        // Analyzer must NOT emit "duplicate column name" — the first x had a parse error.
        Assert.False(semaDiags.HasErrors);
    }

    [Fact]
    public void TestAnalyzer_WhenSourcePassedToAnalyze_SourceAndUnitArePaired()
    {
        var src = "create table dup(x: string); create table dup(y: string);";
        var parseDiags = new DiagnosticEngine();
        var unit = Parser.Parse(Lexer.Tokenize(src.AsMemory()), parseDiags);

        var semaDiags = new DiagnosticEngine();
        Analyzer.Analyze(unit, semaDiags);
        Assert.True(semaDiags.HasErrors);
        Assert.Contains("dup", semaDiags.Diagnostics[0].Message);
    }

    [Fact]
    public void TestAnalyzer_WhenCalledWithMemory_WorksLikeString()
    {
        var src = "create table foo(x: string, y: string);";
        var parseDiags = new DiagnosticEngine();
        var unit = Parser.Parse(Lexer.Tokenize(src.AsMemory()), parseDiags);

        var semaDiags = new DiagnosticEngine();
        Analyzer.Analyze(unit, semaDiags);
        Assert.False(semaDiags.HasErrors);
    }

    // --- Tests for the new API where source is owned by CompilationUnit ---

    [Fact]
    public void CompilationUnit_Source_MatchesLexedText()
    {
        var src = "create table foo(x: string);";
        var unit = Parser.Parse(Lexer.Tokenize(src.AsMemory()), new DiagnosticEngine());

        Assert.Equal(src, unit.Source.ToString());
    }

    [Fact]
    public void Analyzer_Analyze_WithoutSourceParam_UsesUnitSource()
    {
        var src = "create table dup(x: string); create table dup(y: string);";
        var unit = Parser.Parse(Lexer.Tokenize(src.AsMemory()), new DiagnosticEngine());

        var semaDiags = new DiagnosticEngine();
        Analyzer.Analyze(unit, semaDiags);

        Assert.True(semaDiags.HasErrors);
        Assert.Contains("dup", semaDiags.Diagnostics[0].Message);
    }
}
