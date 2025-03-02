using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Magic.GeneralSystem.Toolkit.Settings
{
    public enum InstallLocation
    {
        /// <summary>
        /// Something went wrong, and app should have been shut down
        /// </summary>
        Error = -1,

        /// <summary>
        /// The CLI app was installed with the dotnet tools --global
        /// </summary>
        Global = 0,

        /// <summary>
        /// The CLI app was installed with the dotnet tools --local 
        ///  to a specific project location.
        /// </summary>
        Local = 1,

        /// <summary>
        /// The CLI app was installed in a custom directory.
        /// </summary>
        CustomLocation = 2
    }
}
