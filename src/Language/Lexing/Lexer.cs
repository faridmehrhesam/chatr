using System.Buffers;
using System.Collections.Frozen;
using System.Diagnostics;

namespace Chatr.Language.Lexing;

public ref struct Lexer
{
    // Rough estimate for pre-allocating the token list.
    private const int AvgCharsPerToken = 5;

    private static readonly FrozenDictionary<string, TokenKind>.AlternateLookup<ReadOnlySpan<char>> _keywordsLookup = new Dictionary<string, TokenKind>
    {
        ["create"] = TokenKind.Create,
        ["mut"] = TokenKind.Mut,
        ["string"] = TokenKind.String,
        ["table"] = TokenKind.Table,
    }
    .ToFrozenDictionary()
    .GetAlternateLookup<ReadOnlySpan<char>>();

    private static readonly string[] _multiLineStringSequences = ["\"\"\"", "'''"];

    private readonly ReadOnlySpan<char> _source;
    private int _position;

    private Lexer(ReadOnlySpan<char> source)
    {
        _source = source;
        _position = 0;
    }

    /// <summary>Tokenizes the given source text and returns a <see cref="LexResult"/> pairing the source with its tokens.</summary>
    public static LexResult Tokenize(ReadOnlyMemory<char> source)
    {
        var lexer = new Lexer(source.Span);
        var estimatedTokens = source.Length / AvgCharsPerToken + 1;
        var rented = ArrayPool<Token>.Shared.Rent(estimatedTokens);
        var count = 0;

        try
        {
            while (true)
            {
                if (count == rented.Length)
                {
                    var larger = ArrayPool<Token>.Shared.Rent(rented.Length * 2);
                    rented.AsSpan(0, count).CopyTo(larger);
                    ArrayPool<Token>.Shared.Return(rented);
                    rented = larger;
                }

                var token = lexer.NextToken();
                rented[count++] = token;

                if (token.Kind.IsEndOfFile())
                {
                    break;
                }
            }

            return new LexResult(source, rented.AsSpan(0, count).ToArray());
        }
        finally
        {
            ArrayPool<Token>.Shared.Return(rented);
        }
    }

    private readonly char? CharacterAt(int index)
    {
        return index >= 0 && index < _source.Length ? _source[index] : null;
    }

    private Token CreateToken(TokenKind kind, int length)
    {
        var start = _position;
        _position += length;
        return new Token
        {
            Kind = kind,
            Span = new Span { Start = start, End = _position },
        };
    }

    private static TokenKind? FindKeyword(ReadOnlySpan<char> identifier)
    {
        return _keywordsLookup.TryGetValue(identifier, out var kind)
            ? kind
            : null;
    }

    private static bool IsIdentifierCharacter(char character)
    {
        return character is (>= 'A' and <= 'Z') or (>= 'a' and <= 'z') or (>= '0' and <= '9') or '_';
    }

    private static bool IsIdentifierStartCharacter(char character)
    {
        return character is (>= 'A' and <= 'Z') or (>= 'a' and <= 'z') or '_';
    }

    private readonly bool MatchesSequence(int position, string sequence)
    {
        if (position + sequence.Length > _source.Length)
        {
            return false;
        }

        return _source.Slice(position, sequence.Length).SequenceEqual(sequence);
    }

    // NOTE: Non-ASCII characters are only supported in comments and inside quotations.
    private Token NextToken()
    {
        var triviaLength = ScanTrivia(_position);
        _position += triviaLength;

        if (_position < _source.Length)
        {
            var punctuation = ScanPunctuation();
            if (punctuation is (TokenKind punctuationKind, int punctuationLength))
            {
                return CreateToken(punctuationKind, punctuationLength);
            }

            var stringLength = ScanStringLiteral();
            if (stringLength is not null)
            {
                return CreateToken(TokenKind.StringLiteral, stringLength.Value);
            }

            var identifierLength = ScanIdentifier(_position);
            if (identifierLength is not null)
            {
                var identifier = _source.Slice(_position, identifierLength.Value);
                var kind = FindKeyword(identifier) ?? TokenKind.Identifier;
                return CreateToken(kind, identifierLength.Value);
            }
        }
        else
        {
            return CreateToken(TokenKind.Eof, 0);
        }

        return CreateToken(TokenKind.Bad, 1);
    }

    private readonly int? ScanHexDigits(int start, int count)
    {
        var position = start;

        for (var i = 0; i < count; i++)
        {
            var character = CharacterAt(position);
            if (character is null || !char.IsAsciiHexDigit(character.Value))
            {
                return null;
            }
            position += 1;
        }

        return position - start;
    }

    private readonly int? ScanIdentifier(int start)
    {
        var position = start;
        var firstCharacter = CharacterAt(position);

        if (firstCharacter.HasValue && IsIdentifierStartCharacter(firstCharacter.Value))
        {
            position += 1;
            while (position < _source.Length && IsIdentifierCharacter(_source[position]))
            {
                position += 1;
            }
        }
        else
        {
            return null;
        }

        return position - start;
    }

    private readonly int ScanMultiLineStringLiteral(int start, string endSequence)
    {
        var position = start;
        while (position < _source.Length)
        {
            if (MatchesSequence(position, endSequence))
            {
                break;
            }
            position += 1;
        }
        return position - start;
    }

    private readonly int? ScanOctalCode(int start)
    {
        var position = start;
        var firstCharacter = CharacterAt(position);

        if (firstCharacter.HasValue && firstCharacter.Value is >= '0' and <= '7')
        {
            position += 1;

            for (var i = 0; i < 2; i++)
            {
                var nextCharacter = CharacterAt(position);
                if (nextCharacter.HasValue && nextCharacter.Value is >= '0' and <= '7')
                {
                    position += 1;
                }
                else
                {
                    break;
                }
            }

            return position - start;
        }

        return null;
    }

    private readonly (TokenKind Kind, int Length)? ScanPunctuation()
    {
        return _source[_position] switch
        {
            '(' => (TokenKind.LParen, 1),
            ')' => (TokenKind.RParen, 1),
            ',' => (TokenKind.Comma, 1),
            ';' => (TokenKind.Semi, 1),
            ':' => (TokenKind.Colon, 1),
            _ => null,
        };
    }

    private readonly int? ScanStringEscape(int start)
    {
        Debug.Assert(_source[start] == '\\');

        var position = start + 1;
        var nextCharacter = CharacterAt(position);
        if (nextCharacter is null)
        {
            return null;
        }

        switch (nextCharacter.Value)
        {
            case '\\' or '\'' or '"' or 'a' or 'b' or 'f' or 'n' or 'r' or 't' or 'v':
                position += 1;
                break;

            case 'u':
                {
                    position += 1;
                    var hexLength = ScanHexDigits(position, 4);
                    if (hexLength is null)
                    {
                        return null;
                    }
                    position += hexLength.Value;
                    break;
                }

            case 'U':
                {
                    position += 1;
                    var hexLength = ScanHexDigits(position, 8);
                    if (hexLength is null)
                    {
                        return null;
                    }
                    position += hexLength.Value;
                    break;
                }

            case 'x':
                {
                    position += 1;
                    var hexLength = ScanHexDigits(position, 2);
                    if (hexLength is null)
                    {
                        return null;
                    }
                    position += hexLength.Value;
                    break;
                }

            default:
                {
                    var octalLength = ScanOctalCode(position);
                    if (octalLength is null)
                    {
                        return null;
                    }
                    position += octalLength.Value;
                    break;
                }
        }

        return position - start;
    }

    private readonly int? ScanStringLiteral()
    {
        var start = _position;
        var position = start;

        var openingCharacter = CharacterAt(position);
        if (openingCharacter is null)
        {
            return null;
        }

        var character = openingCharacter.Value;

        bool isVerbatim;
        if (character == '@')
        {
            position += 1;
            var characterAfterAt = CharacterAt(position);
            if (characterAfterAt is null)
            {
                return null;
            }

            character = characterAfterAt.Value;
            isVerbatim = true;
        }
        else
        {
            isVerbatim = false;
        }

        var multiLineLength = TryScanMultiLineString(position, start);
        if (multiLineLength is int ml)
        {
            return ml;
        }

        foreach (var sequence in _multiLineStringSequences)
        {
            if (MatchesSequence(position, sequence))
            {
                return null;
            }
        }

        if (character is not '\'' and not '"')
        {
            return null;
        }

        position += 1;
        var contentLength = ScanStringLiteralContent(position, character, isVerbatim);
        if (contentLength is null)
        {
            return null;
        }

        position += contentLength.Value;

        return CharacterAt(position) == character ? position + 1 - start : null;
    }

    private readonly int? TryScanMultiLineString(int position, int start)
    {
        foreach (var sequence in _multiLineStringSequences)
        {
            if (!MatchesSequence(position, sequence))
            {
                continue;
            }

            position += sequence.Length;
            position += ScanMultiLineStringLiteral(position, sequence);
            return MatchesSequence(position, sequence)
                ? position + sequence.Length - start
                : null;
        }
        return null;
    }

    private readonly int? ScanStringLiteralContent(int start, char quoteCharacter, bool isVerbatim)
    {
        var position = start;

        while (position < _source.Length)
        {
            var character = _source[position];
            if (character == quoteCharacter && isVerbatim && CharacterAt(position + 1) == quoteCharacter)
            {
                position += 2;
            }
            else if (character == '\\' && !isVerbatim)
            {
                var escapeLength = ScanStringEscape(position);
                if (escapeLength is null)
                {
                    return null;
                }
                position += escapeLength.Value;
            }
            else if (character == quoteCharacter || (!isVerbatim && (character == '\r' || character == '\n')))
            {
                break;
            }
            else
            {
                position += 1;
            }
        }

        return position - start;
    }

    private readonly int ScanTrivia(int start)
    {
        var sourceLength = _source.Length;
        var position = start;

        // Early return: no whitespace or comment starts here.
        if (position < sourceLength && !char.IsWhiteSpace(_source[position]) && _source[position] != '/')
        {
            return 0;
        }

        while (true)
        {
            while (position < sourceLength && char.IsWhiteSpace(_source[position]))
            {
                position += 1;
            }

            if (position + 1 < sourceLength && _source[position] == '/' && _source[position + 1] == '/')
            {
                position += 2 + ScanLineComment(position + 2);
                continue;
            }

            break;
        }

        return position - start;
    }

    private readonly int ScanLineComment(int start)
    {
        var position = start;
        var sourceLength = _source.Length;

        while (position < sourceLength)
        {
            var character = _source[position];
            position += 1;

            if (character == '\n')
            {
                break;
            }

            if (character == '\r')
            {
                if (position < sourceLength && _source[position] == '\n')
                {
                    position += 1;
                }
                break;
            }
        }

        return position - start;
    }

}
