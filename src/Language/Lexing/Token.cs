namespace Chatr.Language.Lexing;

public readonly struct Token : IEquatable<Token>
{
    public required TokenKind Kind { get; init; }
    public required Span Span { get; init; }

    public bool Equals(Token other)
    {
        return Kind == other.Kind && Span.Equals(other.Span);
    }

    public override bool Equals(object? obj)
    {
        return obj is Token other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Kind, Span);
    }

    public static bool operator ==(Token left, Token right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Token left, Token right)
    {
        return !left.Equals(right);
    }

    public override string ToString()
    {
        return $"{Kind.Describe()} [{Span}]";
    }
}
