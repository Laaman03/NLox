using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLox.Lib.Parsing;

namespace NLox.Lib
{
    public class ErrorReporter
    {
        public bool HadError { get; private set; }
        public bool HadRuntimeError { get; private set; }
        public void Error(int line, string message)
        {
            Report(line, "", message);
        }
        public void Error(Token token, string message)
        {
            if (token.Type == TokenType.EOF)
            {
                Report(token.Line, " at end", message);
            }
            else
            {
                Report(token.Line, $" at '{token.Lexeme}", message);
            }
        }
        public void RuntimeError(RuntimeError error)
        {
            Console.Error.WriteLine(error.Message + $"\n[line {error.Token.Line}]");
            HadRuntimeError = true;
        }

        void Report(int line, string where, string message)
        {
            Console.Error.WriteLine($"[line {line}] Error {where}: {message}");
            HadError = true;
        }

        public void Reset()
        {
            HadError = false;
            HadRuntimeError = false;
        }
    }
}
