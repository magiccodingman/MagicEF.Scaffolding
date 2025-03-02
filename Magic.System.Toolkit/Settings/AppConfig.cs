using Magic.GeneralSystem.Toolkit.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Magic.GeneralSystem.Toolkit
{
    
    public class AppConfig
    {
        public InstallLocation InstalledLocation { get; set; } = InstallLocation.CustomLocation;

        /// <summary>
        /// Set this to true if dotnet is required and/or used within the CLI app
        /// </summary>
        public bool RequiresDotnet { get; set; } = false;

        public OSPlatform OperatingSystem { get; set; }
    }
}
