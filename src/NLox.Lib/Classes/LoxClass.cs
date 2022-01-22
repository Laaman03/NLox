using System;
using NLox.Lib.Functions;
using System.Collections.Generic;

namespace NLox.Lib.Classes
{
    internal class LoxClass : ILoxCallable
    {
        public string Name { get; private init; }
        private Dictionary<string, LoxFunction> _methods;
        public LoxClass(string name, Dictionary<string, LoxFunction> methods)
        {
            Name = name;
            _methods = methods;
        }

        public int Arity => 0;

        public object Call(Interpreter interpreter, List<object> args)
        {
            var instance = new LoxInstance(this);
            return instance;
        }

        public override string ToString()
        {
            return Name;
        }

        internal LoxFunction FindMethod(string name)
        {
            if (_methods.TryGetValue(name, out var method))
            {
                return method;
            }
            return null;
        }
    }
}