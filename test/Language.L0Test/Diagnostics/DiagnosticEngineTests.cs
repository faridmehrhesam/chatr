using Chatr.Language.Diagnostics;

namespace Chatr.Language.L0Test.Diagnostics;

public class DiagnosticEngineTests
{
    private static Span AnySpan => new() { Start = 0, End = 0 };

    [Fact]
    public void TestDiagnosticEngine_WhenEngineIsNew_HasNoErrors()
    {
        var engine = new DiagnosticEngine();
        Assert.False(engine.HasErrors);
        Assert.Equal(0, engine.ErrorCount);
    }

    [Fact]
    public void TestDiagnosticEngine_WhenErrorEmitted_HasErrorsIsTrue()
    {
        var engine = new DiagnosticEngine();
        engine.Emit(Diagnostic.Error("oops", AnySpan));
        Assert.True(engine.HasErrors);
    }

    [Fact]
    public void TestDiagnosticEngine_WhenOnlyWarningEmitted_HasErrorsIsFalse()
    {
        var engine = new DiagnosticEngine();
        engine.Emit(Diagnostic.Warning("careful", AnySpan));
        Assert.False(engine.HasErrors);
    }

    [Fact]
    public void TestDiagnosticEngine_WhenMixedDiagnosticsEmitted_ErrorCountCountsOnlyErrors()
    {
        var engine = new DiagnosticEngine();
        engine.Emit(Diagnostic.Error("e1", AnySpan));
        engine.Emit(Diagnostic.Warning("w1", AnySpan));
        engine.Emit(Diagnostic.Error("e2", AnySpan));
        Assert.Equal(2, engine.ErrorCount);
    }

    [Fact]
    public void TestDiagnosticEngine_WhenDiagnosticsEmitted_PreservesEmitOrder()
    {
        var engine = new DiagnosticEngine();
        engine.Emit(Diagnostic.Error("first", AnySpan));
        engine.Emit(Diagnostic.Information("second", AnySpan));
        var msgs = engine.Diagnostics.Select(d => d.Message).ToList();
        Assert.Equal(["first", "second"], msgs);
    }

    [Fact]
    public void TestDiagnosticEngine_WhenDiagnosticsAccessed_SupportsIndexAndCount()
    {
        var engine = new DiagnosticEngine();
        engine.Emit(Diagnostic.Error("first", AnySpan));
        engine.Emit(Diagnostic.Warning("second", AnySpan));
        Assert.Equal("first", engine.Diagnostics[0].Message);
        Assert.Equal(2, engine.Diagnostics.Count);
    }

    [Fact]
    public void TestDiagnosticEngine_WhenErrorToStringCalled_FormatsWithErrorPrefix()
    {
        var span = new Span { Start = 3, End = 7 };
        var d = Diagnostic.Error("unexpected token", span);
        Assert.Equal("error: unexpected token [3..7]", d.ToString());
    }

    [Fact]
    public void TestDiagnosticEngine_WhenWarningToStringCalled_FormatsWithWarningPrefix()
    {
        var span = new Span { Start = 1, End = 5 };
        var d = Diagnostic.Warning("unused variable", span);
        Assert.Equal("warning: unused variable [1..5]", d.ToString());
    }

    [Fact]
    public void TestDiagnosticEngine_WhenInfoToStringCalled_FormatsWithInformationPrefix()
    {
        var span = new Span { Start = 0, End = 3 };
        var d = Diagnostic.Information("hint", span);
        Assert.Equal("information: hint [0..3]", d.ToString());
    }
}
