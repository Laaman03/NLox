using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static NLox.Lib.TokenType;

namespace NLox.Lib
{
    // IStmtVisitor returns int becuase C# can't do void generics
    // So we'll just return 0 always
    public class Interpreter : IExprVisitor<object>, IStmtVisitor<int>
    {
        private readonly ErrorReporter _reporter;
        public Interpreter(ErrorReporter reporter)
        {
            _reporter = reporter;
        }
        public void Interpret(Expr expression)
        {
            try
            {
                object value = Evaluate(expression);
                Console.WriteLine(Stringify(value));
            }
            catch (RuntimeError error)
            {
                _reporter.RuntimeError(error);
            }
        }
        public object VisitBinaryExpr(Binary expr)
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

        public object VisitGroupingExpr(Grouping expr)
        {
            return Evaluate(expr.Expression);
        }

        public object VisitLiteralExpr(Literal expr)
        {
            return expr.Value;
        }

        public object VisitUnaryExpr(Unary expr)
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

        private object Evaluate(Expr expr) => expr.Accept(this);
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

        public int VisitExprStmt(ExprStmt expr)
        {
            Evaluate()
        }

        public int VisitPrintStmt(PrintStmt expr)
        {
            throw new NotImplementedException();
        }
    }
}
