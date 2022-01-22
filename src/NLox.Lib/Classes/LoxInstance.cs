using System;
using NLox.Lib;
using NLox.Lib.Parsing;
using System.Collections.Generic;

namespace NLox.Lib.Classes
{
    internal class LoxInstance
    {
        private LoxClass @class;
        private Dictionary<string, object> props = new();

        public LoxInstance(LoxClass loxClass)
        {
            @class = loxClass;
            props.Add("__zero", 0);
        }

        public override string ToString() => $"{@class.Name} instance";

        internal object GetProp(Token name)
        {
            if (props.TryGetValue(name.Lexeme, out var value))
            {
                return value;
            }

            var method = @class.FindMethod(name.Lexeme);
            if (method != null) return method.Bind(this);

            throw new RuntimeError(name, $"Undefined property {name.Lexeme}.");
        }

        internal void Set(Token name, object value)
        {
            props[name.Lexeme] = value;
        }
    }
}