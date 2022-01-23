using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLox.Lib.Parsing;
using NLox.Lib.Functions;
using NLox.Lib.Classes;
using static NLox.Lib.Parsing.TokenType;

namespace NLox.Lib
{
    // IStmtVisitor returns int becuase C# can't do void generics
    // So we'll just return 0 always
    public class Interpreter : Expr.IVisitor<object>, Stmt.IVisitor<int>
    {
        public readonly Environment Globals = new();
        private Environment env;
        private readonly Dictionary<Expr, int> locals = new();
        private readonly ErrorReporter _reporter;

        public Interpreter(ErrorReporter reporter)
        {
            _reporter = reporter;
            env = Globals;
        }
        public void Interpret(List<Stmt> stmts)
        {
            try
            {
                foreach (var stmt in stmts)
                {
                    Execute(stmt);
                }
            }
            catch (RuntimeError error)
            {
                _reporter.RuntimeError(error);
            }
        }

        private void Execute(Stmt stmt)
        {
            stmt.Accept(this);
        }
        public object VisitBinaryExpr(Expr.Binary expr)
        {
            object left = Evaluate(expr.Left);
            object right = Evaluate(expr.Right);

            switch (expr.Op.Type)
            {
                case GREATER:
                    CheckNumberOperands(expr.Op, left, right);
                    return (double)left > (double)right;
                case GREATER_EQUAL:
                    CheckNumberOperands(expr.Op, left, right);
                    return (double)left >= (double)right;
                case LESS:
                    CheckNumberOperands(expr.Op, left, right);
                    return (double)left < (double)right;
                case LESS_EQUAL:
                    CheckNumberOperands(expr.Op, left, right);
                    return (double)left <= (double)right;
                case EQUAL_EQUAL:
                    return IsEqual(left, right);
                case BANG_EQUAL:
                    return !IsEqual(left, right);
                case MINUS:
                    CheckNumberOperands(expr.Op, left, right);
                    return (double)left - (double)right;
                case PLUS:
                    if (left is double lNum && right is double rNum)
                    {
                        return lNum + rNum;
                    }
                    if (left is string lStr && right is string rStr)
                    {
                        return lStr + rStr;
                    }
                    throw new RuntimeError(expr.Op, "Operands must be two numbers or two strings.");
                case SLASH:
                    CheckNumberOperands(expr.Op, left, right);
                    return (double)left / (double)right;
                case STAR:
                    CheckNumberOperands(expr.Op, left, right);
                    return (double)left * (double)right;
            }
            // Unreachable
            return null;
        }

        public object VisitGroupingExpr(Expr.Grouping expr)
        {
            return Evaluate(expr.Expression);
        }

        public object VisitLiteralExpr(Expr.Literal expr)
        {
            return expr.Value;
        }

        public object VisitCallExpr(Expr.Call expr)
        {
            object callee = Evaluate(expr.Callee);
            if (callee is not ILoxCallable)
            {
                throw new RuntimeError(expr.Paren, "Can only call functions and classes.");
            }
            List<object> args = new();
            foreach (Expr arg in expr.Arguments)
            {
                args.Add(Evaluate(arg));
            }

            var func = (ILoxCallable)callee;
            if (args.Count != func.Arity && func.Arity != -1)
            {
                throw new RuntimeError(expr.Paren,
                    $"Expected {func.Arity} arguments but got {args.Count}.");
            }
            return func.Call(this, args);
        }

        public object VisitThisExpr(Expr.This expr) => LookupVariable(expr.Keyword, expr);

        public object VisitGetExpr(Expr.Get expr)
        {
            object @object = Evaluate(expr.Owner);
            if (@object is LoxInstance instance)
            {
                return instance.GetProp(expr.Name);
            }

            throw new RuntimeError(expr.Name, "Only instances have properties.");
        }

        public object VisitUnaryExpr(Expr.Unary expr)
        {
            object right = Evaluate(expr.Right);
            switch (expr.Op.Type)
            {
                case MINUS:
                    CheckNumberOperand(expr.Op, right);
                    return -(double)right;
                case BANG:
                    CheckNumberOperand(expr.Op, right);
                    return !IsTruthy(right);
            }
            // Unreachable
            return null;
        }

        public object VisitLogicalExpr(Expr.Logical expr)
        {
            object left = Evaluate(expr.Left);

            if (expr.Op.Type == OR)
            {
                if (IsTruthy(left)) return left;
            }
            // Must be and
            else
            {
                if (!IsTruthy(left)) return left;
            }
            return Evaluate(expr.Right);
        }

        public object VisitVariableExpr(Expr.Variable expr) => LookupVariable(expr.Name, expr);

        public object VisitAssignExpr(Expr.Assign expr)
        {
            object value = Evaluate(expr.Expression);
            if (locals.TryGetValue(expr, out var distance))
            {
                env.AssignAt(distance, expr.Name, value);
            }
            else
            {
                Globals.Assign(expr.Name, value);
            }

            return value;
        }

        public object VisitSetExpr(Expr.Set expr)
        {
            object @object = Evaluate(expr.Owner);
            
            if (@object is not LoxInstance)
            {
                throw new RuntimeError(expr.Name, "Only instances have fields.");
            }

            var value = Evaluate(expr.Value);
            ((LoxInstance)@object).Set(expr.Name, value);
            return value;
        }

        private object Evaluate(Expr expr) => expr.Accept(this);

        public int VisitExpressionStmt(Stmt.Expression stmt)
        {
            Evaluate(stmt.ExpressionValue);
            return 0;
        }

        public int VisitIfStmt(Stmt.If stmt)
        {
            if (IsTruthy(Evaluate(stmt.Condition)))
            {
                Execute(stmt.ThenBranch);
            }
            else if (stmt.ElseBranch != null)
            {
                Execute(stmt.ElseBranch);
            }
            return 0;
        }

        public int VisitWhileStmt(Stmt.While stmt)
        {
            while (IsTruthy(Evaluate(stmt.Condition)))
            {
                Execute(stmt.Body);
            }
            return 0;
        }

        public int VisitBlockStmt(Stmt.Block stmt)
        {
            ExecuteBlock(stmt.Statements, new Environment(env));
            return 0;
        }

        public int VisitPrintStmt(Stmt.Print stmt)
        {
            object value = Evaluate(stmt.ExpressionValue);
            Console.WriteLine(Stringify(value));
            return 0;
        }

        public int VisitClassStmt(Stmt.Class stmt)
        {
            env.Define(stmt.Name.Lexeme, null);

            Dictionary<string, LoxFunction> methods = new();
            foreach (var method in stmt.Methods)
            {
                var func = new LoxFunction(method, env, method.Name.Lexeme == "init");
                methods.Add(method.Name.Lexeme, func);
            }

            var @class = new LoxClass(stmt.Name.Lexeme, methods);
            env.Assign(stmt.Name, @class);
            return 0;
        }

        public int VisitFunctionStmt(Stmt.Function stmt)
        {
            var func = new LoxFunction(stmt, env, false);
            env.Define(stmt.Name.Lexeme, func);
            return 0;
        }

        public int VisitReturnStmt(Stmt.Return stmt)
        {
            object value = null;
            if (stmt.Value != null) value = Evaluate(stmt.Value);

            throw new Return(value);
        }

        public int VisitVarStmt(Stmt.Var stmt)
        {
            object value = null;
            if (stmt.Initializer is not null)
            {
                value = Evaluate(stmt.Initializer);
            }
            env.Define(stmt.Name.Lexeme, value);
            return 0;
        }

        public object LookupVariable(Token name, Expr expr)
        {
            if (locals.TryGetValue(expr, out var distance))
            {
                return env.GetAt(distance, name.Lexeme);
            }
            else
            {
                return Globals.Get(name);
            }
        }

        internal void ExecuteBlock(List<Stmt> statements, Environment env)
        {
            Environment prev = this.env;
            try
            {
                this.env = env;
                foreach (var stmt in statements)
                {
                    Execute(stmt);
                }    
            }
            finally
            {
                this.env = prev;
            }
        }

        public void Resolve(Expr expr, int depth)
        {
            locals.Add(expr, depth);
        }
        
        private bool IsTruthy(object val)
        {
            if (val == null) return false;
            if (val is bool b) return b;
            return true;
        }
        private bool IsEqual(object a, object b)
        {
            if (a is null && b is null) return true;
            if (a is null) return false;

            return a.Equals(b);
        }

        private void CheckNumberOperand(Token op, object obj)
        {
            if (obj is double) return;
            throw new RuntimeError(op, "Operand must be a number.");
        }

        private void CheckNumberOperands(Token op, object left, object right)
        {
            if (left is double && right is double) return;
            throw new RuntimeError(op, $"Operands must be numbers. {left.GetType()} | {right.GetType()}");
        }

        private string Stringify(object obj)
        {
            if (obj is null) return "nil";

            if (obj is double d)
            {
                // Strips .0 down to look like an int
                var text = d.ToString();
                if (text.EndsWith(".0"))
                {
                    text = text[0..(text.Length - 2)];
                }
                return text;
            }
            return obj.ToString();
        }
    }
}
