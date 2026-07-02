namespace Chatr.Language.L0Test;

public class SpanToLineColTests
{
    [Fact]
    public void Convert_WhenOffsetIsZero_ReturnsLine1Col1()
    {
        var (line, col) = SpanToLineCol.Convert("hello", 0);
        Assert.Equal(1, line);
        Assert.Equal(1, col);
    }

    [Fact]
    public void Convert_WhenOffsetInMiddleOfSingleLine_ReturnsCorrectColumn()
    {
        var (line, col) = SpanToLineCol.Convert("hello", 3);
        Assert.Equal(1, line);
        Assert.Equal(4, col);
    }

    [Fact]
    public void Convert_WhenOffsetIsNewline_ReturnsNextLineCol1()
    {
        var (line, col) = SpanToLineCol.Convert("ab\ncd", 3);
        Assert.Equal(2, line);
        Assert.Equal(1, col);
    }

    [Fact]
    public void Convert_WhenOffsetIsAfterNewline_ReturnsNextLineCol2()
    {
        var (line, col) = SpanToLineCol.Convert("ab\ncd", 4);
        Assert.Equal(2, line);
        Assert.Equal(2, col);
    }

    [Fact]
    public void Convert_WhenOffsetIsOnThirdLine_ReturnsLine3Col1()
    {
        var (line, col) = SpanToLineCol.Convert("a\nb\nc", 4);
        Assert.Equal(3, line);
        Assert.Equal(1, col);
    }
}
