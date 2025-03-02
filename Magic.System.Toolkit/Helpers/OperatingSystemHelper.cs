using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Magic.GeneralSystem.Toolkit.Helpers
{
    public static class OperatingSystemHelper
    {
        /// <summary>
        /// Checks if the current operating system is within the list of supported OS platforms.
        /// If the provided list of supported platforms is empty or null, it is considered "all OS platforms allowed" and returns true.
        /// Otherwise, it returns true if the current OS is in the list, and false if it is not.
        /// </summary>
        /// <param name="supportedPlatforms">A collection of OS platforms that are allowed. If null or empty, all OS platforms are considered supported.</param>
        /// <param name="currentPlatform">The OS platform that needs to be validated.</param>
        /// <returns>True if the OS is supported or if no restrictions are set; otherwise, false.</returns>
        public static bool ValidateSupportedOs(IEnumerable<OSPlatform>? supportedPlatforms, OSPlatform currentPlatform)
        {
            // If the list is null or empty, all OS platforms are allowed.
            if (supportedPlatforms == null || !supportedPlatforms.Any())
                return true;

            // Check if the current OS is in the list of supported platforms.
            return supportedPlatforms.Contains(currentPlatform);
        }

        public static OSPlatform GetOperatingSystem()
        {
            var knownPlatforms = new List<OSPlatform>
            {
                OSPlatform.Windows,
                OSPlatform.OSX,
                OSPlatform.Linux,
                OSPlatform.FreeBSD
            };

            foreach (var platform in knownPlatforms)
            {
                if (RuntimeInformation.IsOSPlatform(platform))
                    return platform;
            }

            throw new Exception("Unknown Operating System");
        }
    }
}
