using Chatr.Language.Lexing;

namespace Chatr.Language.L0Test.Lexing;

public class LexerTests
{
    private static List<TokenKind> Kinds(string src)
    {
        return [.. Lexer.Tokenize(src.AsMemory()).Tokens.Select(t => t.Kind)];
    }

    [Fact]
    public void TestLexer_WhenInputIsEmpty_ReturnsOnlyEof()
    {
        Assert.Equal([TokenKind.Eof], Kinds(""));
    }

    [Fact]
    public void TestLexer_WhenInputIsWhitespaceOnly_ReturnsOnlyEof()
    {
        Assert.Equal([TokenKind.Eof], Kinds("   \t\n"));
    }

    [Fact]
    public void TestLexer_WhenInputIsCommentOnly_ReturnsOnlyEof()
    {
        Assert.Equal([TokenKind.Eof], Kinds("// a comment"));
    }

    [Fact]
    public void TestLexer_WhenInputHasPunctuators_ReturnsCorrectKinds()
    {
        Assert.Equal(
            [
                TokenKind.LParen,
                TokenKind.RParen,
                TokenKind.Comma,
                TokenKind.Semi,
                TokenKind.Colon,
                TokenKind.Eof,
            ],
            Kinds("(),;: "));
    }

    [Fact]
    public void TestLexer_WhenCommentPrecedesToken_TokenIsLexed()
    {
        Assert.Equal([TokenKind.String, TokenKind.Eof], Kinds("// comment\nstring"));
    }

    [Theory]
    // "parser"/"pfn" omitted: TokenKind.Parser/Pfn don't exist yet — pre-existing gap, out of scope here.
    [InlineData("create", TokenKind.Create)]
    [InlineData("mut", TokenKind.Mut)]
    [InlineData("string", TokenKind.String)]
    [InlineData("table", TokenKind.Table)]
    public void TestLexer_WhenKeywordInput_ReturnsCorrectKind(string keyword, TokenKind expectedKind)
    {
        Assert.Equal([expectedKind, TokenKind.Eof], Kinds(keyword));
    }

    [Fact]
    public void TestLexer_WhenKeywordHasSuffix_LexedAsIdent()
    {
        Assert.Equal([TokenKind.Identifier, TokenKind.Eof], Kinds("stringx"));
    }

    [Fact]
    public void TestLexer_WhenInputHasIdentifiers_ReturnsIdentKinds()
    {
        Assert.Equal(
            [TokenKind.Identifier, TokenKind.Identifier, TokenKind.Identifier, TokenKind.Eof],
            Kinds("hello _world foo123"));
    }

    [Fact]
    public void TestLexer_WhenDoubleQuotedString_ReturnsStringLit()
    {
        Assert.Equal([TokenKind.StringLiteral, TokenKind.Eof], Kinds("\"hello\""));
    }

    [Fact]
    public void TestLexer_WhenSingleQuotedString_ReturnsStringLit()
    {
        Assert.Equal([TokenKind.StringLiteral, TokenKind.Eof], Kinds("'hello'"));
    }

    [Fact]
    public void TestLexer_WhenVerbatimString_ReturnsStringLit()
    {
        Assert.Equal([TokenKind.StringLiteral, TokenKind.Eof], Kinds("@\"hello\""));
    }

    [Fact]
    public void TestLexer_WhenMultilineDoubleQuotedString_ReturnsStringLit()
    {
        Assert.Equal([TokenKind.StringLiteral, TokenKind.Eof], Kinds("\"\"\"hello\"\"\""));
    }

    [Fact]
    public void TestLexer_WhenMultilineSingleQuotedString_ReturnsStringLit()
    {
        Assert.Equal([TokenKind.StringLiteral, TokenKind.Eof], Kinds("'''hello'''"));
    }

    [Fact]
    public void TestLexer_WhenStringLiteralPrecedesToken_BothLexed()
    {
        Assert.Equal(
            [TokenKind.StringLiteral, TokenKind.String, TokenKind.Eof],
            Kinds("\"hi\" string"));
    }

    [Fact]
    public void TestLexer_WhenStringIsUnterminated_ReturnsBadToken()
    {
        var tokens = Lexer.Tokenize("\"hello".AsMemory()).Tokens;
        Assert.Equal(TokenKind.Bad, tokens[0].Kind);
    }

    [Fact]
    public void TestLexer_WhenUnrecognizedAsciiChar_ReturnsBadToken()
    {
        var tokens = Lexer.Tokenize("@".AsMemory()).Tokens;
        Assert.Equal(TokenKind.Bad, tokens[0].Kind);
        Assert.Equal(0, tokens[0].Span.Start);
        Assert.Equal(1, tokens[0].Span.End);
    }

    [Fact]
    public void TestLexer_WhenUnrecognizedUnicodeChar_ReturnsBadToken()
    {
        var tokens = Lexer.Tokenize("ñ".AsMemory()).Tokens;
        Assert.Equal(TokenKind.Bad, tokens[0].Kind);
        Assert.Equal(1, tokens[0].Span.Length);
    }

    [Fact]
    public void TestLexer_WhenStringHasSimpleEscape_ReturnsStringLit()
    {
        Assert.Equal([TokenKind.StringLiteral, TokenKind.Eof], Kinds("\"he\\nllo\""));
    }

    [Fact]
    public void TestLexer_WhenStringHasUnicodeEscape_ReturnsStringLit()
    {
        Assert.Equal([TokenKind.StringLiteral, TokenKind.Eof], Kinds("\"\\u0041\""));
    }

    [Fact]
    public void TestLexer_WhenStringHasLargeUnicodeEscape_ReturnsStringLit()
    {
        Assert.Equal([TokenKind.StringLiteral, TokenKind.Eof], Kinds("\"\\U00000041\""));
    }

    [Fact]
    public void TestLexer_WhenStringHasHexEscape_ReturnsStringLit()
    {
        Assert.Equal([TokenKind.StringLiteral, TokenKind.Eof], Kinds("\"\\x41\""));
    }

    [Fact]
    public void TestLexer_WhenStringHasOctalEscape_ReturnsStringLit()
    {
        Assert.Equal([TokenKind.StringLiteral, TokenKind.Eof], Kinds("\"\\101\""));
    }

    [Fact]
    public void TestLexer_WhenStringHasInvalidEscape_ReturnsBadToken()
    {
        Assert.Equal(TokenKind.Bad, Lexer.Tokenize("\"\\q\"".AsMemory()).Tokens[0].Kind);
    }

    [Fact]
    public void TestLexer_WhenStringHasIncompleteUnicodeEscape_ReturnsBadToken()
    {
        Assert.Equal(TokenKind.Bad, Lexer.Tokenize("\"\\u41\"".AsMemory()).Tokens[0].Kind);
    }

    [Fact]
    public void TestLexer_WhenVerbatimStringHasDoubledQuote_ReturnsStringLit()
    {
        Assert.Equal([TokenKind.StringLiteral, TokenKind.Eof], Kinds("@\"he\"\"llo\""));
    }

    [Fact]
    public void TestLexer_WhenMultilineStringIsUnterminated_ReturnsBadToken()
    {
        Assert.Equal(TokenKind.Bad, Lexer.Tokenize("@\"\"\"hello".AsMemory()).Tokens[0].Kind);
    }

    [Fact]
    public void TestLexer_WhenNonVerbatimMultilineStringIsUnterminated_ReturnsBadToken()
    {
        Assert.Equal(TokenKind.Bad, Lexer.Tokenize("\"\"\"hello".AsMemory()).Tokens[0].Kind);
    }

    [Fact]
    public void Tokenize_ReturnsLexResult_WithMatchingSource()
    {
        var src = "create table foo(x: string);";
        var result = Lexer.Tokenize(src.AsMemory());
        Assert.Equal(src, result.Source.ToString());
        Assert.NotEmpty(result.Tokens);
    }

    [Fact]
    public void Tokenize_ReturnsLexResult_WithImmutableTokenCollection()
    {
        var result = Lexer.Tokenize("".AsMemory());
        Assert.IsNotType<List<Token>>(result.Tokens);
    }

    [Fact]
    public void TestLexer_WhenVerbatimStringContainsNewline_ReturnsStringLit()
    {
        Assert.Equal([TokenKind.StringLiteral, TokenKind.Eof], Kinds("@\"hel\nlo\""));
    }

    [Fact]
    public void TestLexer_WhenCommentEndsWithCR_NextTokenIsLexed()
    {
        Assert.Equal([TokenKind.Identifier, TokenKind.Eof], Kinds("// comment\rworld"));
    }

    [Fact]
    public void TestLexer_WhenCommentEndsWithCRLF_NextTokenIsLexed()
    {
        Assert.Equal([TokenKind.Identifier, TokenKind.Eof], Kinds("// comment\r\nworld"));
    }
}
