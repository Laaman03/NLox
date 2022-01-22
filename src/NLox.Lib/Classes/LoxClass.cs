using System;
using NLox.Lib.Functions;
using System.Collections.Generic;

namespace NLox.Lib.Classes
{
    internal class LoxClass : ILoxCallable
    {
        public string Name { get; private init; }
        private Dictionary<string, LoxFunction> _methods;
        public int Arity
        {
            get
            {
                var ctor = FindMethod("init");
                if (ctor != null) return ctor.Arity;
                else return 0;
            }
        }
        public LoxClass(string name, Dictionary<string, LoxFunction> methods)
        {
            Name = name;
            _methods = methods;
        }

        public object Call(Interpreter interpreter, List<object> args)
        {
            var instance = new LoxInstance(this);
            var ctor = FindMethod("init");
            if (ctor != null)
            {
                ctor.Bind(instance).Call(interpreter, args);
            }
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