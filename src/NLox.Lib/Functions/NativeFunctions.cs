using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLox.Lib;
using NLox.Lib.Parsing;

namespace NLox.Lib.Functions
{
    internal abstract class NativeFunction : ILoxCallable
    {
        private string _name;
        public int Arity { get; private set; }

        public NativeFunction(string name, int arity)
        {
            _name = name;
            Arity = arity;
        }

        public abstract object Call(Interpreter interpreter, List<object> args);
        public void AddToEnv(Environment env)
        {
            env.Define(_name, this);
        }
    }

    internal class NativeFuncClock : NativeFunction
    {
        public NativeFuncClock() : base("clock", 0) { }
        public override object Call(Interpreter _, List<object> __) => System.Environment.TickCount / 1000;
    }

    internal class NativeFuncVersion : NativeFunction
    {
        public NativeFuncVersion() : base("maxInt", 0) { }
        public override object Call(Interpreter interpreter, List<object> args) => int.MaxValue;
    }

    internal static class NativeFunctions
    {
        static NativeFunction[] funcs = new NativeFunction[]
        {
            new NativeFuncClock(),
            new NativeFuncVersion(),
        };

        public static void DefineGlobals(Environment env)
        {
            foreach (var func in funcs)
            {
                func.AddToEnv(env);
            }
        }
    }
}
