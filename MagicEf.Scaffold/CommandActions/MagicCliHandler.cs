using Magic.CLI.Helpers.Dotnet;
using Magic.CLI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MagicEf.Scaffold.CommandActions
{
    public class MagicCliHandler : CommandHandlerBase, IAsyncCommandHandler
    {
        public override void Handle(string[] args)
        {
            _ = HandleAsync(args); // Fire and forget
        }

        public async Task HandleAsync(string[] args)
        {
            MagicCliResponse response = await ValidateDotnet.ValidateDotnetAvailabilityAsync();
        }
    }
}
