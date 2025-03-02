using System;
using MagicEf.Scaffold;
class Program
{
    static void Main(string[] args)
    {

#if DEBUG
        if (args.Length == 0)
        {
            args = new string[] { "--cli" };
        }
#else
        if (args.Length == 0)
        {
            Console.WriteLine("No arguments provided. Use --help for usage information.");
            return;
        }
#endif



        var dispatcher = new CommandDispatcher();
        dispatcher.Dispatch(args);
    }
}
