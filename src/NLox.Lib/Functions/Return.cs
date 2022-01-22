using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NLox.Lib.Functions
{
    public class Return : Exception
    {
        public object Value { get; private init; }
        public Return(object value)
        {
            Value = value;
        }
    }
}
