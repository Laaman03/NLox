using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLox.Lib.Classes;

namespace NLox.Lib.Functions
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

        internal LoxFunction Bind(LoxInstance instance)
        {
            var env = new Environment(closure);
            env.Define("this", instance);
            return new LoxFunction(_decl, env);
        }
        public override string ToString()
        {
            return $"<fn {_decl.Name.Lexeme}>";
        }
    }
}
