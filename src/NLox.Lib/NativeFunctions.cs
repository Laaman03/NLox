using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NLox.Lib
{
    internal class NativeFuncClock : ILoxCallable
    {
        public int Arity { get => 0; }
        public object Call(Interpreter interpreter, List<object> _) => System.Environment.TickCount/1000;
    }
}
