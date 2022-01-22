using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static NLox.Lib.Parsing.TokenType;

namespace NLox.Lib.Parsing
{
    public class Scanner
    {
        private static readonly Dictionary<string, TokenType> keywords = new()
        {
            { "and", AND },
            { "class", CLASS },
            { "else", ELSE },
            { "false", FALSE },
            { "for", FOR },
            { "fun", FUN },
            { "if", IF },
            { "nil", NIL },
            { "or", OR },
            { "print", PRINT },
            { "return", RETURN },
            { "super", SUPER },
            { "this", THIS },
            { "true", TRUE },
            { "var", VAR },
            { "while", WHILE },
        };

        readonly string _source;
        readonly ErrorReporter _reporter;
        readonly List<Token> _tokens = new();

        int start = 0;
        int current = 0;
        int line = 1;

        public Scanner(string source, ErrorReporter reporter)
        {
            _source = source;
            _reporter = reporter;
        }

        public List<Token> ScanTokens()
        {
            while (!IsAtEnd())
            {
                start = current;
                ScanToken();
            }

            _tokens.Add(new Token(EOF, "", null, line));
            return _tokens;
        }

        void ScanToken()
        {
            char c = Advance();
            switch (c)
            {
                case '(': AddToken(LEFT_PAREN); break;
                case ')': AddToken(RIGHT_PAREN); break;
                case '{': AddToken(LEFT_BRACE); break;
                case '}': AddToken(RIGHT_BRACE); break;
                case ',': AddToken(COMMA); break;
                case '.': AddToken(DOT); break;
                case '-': AddToken(MINUS); break;
                case '+': AddToken(PLUS); break;
                case ';': AddToken(SEMICOLON); break;
                case '*': AddToken(STAR); break;
                case '!': AddToken(TokenMatch('=') ? BANG_EQUAL : BANG); break;
                case '=': AddToken(TokenMatch('=') ? EQUAL_EQUAL : EQUAL); break;
                case '<': AddToken(TokenMatch('=') ? LESS_EQUAL : LESS); break;
                case '>': AddToken(TokenMatch('=') ? GREATER_EQUAL : GREATER); break;
                case '/':
                    if (TokenMatch('/'))
                    {
                        while (Peek() != '\n' && !IsAtEnd()) Advance();
                    }
                    else
                    {
                        AddToken(SLASH);
                    }
                    break;

                // Ignore whitespace
                case ' ':
                case '\r':
                case '\t':
                    break;
                case '\n':
                    line++;
                    break;

                case '"': StringMatch(); break;

                default:
                    if (IsDigit(c))
                    {
                        NumberMatch();
                    }
                    else if (IsAlpha(c))
                    {
                        IdentifierMatch();
                    }
                    else
                    {
                        _reporter.Error(line, "Unexpected character.");
                    }
                    break;

            }
        }

        char Advance() => _source[current++];

        void AddToken(TokenType type)
        {
            AddToken(type, null);
        }

        void AddToken(TokenType type, object literal)
        {
            var text = _source[start..current];
            _tokens.Add(new Token(type, text, literal, line));
        }

        bool IsAtEnd() => current >= _source.Length;
        bool IsDigit(char c) => c >= '0' && c <= '9';
        bool IsAlpha(char c) =>
            (c >= 'a' && c <= 'z') ||
            (c >= 'A' && c <= 'Z') ||
            c == '_';
        bool IsAlphaNumeric(char c) => IsAlpha(c) || IsDigit(c);

        bool TokenMatch(char expected)
        {
            if (IsAtEnd()) return false;
            if (_source[current] != expected) return false;

            current++;
            return true;
        }

        char Peek() => IsAtEnd() ? '\0' : _source[current];
        char PeekNext() => (current + 1) >= _source.Length ?
            '\0' :
            _source[current + 1];

        void StringMatch()
        {
            while (Peek() != '"' && !IsAtEnd())
            {
                if (Peek() == '\n') line++;
                Advance();
            }

            if (IsAtEnd())
            {
                _reporter.Error(line, "Unterminated string at EOF.");
                return;
            }

            // Consume the closing '"'
            Advance();
            var value = _source[(start + 1)..(current - 1)];
            AddToken(STRING, value);
        }

        void NumberMatch()
        {
            while (IsDigit(Peek())) Advance();

            // Look for decimal
            if (Peek() == '.' && IsDigit(PeekNext()))
            {
                // Consume the '.'
                Advance();

                while (IsDigit(Peek())) Advance();
            }

            AddToken(NUMBER, double.Parse(_source[start..current]));
        }

        void IdentifierMatch()
        {
            while (IsAlphaNumeric(Peek())) Advance();
            var text = _source[start..current];

            if (keywords.TryGetValue(text, out var type))
            {
                AddToken(type);
            }

            else
            {
                AddToken(IDENTIFIER);
            }
        }

    }
}
