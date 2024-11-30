using System;
using MagicEf.Scaffold;
class Program
{
    static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("No arguments provided. Use --help for usage information.");
            return;
        }

        var dispatcher = new CommandDispatcher();
        dispatcher.Dispatch(args);
    }
}
