using System.Net.Http.Json;
using Chatr.Language;
using Chatr.Language.Diagnostics;
using Chatr.Language.Lexing;
using Chatr.Language.Parsing;
using Chatr.Language.Semantics;

namespace Chatr.Studio.Web.Services;

public record EditorDiagnostic(
    DiagnosticSeverity Severity,
    string Message,
    (int Line, int Column) Start,
    (int Line, int Column) End);

public sealed class EditorSessionService(IHttpClientFactory httpFactory) : IDisposable
{
    private readonly HttpClient _http = httpFactory.CreateClient("StudioApi");
    private Guid _packageId;
    private string _currentContent = "";
    private CancellationTokenSource? _cts;

    public IReadOnlyList<string> FilePaths { get; private set; } = [];
    public string? CurrentFilePath { get; private set; }
    public string CurrentContent => _currentContent;
    public bool IsSaving { get; private set; }
    public IReadOnlyList<EditorDiagnostic> Diagnostics { get; private set; } = [];

    public event Action? StateChanged;

    public async Task InitAsync(Guid packageId)
    {
        _packageId = packageId;
        var paths = await _http.GetFromJsonAsync<string[]>($"packages/{packageId}/files");
        FilePaths = paths ?? [];
        StateChanged?.Invoke();
    }

    public async Task SelectFileAsync(string path)
    {
        var result = await _http.GetFromJsonAsync<FileContent>($"packages/{_packageId}/files/{path}");
        CurrentFilePath = path;
        _currentContent = result?.Content ?? "";
        RunDiagnostics();
    }

    public void OnContentChanged(string content)
    {
        _currentContent = content;
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;
        Task.Run(async () =>
        {
            try
            {
                await Task.Delay(300, token);
                RunDiagnostics();
            }
            catch (OperationCanceledException)
            {
            }
        }, token);
    }

    public async Task SaveAsync()
    {
        IsSaving = true;
        StateChanged?.Invoke();
        try
        {
            await _http.PutAsJsonAsync($"packages/{_packageId}/files/{CurrentFilePath}", new { content = _currentContent });
        }
        finally
        {
            IsSaving = false;
            StateChanged?.Invoke();
        }
    }

    private void RunDiagnostics()
    {
        var source = _currentContent.AsMemory();
        var engine = new DiagnosticEngine();
        var lex = Lexer.Tokenize(source);
        var unit = Parser.Parse(lex, engine);
        Analyzer.Analyze(unit, engine);
        Diagnostics = engine.Diagnostics
            .Select(d => new EditorDiagnostic(
                d.Severity,
                d.Message,
                SpanToLineCol.Convert(source.Span, d.Span.Start),
                SpanToLineCol.Convert(source.Span, d.Span.End)))
            .ToList();
        StateChanged?.Invoke();
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }

    private sealed record FileContent(string Content);
}
