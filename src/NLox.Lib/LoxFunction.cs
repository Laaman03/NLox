using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NLox.Lib
{
    public class LoxFunction : ILoxCallable
    {
        private readonly Stmt.Function _decl;
        private readonly Environment closure;
        public LoxFunction(Stmt.Function decl, Environment environment)
        {
            _decl = decl;
            closure = environment;
        }
        public int Arity => _decl.Parameters.Count;
        public object Call(Interpreter interpreter, List<object> args)
        {
            var env = new Environment(closure);
            for (int i = 0; i < args.Count; i++)
            {
                env.Define(_decl.Parameters[i].Lexeme, args[i]);
            }
            try
            {
                interpreter.ExecuteBlock(_decl.Body, env);
            }
            catch (Return returnValue)
            {
                return returnValue.Value;
            }
            return null;
        }
        public override string ToString()
        {
            return $"<fn {_decl.Name.Lexeme}>";
        }
    }
}
