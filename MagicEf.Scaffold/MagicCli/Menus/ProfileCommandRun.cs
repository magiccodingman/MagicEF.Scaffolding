using Magic.Cli.Toolkit;
using MagicEf.Scaffold.MagicCli.CommandRunners;
using MagicEf.Scaffold.Models;
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

            var primaryBaseProject = new PrimaryBaseProject(profile);

            if (profile.ProjectSpecificSettings.RunDatabaseFirstScaffolding == true)
            {
                //await DbFirstScaffoldRunner.Run(primaryBaseProject);

                // RUn for the user the, "ambiguousIndex" and "removeOnConfiguring"
            }
            if (profile.ProjectSpecificSettings.RunSeparateVirtualProperties == true)
                await SeparateVirtualPropertiesRunner.Run(primaryBaseProject);

            if (profile.ProjectSpecificSettings.RunMagicIndexedDbScaffolding == true)
            {
                await MagicIndexedDbRunner.Run(primaryBaseProject);
            }

                Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("Click Enter to go back to the profile menu...");
            Console.Read();
            await SelectProfile(profile);
        }
    }
}
