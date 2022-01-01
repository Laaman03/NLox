using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NLox.Lib
{
    public static class Common
    {
        static void Error(int line, string message)
        {
            Report(line, "", message);
        }

        static void Report(int line, string where, string message)
        {
            Console.Error.WriteLine($"[line {line}] Error {where}: {message}");
        }
    }
}
