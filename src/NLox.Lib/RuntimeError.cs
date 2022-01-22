using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLox.Lib.Parsing;

namespace NLox.Lib
{
    public class RuntimeError : Exception
    {
        public Token Token { get; private init; }

        public RuntimeError(Token token, string message) : base(message)
        {
            Token = token;
        }
    }
}
