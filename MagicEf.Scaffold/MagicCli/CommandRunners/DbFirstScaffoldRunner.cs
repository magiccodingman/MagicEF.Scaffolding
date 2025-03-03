using Magic.GeneralSystem.Toolkit;
using Magic.GeneralSystem.Toolkit.Helpers.Dotnet;
using MagicEf.Scaffold.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MagicEf.Scaffold.MagicCli.CommandRunners
{
    public static class DbFirstScaffoldRunner
    {
        public static async Task<bool> Run(ProjectProfile profile)
        {
            List<MagicSystemResponse>  installPackagesResponses = await DotnetHelper.ValidateAndInstallPackages(profile.PrimaryProjectPath,
                new string[] { 
                    "Microsoft.EntityFrameworkCore.SqlServer",
                    "Microsoft.EntityFrameworkCore.Tools", 
                    "Microsoft.EntityFrameworkCore.Design",
                    "Microsoft.EntityFrameworkCore.Proxies"});

            return true;
        }

    }
}
