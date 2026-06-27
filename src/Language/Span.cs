namespace Chatr.Language;

/// <summary>A half-open range of character offsets into a source string.</summary>
public readonly struct Span : IEquatable<Span>
{
    public required int Start { get; init; }
    public required int End { get; init; }

    public int Length => End - Start;

    public bool Equals(Span other)
    {
        return Start == other.Start && End == other.End;
    }

    public override bool Equals(object? obj)
    {
        return obj is Span other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Start, End);
    }

    public static bool operator ==(Span left, Span right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Span left, Span right)
    {
        return !left.Equals(right);
    }

    public override string ToString()
    {
        return $"{Start}..{End}";
    }
}
