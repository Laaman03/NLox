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
                statements.Add(Declaration());
            }
            return statements;
        }

        public Parser(List<Token> tokens, ErrorReporter reporter)
        {
            _tokens = tokens;
            _reporter = reporter;
        }

        private Stmt Declaration()
        {
            try
            {
                if (Match(VAR)) return VarDeclaration();
                return Statement();
            }
            catch (ParseError e)
            {
                Sync();
                return null;
            }
        }

        private Stmt VarDeclaration()
        {
            var name = Consume(IDENTIFIER, "Expect variable name.");
            Expr initializer = null;

            if (Match(EQUAL))
            {
                initializer = Expression();
            }

            Consume(SEMICOLON, "Expect ';' after variable declaration");
            return new Stmt.Var(name, initializer);
        }

        private Stmt Statement()
        {
            if (Match(PRINT)) return PrintStatement();
            if (Match(LEFT_BRACE)) return new Stmt.Block(Block());
            if (Match(IF)) return IfStatement();
            return ExpressionStatement();
        }

        private Stmt PrintStatement()
        {
            var value = Expression();
            Consume(SEMICOLON, "Expect ';' after value.");
            return new Stmt.Print(value);
        }

        private Stmt ExpressionStatement()
        {
            var expr = Expression();
            Consume(SEMICOLON, "Expect ';' after expression.");
            return new Stmt.Expression(expr);
        }

        private Stmt IfStatement()
        {
            Consume(LEFT_PAREN, "Expect '(' after 'if'.");
            Expr condition = Expression();
            Consume(RIGHT_PAREN, "Expect ')' after if condition.");

            Stmt thenBranch = Statement();
            Stmt elseBranch = null;
            if (Match(ELSE))
            {
                elseBranch = Statement();
            }

            return new Stmt.If(condition, thenBranch, elseBranch);
        }

        private Expr Expression() => Assignment();

        private Expr Assignment()
        {
            Expr expr = Or();
            if (Match(EQUAL))
            {
                Token equals = Previous();
                Expr value = Assignment();

                if (expr is Expr.Variable var)
                {
                    Token name = var.Name;
                    return new Expr.Assign(name, value);
                }

                _reporter.Error(equals, "Invalid assignment target.");
            }
            return expr;
        }

        private Expr Or()
        {
            var expr = And();

            while (Match(OR))
            {
                var op = Previous();
                var right = And();
                expr = new Expr.Logical(expr, op, right);
            }

            return expr;
        }

        private Expr And()
        {
            var expr = Equality();

            while (Match(AND))
            {
                var op = Previous();
                var right = Equality();
                expr = new Expr.Logical(expr, op, right);
            }

            return expr;
        }

        private Expr Equality()
        {
            var expr = Comp();

            while (Match(BANG_EQUAL, EQUAL_EQUAL))
            {
                var op = Previous();
                var right = Comp();
                expr = new Expr.Binary(expr, op, right);
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
                expr = new Expr.Binary(expr, op, right);
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
                expr = new Expr.Binary(expr, op, right);
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
                expr = new Expr.Binary(expr, op, right);
            }

            return expr;
        }

        private Expr Unary()
        {
            if (Match(BANG, MINUS))
            {
                var op = Previous();
                var right = Unary();
                return new Expr.Unary(op, right);
            }
            return Primary();
        }

        private Expr Primary()
        {
            if (Match(FALSE)) return new Expr.Literal(false);
            if (Match(TRUE)) return new Expr.Literal(true);
            if (Match(NIL)) return new Expr.Literal(null);

            if (Match(NUMBER, STRING))
            {
                return new Expr.Literal(Previous().Literal);
            }

            if (Match(IDENTIFIER))
            {
                return new Expr.Variable(Previous());
            }

            if (Match(LEFT_PAREN))
            {
                var expr = Expression();
                Consume(RIGHT_PAREN, "Expect ')' after experssion.");
                return new Expr.Grouping(expr);
            }

            throw Error(Peek(), "Expect expression.");
        }

        private List<Stmt> Block()
        {
            List<Stmt> stmts = new();
            while (!Check(TokenType.RIGHT_BRACE) && !IsAtEnd())
            {
                stmts.Add(Declaration());
            }
            Consume(RIGHT_BRACE, "Expect '}' after block.");
            return stmts;
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
