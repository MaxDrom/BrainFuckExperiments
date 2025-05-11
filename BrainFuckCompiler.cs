using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;


namespace Brainfuck;

public class BrainFuckCompiler
{
    private Dictionary<char, Func<string>> _commands = [];

    public BrainFuckCompiler()
    {
        _commands['.'] =
            () => "Console.Write((char)buffer[position]);";
        _commands['>'] =
            () => "position++;";
        _commands['<'] =
            () => "position--;";
        _commands['+'] = () => "buffer[position]++;";
        _commands['-'] = () => "buffer[position]--;";
        _commands[','] =
            () => "buffer[position] = (byte)Console.Read();";

        _commands['['] = () => "while (buffer[position] != 0) {";
        _commands[']'] = () => "}";
    }

    public Action Compile(string code)
    {
        var resultCode = new StringBuilder();

        resultCode.AppendLine(
            "Span<byte> buffer = stackalloc byte[1024];");
        resultCode.AppendLine("int position = 0;");


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
    public static void Run()
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
            [..refs,MetadataReference.CreateFromFile(typeof(Console).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind
                .DynamicallyLinkedLibrary));

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