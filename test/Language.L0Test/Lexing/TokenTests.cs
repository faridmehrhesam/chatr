using Chatr.Language.Lexing;

namespace Chatr.Language.L0Test.Lexing;

public class TokenTests
{
    [Fact]
    public void TestToken_ImplementsIEquatableOfToken()
    {
        var token = new Token { Kind = TokenKind.Eof, Span = new Span { Start = 0, End = 0 } };
        Assert.IsType<IEquatable<Token>>(token, exactMatch: false);
    }

    [Fact]
    public void TestToken_WhenSameKindAndSpan_AreEqual()
    {
        var a = new Token { Kind = TokenKind.String, Span = new Span { Start = 0, End = 6 } };
        var b = new Token { Kind = TokenKind.String, Span = new Span { Start = 0, End = 6 } };
        Assert.Equal(a, b);
    }

    [Fact]
    public void TestToken_WhenDifferentKindOrSpan_AreNotEqual()
    {
        var a = new Token { Kind = TokenKind.String, Span = new Span { Start = 0, End = 6 } };
        var b = new Token { Kind = TokenKind.Eof, Span = new Span { Start = 0, End = 6 } };
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void TestToken_WhenEqual_SameHashCode()
    {
        var a = new Token { Kind = TokenKind.String, Span = new Span { Start = 0, End = 6 } };
        var b = new Token { Kind = TokenKind.String, Span = new Span { Start = 0, End = 6 } };
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void TestToken_WhenSameKindAndSpan_EqualityOperatorReturnsTrue()
    {
        var a = new Token { Kind = TokenKind.String, Span = new Span { Start = 0, End = 6 } };
        var b = new Token { Kind = TokenKind.String, Span = new Span { Start = 0, End = 6 } };
        Assert.True(a == b);
    }

    [Fact]
    public void TestToken_WhenDifferentKindOrSpan_InequalityOperatorReturnsTrue()
    {
        var a = new Token { Kind = TokenKind.String, Span = new Span { Start = 0, End = 6 } };
        var b = new Token { Kind = TokenKind.Eof, Span = new Span { Start = 0, End = 6 } };
        Assert.True(a != b);
    }


    [Fact]
    public void TestToken_WhenToStringCalled_FormatsKindAndSpan()
    {
        var token = new Token
        {
            Kind = TokenKind.String,
            Span = new Span { Start = 0, End = 6 },
        };

        Assert.Equal("'string' [0..6]", token.ToString());
    }
}
