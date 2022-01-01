using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static NLox.Lib.TokenType;

namespace NLox.Lib
{
    public class Parser
    {
        private readonly List<Token> _tokens;
        private readonly ErrorReporter _reporter;
        private int current;

        public List<Stmt> Parse()
        {
            var statements = new List<Stmt>();
            while (!IsAtEnd())
            {
                statements.Add(Statement());
            }
            return statements;
        }

        public Parser(List<Token> tokens, ErrorReporter reporter)
        {
            _tokens = tokens;
            _reporter = reporter;
        }

        private Stmt Statement()
        {
            if (Match(PRINT)) return PrintStatement();
            return ExpressionStatement();
        }

        private Stmt PrintStatement()
        {
            var value = Expression();
            Consume(SEMICOLON, "Expect ';' after value.");
            return new PrintStmt(value);
        }

        private Stmt ExpressionStatement()
        {
            var expr = Expression();
            Consume(SEMICOLON, "Expect ';' after expression.");
            return new ExprStmt(expr);
        }

        private Expr Expression() => Equality();
        private Expr Equality()
        {
            var expr = Comp();

            while (Match(BANG_EQUAL, EQUAL_EQUAL))
            {
                var op = Previous();
                var right = Comp();
                expr = new Binary(expr, op, right);
            }
            return expr;
        }

        private Expr Comp()
        {
            var expr = Term();
            while(Match(GREATER, GREATER_EQUAL, LESS, LESS_EQUAL))
            {
                var op = Previous();
                var right = Term();
                expr = new Binary(expr, op, right);
            }
            return expr;
        }

        private Expr Term()
        {
            var expr = Factor();

            while(Match(MINUS, PLUS))
            {
                var op = Previous();
                var right = Factor();
                expr = new Binary(expr, op, right);
            }

            return expr;
        }

        private Expr Factor()
        {
            var expr = Unary();

            while (Match(SLASH, STAR))
            {
                var op = Previous();
                var right = Unary();
                expr = new Binary(expr, op, right);
            }

            return expr;
        }

        private Expr Unary()
        {
            if (Match(BANG, MINUS))
            {
                var op = Previous();
                var right = Unary();
                return new Unary(op, right);
            }
            return Primary();
        }

        private Expr Primary()
        {
            if (Match(FALSE)) return new Literal(false);
            if (Match(TRUE)) return new Literal(true);
            if (Match(NIL)) return new Literal(null);

            if (Match(NUMBER, STRING))
            {
                return new Literal(Previous().Literal);
            }

            if (Match(LEFT_PAREN))
            {
                var expr = Expression();
                Consume(RIGHT_PAREN, "Expect ')' after experssion.");
                return new Grouping(expr);
            }

            throw Error(Peek(), "Expect expression.");
        }

        private bool Match(params TokenType[] ttypes)
        {
            foreach (var t in ttypes)
            {
                if (Check(t))
                {
                    Advance();
                    return true;
                }
            }
            return false;
        }

        private bool Check(TokenType ttype)
        {
            if (IsAtEnd()) return false;
            return Peek().Type == ttype;
        }

        private Token Advance()
        {
            if (!IsAtEnd()) current++;
            return Previous();
        }

        private Token Consume(TokenType ttype, string message)
        {
            if (Check(ttype)) return Advance();
            throw Error(Peek(), message);
        }

        private ParseError Error(Token token, string message)
        {
            _reporter.Error(token, message);
            return new ParseError();
        }

        private bool IsAtEnd() => Peek().Type == EOF;
        private Token Peek() => _tokens[current];
        private Token Previous() => _tokens[current - 1];

        private void Sync()
        {
            Advance();
            while (!IsAtEnd())
            {
                if (Previous().Type == SEMICOLON) return;
                switch (Peek().Type)
                {
                    case CLASS:
                    case FUN:
                    case VAR:
                    case FOR:
                    case IF:
                    case WHILE:
                    case PRINT:
                    case RETURN:
                        return;
                }
            }
            Advance();
        }
    }

    class ParseError : Exception { }
}
