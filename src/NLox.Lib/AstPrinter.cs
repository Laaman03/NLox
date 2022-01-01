using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NLox.Lib
{
    public class AstPrinter : IVisitor<string>
    {
        public string VisitBinaryExpr(Binary expr) =>
            Parenthesize(expr.Op.Lexeme, expr.Left, expr.Right);

        public string VisitGroupingExpr(Grouping expr) =>
            Parenthesize("group", expr.Expression);

        public string VisitLiteralExpr(Literal expr)
        {
            if (expr.Value is null) return "nil";
            return expr.Value.ToString();
        }

        public string VisitUnaryExpr(Unary expr) =>
            Parenthesize(expr.Op.Lexeme, expr.Right);

        public string Print(Expr expr)
        {
            return expr.Accept(this);
        }

        string Parenthesize(string name, params Expr[] exprs)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("(").Append(name);
            foreach (var e in exprs)
            {
                sb.Append(" ").Append(e.Accept(this));
            }
            sb.Append(")");
            return sb.ToString();
        }

    }
}
