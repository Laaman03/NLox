using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NLox.Lib.Functions
{
    public interface ILoxCallable
    {
        int Arity { get; }
        object Call(Interpreter interpreter, List<object> args);
    }
}
