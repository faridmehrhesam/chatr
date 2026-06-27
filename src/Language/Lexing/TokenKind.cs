namespace Chatr.Language.Lexing;

public enum TokenKind
{
    // Punctuators
    LParen,
    RParen,
    Comma,
    Semi,
    Colon,

    // Keywords
    Create,
    Mut,
    String,
    Table,

    // Literals
    StringLiteral,

    // Identifier
    Identifier,

    // Others
    Bad,
    Eof,
}
