using Magic.GeneralSystem.Toolkit.Helpers;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Magic.CodeGen.Toolkit.Helpers
{
    public static class MsBuildHelper
    {
        public static Compilation GetProjectCompilation(string directory)
        {
            string normalizedDirectory = DirectoryHelper.NormalizePath(directory);
            Console.WriteLine($"Initializing Roslyn compilation for: {normalizedDirectory}");

            try
            {
                using var workspace = MSBuildWorkspace.Create();

                var csprojFiles = Directory.GetFiles(normalizedDirectory, "*.csproj", SearchOption.TopDirectoryOnly);
                if (csprojFiles.Length == 0)
                    throw new FileNotFoundException($"No .csproj file found in {normalizedDirectory}.");

                string csprojFile = csprojFiles.FirstOrDefault(f => !f.Contains("Backup", StringComparison.OrdinalIgnoreCase))
                                    ?? csprojFiles.First();

                Console.WriteLine($"Opening project: {csprojFile}");

                var project = workspace.OpenProjectAsync(csprojFile).GetAwaiter().GetResult();
                var _comp = project.GetCompilationAsync().GetAwaiter().GetResult();

                if (project == null)
                    throw new Exception($"Failed to compile project: {csprojFile}");

                Console.WriteLine("Successfully loaded project into Roslyn.");
                return _comp;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing Roslyn Compilation: {ex.Message}");
                throw;
            }
        }
    }
}
