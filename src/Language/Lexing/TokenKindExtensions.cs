namespace Chatr.Language.Lexing;

public static class TokenKindExtensions
{
    public static string Describe(this TokenKind kind)
    {
        return kind switch
        {
            // Punctuators
            TokenKind.LParen => "'('",
            TokenKind.RParen => "')'",
            TokenKind.Comma => "','",
            TokenKind.Semi => "';'",
            TokenKind.Colon => "':'",

            // Keywords
            TokenKind.Create => "'create'",
            TokenKind.Mut => "'mut'",
            TokenKind.String => "'string'",
            TokenKind.Table => "'table'",

            // Literals
            TokenKind.StringLiteral => "string literal",

            // Identifier
            TokenKind.Identifier => "identifier",

            // Others
            TokenKind.Bad => "unexpected character",
            TokenKind.Eof => "end of file",

            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
        };
    }

    public static bool IsEndOfFile(this TokenKind kind)
    {
        return kind == TokenKind.Eof;
    }
}
