namespace Chatr.Language.Diagnostics;

public sealed class DiagnosticEngine
{
    private const int InitialCapacity = 8;
    private readonly List<Diagnostic> _diagnostics = new(InitialCapacity);
    private int _errorCount;

    public void Emit(Diagnostic diagnostic)
    {
        if (diagnostic.Severity == DiagnosticSeverity.Error)
        {
            _errorCount++;
        }

        _diagnostics.Add(diagnostic);
    }

    public bool HasErrors => _errorCount > 0;

    public int ErrorCount => _errorCount;

    public IReadOnlyList<Diagnostic> Diagnostics => _diagnostics;
}
