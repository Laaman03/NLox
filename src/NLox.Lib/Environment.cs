using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NLox.Lib
{
    public class Environment
    {
        private readonly Dictionary<string, object> values = new();
        private readonly Environment _enclosing;

        public Environment()
        {
            _enclosing = null;
        }

        public Environment(Environment enclosing)
        {
            _enclosing = enclosing;
        }

        public void Define(string name, object value)
        {
            values[name] = value;
        }

        public void Assign(Token name, object value)
        {
            if (values.ContainsKey(name.Lexeme))
            {
                values[name.Lexeme] = value;
                return;
            }
            if (_enclosing is not null)
            {
                _enclosing.Assign(name, value);
            }

            throw new RuntimeError(name, "Variable not found.");
        }

        public void AssignAt(int distance, Token name, object value)
        {
            Ancestor(distance).values[name.Lexeme] = value;
        }

        public object Get(Token name)
        {
            if (values.TryGetValue(name.Lexeme, out object value))
            {
                return value;
            }
            if (_enclosing is not null)
            {
                return _enclosing.Get(name);
            }
            throw new RuntimeError(name, $"Undefined variable '{name.Lexeme}'.");
        }

        public object GetAt(int distance, String name)
        {
            return Ancestor(distance).values[name];
        }

        public Environment Ancestor(int distance)
        {
            var env = this;
            for (int i = 0; i < distance; i++)
            {
                env = env._enclosing;
            }
            return env;
        }

    }
}
