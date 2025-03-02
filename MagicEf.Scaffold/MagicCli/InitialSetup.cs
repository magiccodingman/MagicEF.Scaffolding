using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Magic.GeneralSystem.Toolkit;

namespace MagicEf.Scaffold.MagicCli
{
    internal class InitialSetup
    {
        /// <summary>
        /// This is currently the Magic EF's supported platforms.
        /// </summary>
        private readonly List<OSPlatform> SupportedOperatingSystems = new List<OSPlatform>()
        {
            OSPlatform.Windows,
            OSPlatform.Linux,
            OSPlatform.OSX,
        };
        public async Task<AppConfig> Initialize()
        {
            AppConfig appConfig = await new MagicSystemInitialize(SupportedOperatingSystems, true).BuildAppConfig();
            return appConfig;
        }
    }
}
