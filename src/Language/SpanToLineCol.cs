namespace Chatr.Language;

public static class SpanToLineCol
{
    public static (int Line, int Column) Convert(ReadOnlySpan<char> source, int offset)
    {
        var line = 1;
        var col = 1;
        for (var i = 0; i < offset && i < source.Length; i++)
        {
            if (source[i] == '\n') { line++; col = 1; }
            else { col++; }
        }
        return (line, col);
    }
}
