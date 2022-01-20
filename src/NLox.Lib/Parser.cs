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

        /// <summary>
        /// Function to begin parsing a list of statements.
        /// </summary>
        /// <returns>A list of statements that the interpreter can consume.</returns>
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

        /// <summary>
        /// The beginning of the parse chain. The parser works by trying to parse the types of
        /// expression/statements in reverse order of precedence and each function will call the
        /// a function of superior precedence.
        /// </summary>
        private Stmt Declaration()
        {
            try
            {
                if (Match(FUN)) return FunDeclaration("function");
                if (Match(VAR)) return VarDeclaration();
                return Statement();
            }
            catch (ParseError e)
            {
                Sync();
                return null;
            }
        }

        private Stmt.Function FunDeclaration(string kind)
        {
            Token name = Consume(IDENTIFIER, $"Expect {kind} name.");
            Consume(LEFT_PAREN, $"Expect '(' after {kind} name.");
            List<Token> parameters = new();
            if (!Check(RIGHT_PAREN))
            {
                do
                {
                    if (parameters.Count >= 255)
                    {
                        Error(Peek(), "Can't have more than 255 parameters.");
                    }
                    parameters.Add(Consume(IDENTIFIER, "Expect parameter name."));
                } while (Match(COMMA));
            }
            Consume(RIGHT_PAREN, "Expect ')' after parameters");
            Consume(LEFT_BRACE, $"Expect '{{' before {kind} body.");
            List<Stmt> body = Block();
            return new Stmt.Function(name, parameters, body);
        }

        /// <summary>
        /// When we decide that we are going to parse a
        /// variable declaration we get the name, then we set <c>initializer</c> to an <c>Expression</c>
        /// and return a <c>Stmt.Var</c> with the name and intializer.
        /// </summary>
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

        /// <summary>
        /// Checks if we should parse a <c>print</c>, a block, or an <c>if</c>.
        /// If none of those are what we are going to be parsing it returns a 
        /// call to <c>ExpressionStatement</c>.
        /// <see cref="PrintStatement"/>
        /// <see cref="Stmt.Block"/>
        /// <see cref="Block"/>
        /// <see cref="ExpressionStatement"/>
        /// </summary>
        private Stmt Statement()
        {
            if (Match(PRINT)) return PrintStatement();
            if (Match(RETURN)) return ReturnStatement();
            if (Match(LEFT_BRACE)) return new Stmt.Block(Block());
            if (Match(IF)) return IfStatement();
            return ExpressionStatement();
        }

        /// <summary>
        /// Parses an Expression into a print statement.
        /// <see cref="Expression"/>
        /// <see cref="Stmt.Print"/>
        /// </summary>
        private Stmt PrintStatement()
        {
            var value = Expression();
            Consume(SEMICOLON, "Expect ';' after value.");
            return new Stmt.Print(value);
        }

        private Stmt ReturnStatement()
        {
            var keyword = Previous();
            Expr value = null;
            if (!Check(SEMICOLON))
            {
                value = Expression();
            }
            Consume(SEMICOLON, "Expect ';' after return value.");
            return new Stmt.Return(keyword, value);
        }

        /// <summary>
        /// Creates a list of statements that are grouped together in a { block }.
        /// Allows the interpreter to create an environment for block scoped variables.
        /// <see cref="Declaration"/>
        /// </summary>
        private List<Stmt> Block()
        {
            List<Stmt> stmts = new();
            while (!Check(RIGHT_BRACE) && !IsAtEnd())
            {
                stmts.Add(Declaration());
            }
            Consume(RIGHT_BRACE, "Expect '}' after block.");
            return stmts;
        }

        /// <summary>
        /// Creates an <c>if</c> statement with a then branch and an else branch.
        /// <see cref="Expression"/>
        /// <see cref="Statement"/>
        /// <see cref="Stmt.If"/>
        /// </summary>
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

        /// <summary>
        /// Creates a statement from an expression.
        /// <see cref="Expression"/>
        /// <see cref="Stmt.Expression"/>
        /// </summary>
        /// <returns></returns>
        private Stmt ExpressionStatement()
        {
            var expr = Expression();
            Consume(SEMICOLON, "Expect ';' after expression.");
            return new Stmt.Expression(expr);
        }

        /// <summary>
        /// This is the beginning of the <c>Expression</c> parse chain. To begin with an
        /// <c>Expression</c> itself is the lowest precedence of the expression chain. So
        /// naturally it only makes a call to <c>Assignment</c>
        /// <see cref="Assignment"/>
        /// </summary>
        /// <returns></returns>
        private Expr Expression() => Assignment();

        /// <summary>
        /// Parser an <c>Assignment</c> expression or any expression of higher precedence.
        /// <code>
        /// var a = 1;
        /// a = 2; // This is a lox assignemnt expression.
        /// </code>
        /// A valid expression is also valid for the left side of an assignment expression.
        /// <code>
        /// newPoint().y = 1;
        /// </code>
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Parse an <c>Or</c> expression or any expression of higher precedence.
        /// </summary>
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

        /// <summary>
        /// Parse an <c>And</c> expression or any expression of higher precedence.
        /// </summary>
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

        /// <summary>
        /// Parse an <c>Equality</c> expression or any expression of higher precedence.
        /// </summary>
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

        /// <summary>
        /// Parse an <c>Comp</c> expression or any expression of higher precedence.
        /// </summary>
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

        /// <summary>
        /// Parse an <c>Term</c> expression or any expression of higher precedence.
        /// </summary>
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

        /// <summary>
        /// Parse an <c>Factor</c> expression or any expression of higher precedence.
        /// </summary>
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

        /// <summary>
        /// Parse an <c>Unary</c> expression or any expression of higher precedence.
        /// </summary>
        private Expr Unary()
        {
            if (Match(BANG, MINUS))
            {
                var op = Previous();
                var right = Unary();
                return new Expr.Unary(op, right);
            }
            return Call();
        }

        /// <summary>
        /// Parse an <c>Call</c> expression or any expression of higher precedence.
        /// </summary>
        private Expr Call()
        {
            Expr expr = Primary();

            while (true)
            {
                if (Match(LEFT_PAREN))
                {
                    expr = FinishCall(expr);
                }
                else
                {
                    break;
                }
            }
            return expr;
        }

        private Expr FinishCall(Expr callee)
        {
            List<Expr> args = new();
            if (!Check(RIGHT_PAREN))
            {
                do
                {
                    if (args.Count >= 255) Error(Peek(), "Can't have more than 255 arguments.");
                    args.Add(Expression());
                }
                while (Match(COMMA));
            }

            // After having parsed all of the args we must encounter another ')'
            var paren = Consume(RIGHT_PAREN, "Expect ')' after call.");
            return new Expr.Call(callee, paren, args);
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

        /// <summary>
        /// Conditionally <c>Advance()</c> upon encountering a token type
        /// that matches one of the provided <c>ttypes</c>.
        /// </summary>
        /// <param name="ttypes"></param>
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

        /// <summary>
        /// Check the next token against a given type. DOES NOT ADVANCE.
        /// </summary>
        /// <param name="ttype"></param>
        private bool Check(TokenType ttype)
        {
            if (IsAtEnd()) return false;
            return Peek().Type == ttype;
        }

        /// <summary>
        /// Advance the token cursor by one.
        /// </summary>
        /// <returns>The next token in the sequence.</returns>
        private Token Advance()
        {
            if (!IsAtEnd()) current++;
            return Previous();
        }

        /// <summary>
        /// <c>Advance()</c> if the next token is of the type provided. Throws error otherwise.
        /// </summary>
        /// <param name="ttype"></param>
        /// <param name="message"></param>
        /// <returns>The next token in the sequence.</returns>
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
