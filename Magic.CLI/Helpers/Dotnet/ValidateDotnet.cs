using Magic.CLI.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Magic.CLI.Helpers.Dotnet
{
    public static class ValidateDotnet
    {
        /// <summary>
        /// Validates if the .NET SDK and dotnet tool commands are available.
        /// </summary>
        /// <returns>A MagicCliResponse indicating success or failure.</returns>
        public static async Task<MagicCliResponse> ValidateDotnetAvailabilityAsync()
        {
            var response = new MagicCliResponse();

            // Check if "dotnet" is available
            if (!await IsCommandAvailableAsync("dotnet --version"))
            {
                response.Success = false;
                response.Message = ".NET SDK is not installed or not available in PATH.";
                return response;
            }

            // Check if "dotnet tool" is available
            if (!await IsCommandAvailableAsync("dotnet tool list -g"))
            {
                response.Success = false;
                response.Message = ".NET Tool system is not available. Ensure .NET SDK is properly installed.";
                return response;
            }

            return response;
        }

        /// <summary>
        /// Executes a command in the system shell and checks if it runs successfully.
        /// </summary>
        /// <param name="command">The command to check.</param>
        /// <returns>True if the command executes successfully, otherwise false.</returns>
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
        /// Determines the correct shell executable based on the operating system.
        /// </summary>
        private static string GetShellExecutable()
        {
            return OperatingSystem.IsWindows() ? "cmd.exe" : "/bin/bash";
        }

        /// <summary>
        /// Gets the correct argument prefix for executing commands.
        /// </summary>
        private static string GetShellArgumentPrefix()
        {
            return OperatingSystem.IsWindows() ? "/c " : "-c ";
        }

    }
}
