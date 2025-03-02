using Magic.GeneralSystem.Toolkit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Magic.GeneralSystem.Toolkit.Helpers.Dotnet
{
    public static class DotnetHelper
    {
        /// <summary>
        /// Runs a dotnet command, streams output in real-time, and returns the result.
        /// </summary>
        /// <param name="arguments">The dotnet CLI arguments (e.g., "tool list -g")</param>
        /// <returns>A MagicCliResponse containing success status and full output.</returns>
        public static async Task<MagicSystemResponse> RunDotnetCommandAsync(string arguments)
        {
            var response = new MagicSystemResponse();
            var outputBuilder = new StringBuilder();

            try
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = processStartInfo };

                // Capture output and errors in real-time
                process.OutputDataReceived += (sender, args) =>
                {
                    if (!string.IsNullOrWhiteSpace(args.Data))
                    {
                        Console.WriteLine(args.Data);
                        outputBuilder.AppendLine(args.Data);
                    }
                };

                process.ErrorDataReceived += (sender, args) =>
                {
                    if (!string.IsNullOrWhiteSpace(args.Data))
                    {
                        Console.WriteLine(args.Data);
                        outputBuilder.AppendLine(args.Data);
                    }
                };

                // Start process and begin capturing output
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                // Wait for process to exit
                await process.WaitForExitAsync();

                // Set success flag based on exit code
                response.Success = process.ExitCode == 0;
                response.Message = outputBuilder.ToString().Trim();
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Error running dotnet command: {ex.Message}";
            }

            return response;
        }
    }
}
