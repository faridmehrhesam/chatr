using Chatr.Language.Lexing;

namespace Chatr.Language.L0Test.Lexing;

public class TokenKindTests
{
    [Theory]
    [InlineData(TokenKind.LParen, "'('")]
    [InlineData(TokenKind.RParen, "')'")]
    [InlineData(TokenKind.Comma, "','")]
    [InlineData(TokenKind.Semi, "';'")]
    [InlineData(TokenKind.Colon, "':'")]
    [InlineData(TokenKind.Create, "'create'")]
    [InlineData(TokenKind.Mut, "'mut'")]
    [InlineData(TokenKind.String, "'string'")]
    [InlineData(TokenKind.Table, "'table'")]
    [InlineData(TokenKind.StringLiteral, "string literal")]
    [InlineData(TokenKind.Identifier, "identifier")]
    [InlineData(TokenKind.Bad, "unexpected character")]
    [InlineData(TokenKind.Eof, "end of file")]
    public void TestTokenKind_WhenDescribeCalled_ReturnsExpectedText(TokenKind kind, string expected)
    {
        Assert.Equal(expected, kind.Describe());
    }

    [Theory]
    [InlineData(TokenKind.LParen, false)]
    [InlineData(TokenKind.RParen, false)]
    [InlineData(TokenKind.Comma, false)]
    [InlineData(TokenKind.Semi, false)]
    [InlineData(TokenKind.Colon, false)]
    [InlineData(TokenKind.Create, false)]
    [InlineData(TokenKind.Mut, false)]
    [InlineData(TokenKind.String, false)]
    [InlineData(TokenKind.Table, false)]
    [InlineData(TokenKind.StringLiteral, false)]
    [InlineData(TokenKind.Identifier, false)]
    [InlineData(TokenKind.Bad, false)]
    [InlineData(TokenKind.Eof, true)]
    public void TestTokenKind_WhenIsEndOfFileCalled_TrueOnlyForEofKind(TokenKind kind, bool expected)
    {
        Assert.Equal(expected, kind.IsEndOfFile());
    }
}
