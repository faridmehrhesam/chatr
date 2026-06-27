using Chatr.Language.Diagnostics;
using Chatr.Language.Lexing;
using Chatr.Language.Syntax;

namespace Chatr.Language.Parsing;

public sealed class Parser
{
    private const int DefaultListCapacity = 4;
    private static readonly TokenKind[] _statementRecoveryTokens = [TokenKind.Create];
    private static readonly TokenKind[] _columnRecoveryTokens = [TokenKind.Comma, TokenKind.Semi, TokenKind.RParen];

    private readonly Token[] _tokens;
    private readonly DiagnosticEngine _diagnostics;
    private int _position;

    private Parser(Token[] tokens, DiagnosticEngine diagnostics)
    {
        _tokens = tokens;
        _diagnostics = diagnostics;
    }

    public static CompilationUnit Parse(LexResult lex, DiagnosticEngine diagnostics)
    {
        return new Parser(lex.Tokens, diagnostics).ParseCompilationUnit(lex.Source);
    }

    private CompilationUnit ParseCompilationUnit(ReadOnlyMemory<char> source)
    {
        var statements = new List<IStatement>(DefaultListCapacity);

        while (Peek() != TokenKind.Eof)
        {
            var statement = ParseStatement();
            if (statement is null)
            {
                Emit($"Expected statement, found {Peek().Describe()}", PeekSpan());
                Synchronize(_statementRecoveryTokens);
                statements.Add(new ErrorStatement());
                continue;
            }

            statements.Add(statement);
        }

        return new CompilationUnit { Source = source, Statements = statements };
    }

    private IStatement? ParseStatement()
    {
        return ParseCreateTableStatement();
    }

    private IStatement? ParseCreateTableStatement()
    {
        if (!ConsumeIf(TokenKind.Create))
        {
            return null;
        }

        var isMutable = ConsumeIf(TokenKind.Mut);
        Expect(TokenKind.Table);

        var nameSpan = Expect(TokenKind.Identifier);
        var columns = ParseColumnList();
        Expect(TokenKind.Semi);

        return nameSpan is Span name
            ? new CreateTableStatement
            {
                Table = new TableDeclaration
                {
                    Name = name,
                    IsMutable = isMutable,
                    Columns = columns
                }
            }
            : new ErrorStatement();
    }

    private List<ColumnDefinition> ParseColumnList()
    {
        if (!ConsumeIf(TokenKind.LParen))
        {
            Emit("Expected '('", PeekSpan());
            return [];
        }

        var columns = new List<ColumnDefinition>(DefaultListCapacity);
        while (Peek() is not TokenKind.RParen and not TokenKind.Eof)
        {
            var column = ParseColumnDefinition();
            if (column is not null)
            {
                columns.Add(column);
                if (!ConsumeIf(TokenKind.Comma))
                {
                    break;
                }
            }
            else
            {
                Emit($"Expected column definition, found {Peek().Describe()}", PeekSpan());
                Synchronize(_columnRecoveryTokens);
                if (!ConsumeIf(TokenKind.Comma))
                {
                    break;
                }
            }
        }

        Expect(TokenKind.RParen);
        return columns;
    }

    private ColumnDefinition? ParseColumnDefinition()
    {
        if (Peek() != TokenKind.Identifier || PeekAt(1) != TokenKind.Colon)
        {
            return null;
        }

        // Lookahead above guarantees these two tokens are present.
        var nameSpan = Expect(TokenKind.Identifier)!.Value;
        Expect(TokenKind.Colon);

        var type = ParseType();
        if (type is null)
        {
            Emit($"Expected type, found {Peek().Describe()}", PeekSpan());
            Synchronize(_columnRecoveryTokens);
            return new ColumnDefinition { Name = nameSpan, Type = TypeKind.Error };
        }

        return new ColumnDefinition { Name = nameSpan, Type = type.Value };
    }

    private TypeKind? ParseType()
    {
        if (Peek() == TokenKind.String)
        {
            Advance();
            return TypeKind.String;
        }

        return null;
    }

    private TokenKind Peek()
    {
        return _position < _tokens.Length ? _tokens[_position].Kind : TokenKind.Eof;
    }

    private TokenKind PeekAt(int offset)
    {
        return _position + offset < _tokens.Length ? _tokens[_position + offset].Kind : TokenKind.Eof;
    }

    private Span PeekSpan()
    {
        return _position < _tokens.Length ? _tokens[_position].Span : new Span { Start = 0, End = 0 };
    }

    private bool ConsumeIf(TokenKind expected)
    {
        if (Peek() == expected)
        {
            Advance();
            return true;
        }
        return false;
    }

    private Span? Expect(TokenKind expected)
    {
        if (Peek() == expected)
        {
            var span = PeekSpan();
            Advance();
            return span;
        }
        Emit($"Expected {expected.Describe()}, found {Peek().Describe()}", PeekSpan());
        return null;
    }

    private void Advance()
    {
        if (_position < _tokens.Length)
        {
            _position++;
        }
    }

    private void Synchronize(ReadOnlySpan<TokenKind> recoveryTokens)
    {
        while (Peek() != TokenKind.Eof)
        {
            if (recoveryTokens.Contains(Peek()))
            {
                return;
            }

            Advance();
        }
    }

    private void Emit(string message, Span span)
    {
        _diagnostics.Emit(Diagnostic.Error(message, span));
    }
}
