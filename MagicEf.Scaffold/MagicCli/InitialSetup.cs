using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Magic.GeneralSystem.Toolkit;
using MagicEf.Scaffold.Settings;
using Magic.GeneralSystem.Toolkit.Helpers.Dotnet;
using Magic.GeneralSystem.Toolkit.Settings;

namespace MagicEf.Scaffold.MagicCli
{
    internal class InitialSetup
    {
        public static readonly string storageFolderName = "MagicEF";
        public static readonly string GeneralSettingsName = "GeneralCliSettings";

        /// <summary>
        /// This is currently the Magic EF's supported platforms.
        /// </summary>
        private readonly List<OSPlatform> SupportedOperatingSystems = new List<OSPlatform>()
        {
            OSPlatform.Windows,
            OSPlatform.Linux,
            OSPlatform.OSX,
        };

        public async Task<(AppConfig, GeneralCliSettings)> Initialize()
        {
            AppConfig appConfig = await new MagicSystemInitialize(storageFolderName, SupportedOperatingSystems, true).BuildAppConfig();
            if (string.IsNullOrWhiteSpace(appConfig.PreferredStoragePath))
                throw new Exception("The MagicEF app couldn't find any location in which it had any file storage permissions.");

            GeneralCliSettings generalSettings = new GeneralCliSettings(appConfig.PreferredStoragePath, GeneralSettingsName);

            bool global = false;
            if (appConfig.InstalledLocation == InstallLocation.Global)
                global = true;

            await DotnetHelper.InstallOrUpdateToolAsync("MagicEf", generalSettings.GetAutoUpdate(), global);

            return (appConfig, generalSettings);
        }
    }
}
