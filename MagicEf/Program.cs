using System;
using MagicEf.Scaffold;
class Program
{
    static async Task Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("No arguments provided. Defaulting user to the cli text GUI.");
            args = new string[] { "--cli" };
        }



        var dispatcher = new CommandDispatcher();
        await dispatcher.DispatchAsync(args);
    }
}
