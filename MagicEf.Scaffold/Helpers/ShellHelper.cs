using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MagicEf.Scaffold.Helpers
{
    public class ShellHelper
    {
        /// <summary>
        /// Utility: runs a shell command synchronously, returns stdout/stderr and exit code.
        /// </summary>
        public (int exitCode, string output) RunShellCommand(string command, string arguments, string workingDirectory)
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using (var process = new Process { StartInfo = processStartInfo })
            {
                process.Start();
                string stdout = process.StandardOutput.ReadToEnd();
                string stderr = process.StandardError.ReadToEnd();
                process.WaitForExit();
                var exitCode = process.ExitCode;

                // Combine stdout + stderr for convenience
                string combinedOutput = stdout;
                if (!string.IsNullOrWhiteSpace(stderr))
                {
                    combinedOutput += Environment.NewLine + stderr;
                }

                return (exitCode, combinedOutput);
            }
        }
    }
}
