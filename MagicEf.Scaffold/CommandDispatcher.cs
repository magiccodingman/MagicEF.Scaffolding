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
        public async Task DispatchAsync(string[] args)
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

                case "--initialSetup":
                    var initialSetupHandler = new InitialSetupHandler();
                    initialSetupHandler.Handle(args);
                    break;

                case "--initialShareSetupHandler":
                    var initialShareSetupHandler = new InitialShareSetupHandler();
                    initialShareSetupHandler.Handle(args);
                    break;

                case "--shareScaffoldProtocolHandler":
                    var shareScaffoldProtocolHandler = new ShareScaffoldProtocolHandler();
                    shareScaffoldProtocolHandler.Handle(args);
                    break;

                case "--migrationRunner":
                    var migrationRunnerHandler = new MigrationRunnerHandler();
                    migrationRunnerHandler.Handle(args);
                    break;

                case "--flattenShareProtocol":
                    var flattenShareProtocolHandler = new FlattenShareProtocolHandler();
                    flattenShareProtocolHandler.Handle(args);
                    break;

                case "--cli":
                    var magicCliHandler = new MagicCliHandler();
                    await magicCliHandler.HandleAsync(args);
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
