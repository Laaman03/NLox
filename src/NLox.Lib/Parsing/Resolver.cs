using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NLox.Lib.Parsing
{
    public class Resolver : Expr.IVisitor<int>, Stmt.IVisitor<int>
    {
        private readonly Interpreter _interpreter;
        private readonly ErrorReporter _reporter;

        /// <summary>
        /// Key:    Identifier Name
        /// Value:  Is the identifier is in scope?
        /// </summary>
        private readonly Stack<Dictionary<string, bool>> scopes = new();

        private FunctionType currentFunctionType = FunctionType.NONE;
        private ClassType currentClassType = ClassType.NONE;


        public Resolver(Interpreter interpreter, ErrorReporter reporter)
        {
            _reporter = reporter;
            _interpreter = interpreter;
        }
        public int VisitBlockStmt(Stmt.Block stmt)
        {
            BeginScope();
            Resolve(stmt.Statements);
            EndScope();
            return 0;
        }
        public int VisitVarStmt(Stmt.Var stmt)
        {
            Declare(stmt.Name);
            if (stmt.Initializer is not null)
            {
                Resolve(stmt.Initializer);
            }
            Define(stmt.Name);
            return 0;
        }
        public int VisitClassStmt(Stmt.Class stmt)
        {
            var enclosingClass = currentClassType;
            currentClassType = ClassType.CLASS;
            Declare(stmt.Name);
            Define(stmt.Name);

            BeginScope();
            scopes.Peek()["this"] = true;
            foreach (var method in stmt.Methods)
            {
                var decl = FunctionType.METHOD;
                ResolveFunction(method, decl);
            }
            EndScope();
            currentClassType = enclosingClass;
            return 0;
        }
        public int VisitFunctionStmt(Stmt.Function stmt)
        {
            Declare(stmt.Name);
            Define(stmt.Name);

            ResolveFunction(stmt, FunctionType.FUNCTION);
            return 0;
        }
        public int VisitExpressionStmt(Stmt.Expression stmt)
        {
            Resolve(stmt.ExpressionValue);
            return 0;
        }
        public int VisitIfStmt(Stmt.If stmt)
        {
            Resolve(stmt.Condition);
            Resolve(stmt.ThenBranch);
            if (stmt.ElseBranch is not null) Resolve(stmt.ElseBranch);
            return 0;
        }
        public int VisitPrintStmt(Stmt.Print stmt)
        {
            Resolve(stmt.ExpressionValue);
            return 0;
        }
        public int VisitReturnStmt(Stmt.Return stmt)
        {
            if (currentFunctionType == FunctionType.NONE)
            {
                _reporter.Error(stmt.Keyword, "Can't return from top-level code.");
            }
            if (stmt.Value is not null) Resolve(stmt.Value);
            return 0;
        }
        public int VisitWhileStmt(Stmt.While stmt)
        {
            Resolve(stmt.Condition);
            Resolve(stmt.Body);
            return 0;
        }
        public int VisitVariableExpr(Expr.Variable expr)
        {
            if (
                scopes.Count > 0 && 
                scopes.Peek().TryGetValue(expr.Name.Lexeme, out var inScope) &&
                !inScope
                )
            {
                _reporter.Error(expr.Name, "Can't read local variable in its own initializer.");
            }
            ResolveLocal(expr, expr.Name);
            return 0;
        }

        public int VisitThisExpr(Expr.This expr)
        {
            ResolveLocal(expr, expr.Keyword);
            return 0;
        }

        public int VisitAssignExpr(Expr.Assign expr)
        {
            Resolve(expr.Expression);
            ResolveLocal(expr, expr.Name);
            return 0;
        }
        public int VisitSetExpr(Expr.Set expr)
        {
            Resolve(expr.Value);
            Resolve(expr.Owner);
            return 0;
        }
        public int VisitBinaryExpr(Expr.Binary expr)
        {
            Resolve(expr.Left);
            Resolve(expr.Right);
            return 0;
        }
        public int VisitLogicalExpr(Expr.Logical expr)
        {
            Resolve(expr.Left);
            Resolve(expr.Right);
            return 0;
        }
        public int VisitUnaryExpr(Expr.Unary expr)
        {
            Resolve(expr.Right);
            return 0;
        }
        public int VisitCallExpr(Expr.Call expr)
        {
            Resolve(expr.Callee);

            foreach (var arg in expr.Arguments)
            {
                Resolve(arg);
            }

            return 0;
        }

        public int VisitGetExpr(Expr.Get expr)
        {
            Resolve(expr.Owner);
            return 0;
        }

        public int VisitGroupingExpr(Expr.Grouping expr)
        {
            Resolve(expr.Expression);
            return 0;
        }

        public int VisitLiteralExpr(Expr.Literal _) => 0;

        private void Declare(Token name)
        {
            if (scopes.Count == 0) return;

            var scope = scopes.Peek();
            if (scope.ContainsKey(name.Lexeme))
            {
                _reporter.Error(name, "Already a variable with this name in this scope.");
            }
            scope.Add(name.Lexeme, false);
        }

        private void Define(Token name)
        {
            if (scopes.Count == 0) return;

            scopes.Peek()[name.Lexeme] = true;
        }

        /// <summary>
        /// Call each <c>Accept</c> method on this for every stmt in the list.
        /// </summary>
        public void Resolve(List<Stmt> stmts)
        {
            foreach (var stmt in stmts)
            {
                Resolve(stmt);
            }
        }

        /// <summary>
        /// Call the stmt's <c>Accept</c> method on this.
        /// </summary>
        private void Resolve(Stmt stmt)
        {
            stmt.Accept(this);
        }

        /// <summary>
        /// Call the expr's <c>Accept</c> method on this.
        /// </summary>
        private void Resolve(Expr expr)
        {
            expr.Accept(this);
        }

        private void ResolveLocal(Expr expr, Token name)
        {
            for (var i = 0; i < scopes.Count; i++)
            {
                if (scopes.ElementAt(i).ContainsKey(name.Lexeme))
                {
                    _interpreter.Resolve(expr, i);
                }
            }
        }

        private void ResolveFunction(Stmt.Function function, FunctionType functionType)
        {
            var enclosingFunction = currentFunctionType;
            currentFunctionType = functionType;
            BeginScope();
            foreach (var param in function.Parameters)
            {
                Declare(param);
                Define(param);
            }
            Resolve(function.Body);
            EndScope();
            currentFunctionType = enclosingFunction;
        }

        private void BeginScope() => scopes.Push(new());
        private void EndScope() => scopes.Pop();

        private enum FunctionType
        {
            NONE,
            FUNCTION,
            METHOD,
        }

        private enum ClassType
        {
            NONE,
            CLASS,
        }
    }
}
