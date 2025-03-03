using Magic.Cli.Toolkit;
using MagicEf.Scaffold.MagicCli.CommandRunners;
using MagicEf.Scaffold.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MagicEf.Scaffold.CommandActions
{
    public partial class MagicCliHandler
    {
        private async Task ProfileCommandRun(ProjectProfile profile)
        {
            Console.Clear();

            bool Successful = await DbFirstScaffoldRunner.Run(profile);

            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("Click Enter to go back to the profile menu...");
            Console.Read();

            await SelectProfile(profile);
        }
    }
}
