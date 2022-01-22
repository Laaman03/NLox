using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLox.Lib.Classes;
using NLox.Lib.Parsing;

namespace NLox.Lib.Functions
{
    public class LoxFunction : ILoxCallable
    {
        private readonly Stmt.Function _decl;
        private readonly Environment _closure;
        private readonly bool _isInitialzer;
        public LoxFunction(Stmt.Function decl, Environment closure, bool isInitialzer)
        {
            _decl = decl;
            _closure = closure;
            _isInitialzer = isInitialzer;
        }
        public int Arity => _decl.Parameters.Count;
        public object Call(Interpreter interpreter, List<object> args)
        {
            var env = new Environment(_closure);
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
                if (_isInitialzer) return _closure.GetAt(0, "this");
                return returnValue.Value;
            }
            return null;
        }

        internal LoxFunction Bind(LoxInstance instance)
        {
            var env = new Environment(_closure);
            env.Define("this", instance);
            return new LoxFunction(_decl, env, _isInitialzer);
        }
        public override string ToString()
        {
            return $"<fn {_decl.Name.Lexeme}>";
        }
    }
}
