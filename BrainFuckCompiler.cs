using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;


namespace Brainfuck;

public class BrainFuckCompiler
{
    private readonly Dictionary<char, Func<string>> _commands = [];

    public BrainFuckCompiler()
    {
        _commands['.'] =
            () => "Console.Write((char)(*p));";
        _commands['>'] =
            () => "p++;";
        _commands['<'] =
            () => "p--;";
        _commands['+'] = () => "(*p)++;";
        _commands['-'] = () => "(*p)--;";
        _commands[','] =
            () => "p[0] = (byte)Console.Read();";

        _commands['['] = () => "while ((*p) != 0) {";
        _commands[']'] = () => "}";
    }

    public Action Compile(string code)
    {
        var resultCode = new StringBuilder();

        resultCode.AppendLine(
            "var p = stackalloc byte[30000];");

        foreach (var c in code)
        {
            if (!_commands.TryGetValue(c, out var func))
                continue;

            resultCode.AppendLine(func());
        }

        string fullCode = $@"
using System;

public static class DynamicCode
{{
    public unsafe static void Run()
    {{
        {resultCode}
    }}
}}";

        var tree = CSharpSyntaxTree.ParseText(fullCode);

        var refs = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a =>
                !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
            .Select(a =>
                MetadataReference.CreateFromFile(a.Location));

        var compilation = CSharpCompilation.Create(
            "DynamicAssembly",
            [tree],
            [
                ..refs,
                MetadataReference.CreateFromFile(typeof(Console)
                    .Assembly.Location)
            ],
            new CSharpCompilationOptions(OutputKind
                .DynamicallyLinkedLibrary, allowUnsafe:true));

        using var ms = new MemoryStream();
        var result = compilation.Emit(ms);

        if (!result.Success)
        {
            throw new Exception("Compilation failed:\n" +
                                string.Join('\n',
                                    result.Diagnostics.Select(d =>
                                        d.ToString())));
        }

        var assembly = Assembly.Load(ms.ToArray());
        var type = assembly.GetType("DynamicCode");
        var method = type!.GetMethod("Run");

        return () => method!.Invoke(null, null);
    }
}