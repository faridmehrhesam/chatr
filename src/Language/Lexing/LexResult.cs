namespace Chatr.Language.Lexing;

public readonly struct LexResult
{
    public ReadOnlyMemory<char> Source { get; }
    public Token[] Tokens { get; }

    public LexResult(ReadOnlyMemory<char> source, Token[] tokens)
    {
        Source = source;
        Tokens = tokens;
    }
}
