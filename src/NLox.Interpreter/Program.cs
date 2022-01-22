using System.Text;
using NLox.Lib;
using NLox.Lib.Parsing;

var reporter = new ErrorReporter();
var interpreter = new Interpreter(reporter);

if (args.Length > 1)
{
    Console.WriteLine("Usage: nlox [script]");
    System.Environment.Exit(64);
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

    if (reporter.HadError) System.Environment.Exit(65);
    if (reporter.HadRuntimeError) System.Environment.Exit(70);
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
        reporter.Reset();
    }
}

void Run(string source)
{
    var scanner = new Scanner(source, reporter);
    var tokens = scanner.ScanTokens();
    var parser = new Parser(tokens, reporter);
    var stmts = parser.Parse();
    var resolver = new Resolver(interpreter, reporter);

    if (reporter.HadError) return;

    resolver.Resolve(stmts);

    if (reporter.HadError) return;
    interpreter.Interpret(stmts);
}