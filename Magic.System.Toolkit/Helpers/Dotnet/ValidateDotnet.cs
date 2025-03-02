using Magic.GeneralSystem.Toolkit;
using Magic.GeneralSystem.Toolkit.Settings;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Magic.GeneralSystem.Toolkit.Helpers.Dotnet
{
    public static class ValidateDotnet
    {
        /// <summary>
        /// Validates if the .NET SDK and dotnet tool commands are available.
        /// </summary>
        /// <returns>A MagicCliResponse indicating success or failure.</returns>
        public static async Task<MagicSystemResponse<InstallLocation>> ValidateDotnetAvailabilityAsync()
        {
            var response = new MagicSystemResponse<InstallLocation>
            {
                Success = true,
                Message = "Validated .NET command availability",
            };

            // Check if "dotnet" command is accessible
            if (!await IsCommandAvailableAsync("dotnet --version"))
            {
                response.Success = false;
                response.Message = ".NET SDK is not installed or not available in PATH.";
                response.Result = InstallLocation.Error;
                return response;
            }

            // Check if "dotnet tool" is available
            if (!await IsCommandAvailableAsync("dotnet tool list -g") && !IsDotnetToolInstalledLocally())
            {
                response.Success = false;
                response.Message = ".NET Tool system is not available. Ensure .NET SDK is properly installed.";
                response.Result = InstallLocation.Error;
                return response;
            }

            // Determine installation location
            if (await IsCommandAvailableAsync("dotnet tool list -g") && await IsCommandAvailableAsync("dotnet tool run MagicEF --help"))
            {
                response.Result = InstallLocation.Global;
            }
            else if (IsDotnetToolInstalledLocally())
            {
                response.Result = InstallLocation.Local;
            }
            else
            {
                response.Success = false;
                response.Message = "MagicEF is not installed globally or locally. Run 'dotnet tool install --global MagicEF' or use a local install.";
                response.Result = InstallLocation.Error;
            }

            return response;
        }

        /// <summary>
        /// Executes a command in the system shell and checks if it runs successfully.
        /// </summary>
        private static async Task<bool> IsCommandAvailableAsync(string command)
        {
            try
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = GetShellExecutable(),
                    Arguments = GetShellArgumentPrefix() + command,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = processStartInfo };
                process.Start();
                await process.WaitForExitAsync();
                return process.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Determines if the CLI app is installed locally (per-project).
        /// </summary>
        private static bool IsDotnetToolInstalledLocally()
        {
            var localConfigPath = Path.Combine(Directory.GetCurrentDirectory(), ".config", "dotnet-tools.json");
            if (!File.Exists(localConfigPath)) return false;

            try
            {
                var json = File.ReadAllText(localConfigPath);
                var tools = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                return tools != null && tools.ContainsKey("MagicEF");
            }
            catch
            {
                return false;
            }
        }

        private static string GetShellExecutable() => OperatingSystem.IsWindows() ? "cmd.exe" : "/bin/bash";
        private static string GetShellArgumentPrefix() => OperatingSystem.IsWindows() ? "/c " : "-c ";
    }

}
