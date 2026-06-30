using Chatr.Language.Diagnostics;
using Chatr.Language.Syntax;

namespace Chatr.Language.Semantics;

public static class Analyzer
{
    public static void Analyze(CompilationUnit unit, DiagnosticEngine diagnostics)
    {
        new Visitor(unit.Source, diagnostics).Visit(unit);
    }

    private sealed class Visitor(ReadOnlyMemory<char> source, DiagnosticEngine diagnostics) : DefaultVisitor
    {
        private readonly HashSet<string> _tableNames = new(StringComparer.Ordinal);
        private readonly HashSet<string> _columnNames = new(StringComparer.Ordinal);

        protected override void DefaultVisit(ISyntaxNode node)
        {

        }

        public override void VisitCompilationUnit(CompilationUnit node)
        {
            // Pass 1: register all table names, error on duplicates.
            var tableNameLookup = _tableNames.GetAlternateLookup<ReadOnlySpan<char>>();
            foreach (var statement in node.Statements)
            {
                if (statement is CreateTableStatement createTableStatement)
                {
                    var nameSpan = GetText(createTableStatement.Table.Name);
                    if (!tableNameLookup.Contains(nameSpan))
                    {
                        _tableNames.Add(nameSpan.ToString());
                    }
                    else
                    {
                        Emit($"Duplicate table name '{nameSpan}'", createTableStatement.Table.Name);
                    }
                }
            }

            // Pass 2: walk each statement for column-level checks.
            foreach (var statement in node.Statements)
            {
                statement.Accept(this);
            }
        }

        public override void VisitCreateTableStatement(CreateTableStatement node)
        {
            node.Table.Accept(this);
        }

        public override void VisitTableDeclaration(TableDeclaration node)
        {
            if (node.Columns.Count == 0 || AllColumnsAreErrors(node.Columns))
            {
                Emit("Table must have at least one column", node.Name);
                return;
            }

            _columnNames.Clear();
            var lookup = _columnNames.GetAlternateLookup<ReadOnlySpan<char>>();
            foreach (var column in node.Columns)
            {
                if (column.Type == TypeKind.Error)
                {
                    continue;
                }

                var nameSpan = GetText(column.Name);
                if (!lookup.Contains(nameSpan))
                {
                    _columnNames.Add(nameSpan.ToString());
                }
                else
                {
                    Emit($"Duplicate column name '{nameSpan}'", column.Name);
                }
            }
        }

        private static bool AllColumnsAreErrors(IReadOnlyList<ColumnDefinition> columns)
        {
            for (var i = 0; i < columns.Count; i++)
            {
                if (columns[i].Type != TypeKind.Error)
                {
                    return false;
                }
            }
            return true;
        }

        private ReadOnlySpan<char> GetText(Span span)
        {
            return source.Span.Slice(span.Start, span.Length);
        }

        private void Emit(string message, Span span)
        {
            diagnostics.Emit(Diagnostic.Error(message, span));
        }
    }
}
