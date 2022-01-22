using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NLox.Lib.Parsing
{
    public class Token
    {
        internal TokenType Type { get; private set; }
        internal string Lexeme { get; private set; }
        internal object Literal { get; private set; }
        internal int Line { get; private set; }


        internal Token(TokenType type, string lexeme, object literal, int line)
        {
            Type = type;
            Lexeme = lexeme;
            Literal = literal;
            Line = line;
        }

        public override string ToString()
        {
            return $"{Type} {Lexeme} {Literal}";
        }
    }
}
