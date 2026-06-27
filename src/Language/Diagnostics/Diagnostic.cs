namespace Chatr.Language.Diagnostics;

public readonly struct Diagnostic
{
    public required DiagnosticSeverity Severity { get; init; }
    public required string Message { get; init; }
    public required Span Span { get; init; }

    public static Diagnostic Error(string message, Span span)
    {
        return new() { Severity = DiagnosticSeverity.Error, Message = message, Span = span };
    }

    public static Diagnostic Warning(string message, Span span)
    {
        return new() { Severity = DiagnosticSeverity.Warning, Message = message, Span = span };
    }

    public static Diagnostic Information(string message, Span span)
    {
        return new() { Severity = DiagnosticSeverity.Information, Message = message, Span = span };
    }

    public override string ToString()
    {
        var severity = Severity switch
        {
            DiagnosticSeverity.Information => "information",
            DiagnosticSeverity.Warning => "warning",
            DiagnosticSeverity.Error => "error",
            _ => throw new InvalidOperationException($"Unexpected severity: {Severity}"),
        };

        return $"{severity}: {Message} [{Span}]";
    }
}
