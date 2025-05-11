namespace Brainfuck;

public class BrainFuckMachine(int bufferSize = 30000)
{
    public delegate void BrainfuckCommand(
        ref int commandPosition,
        ref int bufferPosition,
        ref byte cellValue,
        Dictionary<int, int> brackets);

    private Dictionary<char, BrainfuckCommand> _commands = [];

    public void RegisterCommand(char symbol, BrainfuckCommand command)
    {
        _commands[symbol] = command;
    }

    public void ParseAndRun(string program)
    {
        var bracketsMap = CreateBracketsMap(program);
        var bufferPosition = 0;

        Span<byte> buffer = stackalloc byte[bufferSize];
        var i = 0;
        
        while (i < program.Length)
        {
            var c = program[i];
            if (!_commands.TryGetValue(c, out var command))
            {
                i++;
                continue;
            }


            command(ref i, ref bufferPosition,
                ref buffer[bufferPosition], bracketsMap);
        }
    }

    private static Dictionary<int, int> CreateBracketsMap(
        string program)
    {
        var stack = new Stack<int>();
        var result = new Dictionary<int, int>();

        for (var i = 0; i < program.Length; i++)
        {
            var c = program[i];

            switch (c)
            {
                case '[':
                    stack.Push(i);
                    break;
                case ']':
                    var open = stack.Pop();
                    result[open] = i;
                    result[i] = open;
                    break;
            }
        }

        return result;
    }
    
}