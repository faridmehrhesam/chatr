# Chatr Studio — Editor Design Spec
**Date:** 2026-06-30
**Status:** Approved
**Scope:** Phase 1 code editor for `/packages/{id}/edit` — Monaco integration, ChatrLang diagnostics, file tree, save to Gitea

---

## 1. Context

This spec covers the implementation of the Phase 1 editor feature for Chatr Studio, as defined in the [Studio Design Spec](2026-06-26-studio-design.md) (Section 7). The scaffold already provides Blazor WASM, a Language library (lexer/parser/semantics/diagnostics) running in-process, Keycloak auth, and a minimal ASP.NET Core API.

**What this spec adds:**
- Gitea container in Aspire for file storage
- File API endpoints in Studio.Api (proxying Gitea)
- In-memory package registry for dev (no Postgres yet)
- Monaco Editor via BlazorMonaco NuGet
- ChatrLang syntax highlighting (custom Monaco language definition)
- Real-time diagnostics from the Language library → Monaco markers
- File tree panel
- Save file (Ctrl+S) → commit to Gitea `draft` branch

**Out of scope for this spec:** Postgres, team/package CRUD pages, live preview, publishing, Phase 2 editor features (autocomplete, go-to-definition).

---

## 2. Architecture

```
┌─────────────────────────────────────────────────────────────┐
│  Browser (Blazor WASM)                                      │
│                                                             │
│  ┌────────────────────────────────────────────────────────┐ │
│  │  EditorPage.razor  (/packages/{id}/edit)               │ │
│  │                                                        │ │
│  │  ┌──────────────┬───────────────────────────────────┐  │ │
│  │  │ FileTree     │ MonacoEditor                      │  │ │
│  │  │ .razor       │ .razor  (BlazorMonaco)            │  │ │
│  │  │              │  - chatrlang language              │  │ │
│  │  │ tables/      │  - SetModelMarkersAsync            │  │ │
│  │  │   crm.chatr  │  - Ctrl+S save                    │  │ │
│  │  │ screens/     │                                   │  │ │
│  │  │   home.chatr │                                   │  │ │
│  │  └──────────────┴───────────────────────────────────┘  │ │
│  │  ┌────────────────────────────────────────────────────┐ │ │
│  │  │ DiagnosticsPanel.razor                             │ │ │
│  │  │  ● Error  line 3, col 5  Unknown type              │ │ │
│  │  └────────────────────────────────────────────────────┘ │ │
│  └────────────────────────────────────────────────────────┘ │
│                                                             │
│  EditorSessionService (scoped DI)                           │
│    ↕ Language lib (sync, in-process — already WASM)         │
└──────────────────────────┬──────────────────────────────────┘
                           │ HTTPS REST
              ┌────────────▼────────────┐
              │  Studio.Api             │
              │  PackageFilesModule     │
              │  PackageRegistry (mem)  │
              │  IGiteaClient           │
              └────────────┬────────────┘
                           │ HTTP
              ┌────────────▼────────────┐
              │  Gitea (container)      │
              │  team-dev/my-app.git    │
              │  branch: draft          │
              └─────────────────────────┘
```

---

## 3. Frontend — Components

### 3.1 EditorPage.razor

- Route: `/packages/{id}/edit`
- Decorated with `[Authorize]`
- On `OnInitializedAsync`: calls `EditorSessionService.InitAsync(packageId)` to load file tree
- Renders `FileTree`, `MonacoEditor`, and `DiagnosticsPanel` side by side
- No state of its own — purely a layout shell that wires components to the service

### 3.2 FileTree.razor

- Injects `EditorSessionService`
- Subscribes to `SessionService.StateChanged` in `OnInitialized`, unsubscribes in `Dispose`
- Renders `SessionService.FilePaths` as a clickable list, highlighting `SessionService.CurrentFilePath`
- On file click: calls `await SessionService.SelectFileAsync(path)`

### 3.3 MonacoEditor.razor

- Injects `EditorSessionService`
- Wraps `StandaloneCodeEditor` from BlazorMonaco
- On `OnAfterRenderAsync` (first render): calls `IJSRuntime.InvokeVoidAsync("chatrlangLang.register")` to register the language definition (see Section 7)
- `OnDidChangeModelContent` callback → calls `SessionService.OnContentChanged(newContent)`
- Subscribes to `StateChanged`: when `SessionService.Diagnostics` changes, calls `SetModelMarkersAsync("chatrlang", markers)`
- Wires `Ctrl+S` via `AddCommand` → `SessionService.SaveAsync()`
- When `SessionService.CurrentFilePath` changes (new file selected): calls `SetValue(newContent)` to load the new file

### 3.4 DiagnosticsPanel.razor

- Injects `EditorSessionService`
- Subscribes to `StateChanged`
- Renders `SessionService.Diagnostics` as a table: severity icon, message, file path, line, column
- Clicking a row: raises `EventCallback<int> OnGoToLine`; `EditorPage` holds `@ref MonacoEditor _editor` and handles this by calling `_editor.ScrollToLineAsync(line)`
- `MonacoEditor` exposes `public Task ScrollToLineAsync(int line)` — calls Monaco's `revealLineInCenter` via JS interop

---

## 4. Frontend — EditorSessionService

**Location:** `Studio.Web/Services/EditorSessionService.cs`
**Lifetime:** Scoped

```
Properties:
  IReadOnlyList<string> FilePaths
  string? CurrentFilePath
  string CurrentContent
  IReadOnlyList<Diagnostic> Diagnostics

Events:
  event Action? StateChanged

Methods:
  Task InitAsync(Guid packageId)          — load file tree from API
  Task SelectFileAsync(string path)       — load file content from API, run diagnostics
  void OnContentChanged(string content)   — debounce 300ms, run diagnostics, fire StateChanged
  Task SaveAsync()                        — PUT content to API, show save indicator
```

**Diagnostic pipeline** (all synchronous, in-process):
1. `Lexer.Tokenize(content)` → `LexResult`
2. `Parser.Parse(lexResult, diagnosticEngine)`
3. `Analyzer.Analyze(compilationUnit, diagnosticEngine)`
4. Map `Diagnostic.Span` (Start + Length) → `(line, col)` via `SpanToLineCol` helper
5. Update `Diagnostics` list, fire `StateChanged`

**`SpanToLineCol` helper** — walks source string once, builds `int[]` of line-start offsets, then binary-searches for a given character offset to return `(line, col)`.

**Debounce** — implemented with `CancellationTokenSource`: each `OnContentChanged` call cancels the previous token and schedules `Task.Delay(300ms)` before running diagnostics.

**Save indicator** — `IsSaving` bool property; `SaveAsync` sets it true, fires `StateChanged`, awaits the API call, sets it false, fires `StateChanged` again. `MonacoEditor` renders a brief "Saved" status in its toolbar.

---

## 5. Backend — Studio.Api

### 5.1 PackageRegistry

**Location:** `Studio.Api/Packages/PackageRegistry.cs`
**Lifetime:** Singleton

In-memory dictionary seeded at startup:
```
00000000-0000-0000-0000-000000000001 → "team-dev/my-app"
```

Returns `null` if ID not found. Endpoints return `404` on null.

### 5.2 PackageFilesModule

**Location:** `Studio.Api/Packages/PackageFilesModule.cs`

Registered in `Program.cs` as a route group under `/packages`:

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/packages/{id}/files` | Returns `string[]` of relative file paths |
| GET | `/packages/{id}/files/{*path}` | Returns `{ "content": "..." }` |
| PUT | `/packages/{id}/files/{*path}` | Body: `{ "content": "..." }` → 204 |

All routes require authorization. All resolve `id` → repo via `PackageRegistry`.

### 5.3 IGiteaClient / GiteaClient

**Location:** `Studio.Api/Gitea/`

```
IGiteaClient:
  Task<string[]> GetTreeAsync(string repo, string branch)
  Task<string> GetFileAsync(string repo, string branch, string path)
  Task PutFileAsync(string repo, string branch, string path, string content, string commitMessage)
```

`GiteaClient` uses a named `HttpClient` with base address from `GiteaBaseUrl` config. All writes target the `draft` branch. Commit message format: `"Studio: update {path}"`.

**Exceptions:** `GiteaNotFoundException` (404 from Gitea) and `GiteaConflictException` (422) are translated to `Results.NotFound()` and `Results.Conflict()` by a minimal exception-handling middleware registered in `Program.cs`.

### 5.4 GiteaBootstrapService

**Location:** `Studio.Api/Gitea/GiteaBootstrapService.cs`
**Type:** `IHostedService`

Runs on startup, idempotent (checks existence before creating):
1. Creates org `team-dev` via Gitea admin API
2. Creates repo `my-app` under `team-dev` with auto-init
3. Creates `draft` branch from `main`
4. Commits seed files to `draft`:
   - `tables/crm.chatr` — minimal valid `CREATE TABLE` statement
   - `screens/home.chatr` — minimal valid screen stub
5. Seeds `PackageRegistry` with the dev package ID

Gitea admin credentials come from environment variables injected by AppHost.

---

## 6. Infrastructure — Gitea in Aspire

**AppHost changes:**

```csharp
var gitea = builder.AddContainer("gitea", "gitea/gitea")
    .WithHttpEndpoint(port: 3000, targetPort: 3000, name: "http")
    .WithEnvironment("GITEA__security__INSTALL_LOCK", "true")
    .WithEnvironment("GITEA__admin__USER", "gitea-admin")
    .WithEnvironment("GITEA__admin__PASSWD", "gitea-admin");

var api = builder.AddProject<Projects.Studio_Api>("studio-api")
    .WithReference(gitea)
    .WithEnvironment("GiteaBaseUrl", gitea.GetEndpoint("http"));
```

Studio.Api reads `GiteaBaseUrl` from config and uses it as the `HttpClient` base address for `GiteaClient`.

---

## 7. Monaco — ChatrLang Language Definition

**File:** `Studio.Web/wwwroot/js/chatrlang-lang.js`

The file exports `window.chatrlangLang = { register }`. The `register` function calls `monaco.languages.register({ id: 'chatrlang' })` and `monaco.languages.setMonarchTokensProvider('chatrlang', tokenizer)`. It is loaded via a `<script>` tag in `wwwroot/index.html` (after the Monaco loader), and `MonacoEditor.razor` invokes it via `IJSRuntime.InvokeVoidAsync("chatrlangLang.register")` on first render.

Token rules (minimal Phase 1 set):
- **Keywords:** `CREATE`, `TABLE`, `MUT`, `STRING` — rendered as `keyword` class
- **Identifiers:** `[a-zA-Z_][a-zA-Z0-9_]*` — `identifier`
- **String literals:** `"""..."""` and `'''...'''` — `string`
- **Brackets:** `(`, `)` — `delimiter.parenthesis`

Diagnostics map to Monaco `MarkerSeverity.Error` / `MarkerSeverity.Warning` based on `DiagnosticSeverity`.

**Span → MarkerData mapping** (handled by `SpanToLineCol` in `EditorSessionService`):
```
Diagnostic.Span.Start → (startLine, startCol)
Diagnostic.Span.Start + Diagnostic.Span.Length → (endLine, endCol)
```
Monaco line/column numbers are 1-based.

---

## 8. Dev Navigation

A hardcoded "Open Editor" link in `NavMenu.razor` routes to:
```
/packages/00000000-0000-0000-0000-000000000001/edit
```

This lets developers reach the editor immediately without a package-list page.

---

## 9. File Layout (new files)

```
src/
├── Studio.Api/
│   ├── Gitea/
│   │   ├── IGiteaClient.cs
│   │   ├── GiteaClient.cs
│   │   ├── GiteaBootstrapService.cs
│   │   ├── GiteaNotFoundException.cs
│   │   └── GiteaConflictException.cs
│   └── Packages/
│       ├── PackageRegistry.cs
│       └── PackageFilesModule.cs
│
└── Studio.Web/
    ├── Pages/
    │   └── Editor.razor                  (EditorPage)
    ├── Components/
    │   ├── FileTree.razor
    │   ├── MonacoEditor.razor
    │   └── DiagnosticsPanel.razor
    ├── Services/
    │   └── EditorSessionService.cs
    └── wwwroot/
        └── js/
            └── chatrlang-lang.js
```

---

## 10. Testing

- `Studio.Api.L0Test`: unit tests for `PackageRegistry` (null on unknown ID), `SpanToLineCol` (offset mapping correctness)
- `Studio.Api.L2Test`: integration test — PUT a file via the API, verify Gitea commit appears on `draft` branch (uses Testcontainers for Gitea)
- `Language.L0Test`: already covers lexer/parser/analyzer — no new tests needed for the diagnostic pipeline itself
