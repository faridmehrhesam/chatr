namespace Chatr.Language.L0Test;

public class SpanTests
{
    [Fact]
    public void TestSpan_ImplementsIEquatableOfSpan()
    {
        Assert.IsType<IEquatable<Span>>(new Span { Start = 0, End = 0 }, exactMatch: false);
    }

    [Fact]
    public void TestSpan_WhenSameStartAndEnd_AreEqual()
    {
        var a = new Span { Start = 3, End = 10 };
        var b = new Span { Start = 3, End = 10 };
        Assert.Equal(a, b);
    }

    [Fact]
    public void TestSpan_WhenDifferentStartOrEnd_AreNotEqual()
    {
        var a = new Span { Start = 3, End = 10 };
        var b = new Span { Start = 3, End = 11 };
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void TestSpan_WhenEqual_SameHashCode()
    {
        var a = new Span { Start = 3, End = 10 };
        var b = new Span { Start = 3, End = 10 };
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void TestSpan_WhenSameStartAndEnd_EqualityOperatorReturnsTrue()
    {
        var a = new Span { Start = 3, End = 10 };
        var b = new Span { Start = 3, End = 10 };
        Assert.True(a == b);
    }

    [Fact]
    public void TestSpan_WhenDifferentStartOrEnd_InequalityOperatorReturnsTrue()
    {
        var a = new Span { Start = 3, End = 10 };
        var b = new Span { Start = 3, End = 11 };
        Assert.True(a != b);
    }


    [Fact]
    public void TestSpan_WhenLengthAccessed_ReturnsEndMinusStart()
    {
        var span = new Span { Start = 3, End = 10 };
        Assert.Equal(7, span.Length);
    }

    [Fact]
    public void TestSpan_WhenDefaultSpan_StartAndEndAreZero()
    {
        var span = default(Span);
        Assert.Equal(0, span.Start);
        Assert.Equal(0, span.End);
    }

    [Fact]
    public void TestSpan_WhenToStringCalled_FormatsAsStartDotDotEnd()
    {
        var span = new Span { Start = 3, End = 10 };
        Assert.Equal("3..10", span.ToString());
    }
}
