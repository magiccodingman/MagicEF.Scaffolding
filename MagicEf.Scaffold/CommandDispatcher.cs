using MagicEf.Scaffold.CommandActions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MagicEf.Scaffold
{
    public class CommandDispatcher
    {
        public void Dispatch(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("No arguments provided. Use --help for usage information.");
                return;
            }

            string command = args[0];

            switch (command)
            {
                case "--ambiguousIndex":
                    var ambiguousIndexHandler = new AmbiguousIndexHandler();
                    ambiguousIndexHandler.Handle(args);
                    break;

                case "--removeConstructors":
                    var removeConstructorsHandler = new RemoveConstructorsHandler();
                    removeConstructorsHandler.Handle(args);
                    break;

                case "--removeOnConfiguring":
                    var removeOnConfiguringHandler = new RemoveOnConfiguringHandler();
                    removeOnConfiguringHandler.Handle(args);
                    break;

                case "--scaffoldProtocol":
                    var scaffoldProtocolHandler = new ScaffoldProtocolHandler();
                    scaffoldProtocolHandler.Handle(args);
                    break;

                case "--dbHelpers":
                    var dbHelpersHandler = new DbHelpersHandler();
                    dbHelpersHandler.Handle(args);
                    break;

                case "--separateVirtualProperties":
                    var separateVirtualPropertiesHandler = new SeparateVirtualPropertiesHandler();
                    separateVirtualPropertiesHandler.Handle(args);
                    break;

                default:
                    Console.WriteLine("Unknown command. Use --help for usage information.");
                    break;
            }
        }
    }

    public abstract class CommandHandlerBase
    {
        public abstract void Handle(string[] args);
    }
}
