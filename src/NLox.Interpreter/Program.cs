using System.Text;
using NLox.Lib;
using static NLox.Lib.Common;

var reporter = new ErrorReporter();
var interpreter = new Interpreter(reporter);

if (args.Length > 1)
{
    Console.WriteLine("Usage: nlox [script]");
    Environment.Exit(64);
}
else if (args.Length == 1)
{
    RunFile(args[0]);
}
else
{
    RunPrompt();
}

void RunFile(string filename)
{
    byte[] bytes = File.ReadAllBytes(filename);
    Run(Encoding.UTF8.GetString(bytes));

    if (reporter.HadError) Environment.Exit(65);
    if (reporter.HadRuntimeError) Environment.Exit(70);
}

void RunPrompt()
{
    var reader = new StreamReader(Console.OpenStandardInput());

    while (true)
    {
        Console.Write("> ");
        var line = reader.ReadLine();
        if (line == null) break;
        Run(line);
    }
}

void Run(string source)
{
    var scanner = new Scanner(source, reporter);
    var tokens = scanner.ScanTokens();
    var parser = new Parser(tokens, reporter);
    var expr = parser.Parse();
    interpreter.Interpret(expr);

    if (reporter.HadError) return;


}