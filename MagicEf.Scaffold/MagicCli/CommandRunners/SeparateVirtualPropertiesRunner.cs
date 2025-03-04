using Magic.GeneralSystem.Toolkit.Helpers.Dotnet;
using Magic.GeneralSystem.Toolkit;
using MagicEf.Scaffold.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MagicEf.Scaffold.MagicCli.CommandRunners
{
    public class SeparateVirtualPropertiesRunner
    {

        public static async Task<bool> Run(PrimaryBaseProject primaryBaseProject)
        {
            string workingPath = primaryBaseProject.WorkingPath;
            /*string command = "--separateVirtualProperties "
               + $@"--directoryPath ""{primaryBaseProject.ScaffoldedModelsPath}"" "
               + $@"--outputPath ""{primaryBaseProject.SeparatedVirtualPropertiesPath}""";
*/
            string command = CommandHelper.CommandBuilder(new string[] {
            "--separateVirtualProperties",
            $@"--directoryPath ""{primaryBaseProject.ScaffoldedModelsPath}""",
            $@"--outputPath ""{primaryBaseProject.SeparatedVirtualPropertiesPath}"""
            });
            var response = await DotnetHelper.RunDotnetCommandAsync(command, workingPath, "MagicEF");
            return true;
        }
    }
}
