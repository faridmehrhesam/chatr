using Chatr.Language.Diagnostics;
using Chatr.Language.Lexing;
using Chatr.Language.Parsing;
using Chatr.Language.Syntax;

namespace Chatr.Language.L0Test.Syntax;

public sealed class CompilationUnitTests
{
    private static CompilationUnit Parse(string src)
    {
        var lex = Lexer.Tokenize(src.AsMemory());
        var diags = new DiagnosticEngine();
        return Parser.Parse(lex, diags);
    }

    [Fact]
    public void CompilationUnit_Statements_DeclaredTypeIsIReadOnlyList()
    {
        var property = typeof(CompilationUnit).GetProperty(nameof(CompilationUnit.Statements))!;
        Assert.Equal(typeof(IReadOnlyList<IStatement>), property.PropertyType);
    }
}
