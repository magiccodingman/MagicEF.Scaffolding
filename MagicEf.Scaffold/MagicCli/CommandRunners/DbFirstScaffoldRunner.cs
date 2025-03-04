using Magic.GeneralSystem.Toolkit;
using Magic.GeneralSystem.Toolkit.Helpers;
using Magic.GeneralSystem.Toolkit.Helpers.Dotnet;
using MagicEf.Scaffold.Models;
using MagicEf.Scaffold.Settings;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MagicEf.Scaffold.MagicCli.CommandRunners
{
    public static class DbFirstScaffoldRunner
    {
        public static async Task<bool> Run(PrimaryBaseProject primaryBaseProject)
        {
            string workingPath = primaryBaseProject.WorkingPath;
            List<MagicSystemResponse>  installPackagesResponses = await DotnetHelper.ValidateAndInstallPackages(workingPath,
                new string[] { 
                    "Microsoft.EntityFrameworkCore.SqlServer",
                    "Microsoft.EntityFrameworkCore.Tools", 
                    "Microsoft.EntityFrameworkCore.Design",
                    "Microsoft.EntityFrameworkCore.Proxies"});


            

            return true;
        }

    }
}
