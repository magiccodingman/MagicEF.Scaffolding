using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MagicEf.Scaffold.Services
{
    public class RoslynCompilationService
    {
        private Compilation _compilation;
        private readonly Dictionary<string, ClassDeclarationSyntax> _classCache = new();
        private readonly Dictionary<string, InterfaceDeclarationSyntax> _interfaceCache = new();

        public RoslynCompilationService(string projectDirectory)
        {
            EnsureMSBuildRegistered();
            InitializeCompilation(projectDirectory);
        }

        private void EnsureMSBuildRegistered()
        {
            if (!MSBuildLocator.IsRegistered)
            {
                var instances = MSBuildLocator.QueryVisualStudioInstances().ToList();
                var instance = instances.OrderByDescending(i => i.Version).FirstOrDefault();

                if (instance == null)
                    throw new InvalidOperationException("No valid MSBuild instances found!");

                MSBuildLocator.RegisterInstance(instance);
                Console.WriteLine($"Registered MSBuild from: {instance.MSBuildPath}");
            }
        }

        private Compilation InitializeCompilation(string directory)
        {
            string normalizedDirectory = NormalizeDirectory(directory);
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

        private async Task InitializeCompilationAsync(string directory)
        {
            string normalizedDirectory = NormalizeDirectory(directory);
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
                _compilation = await project.GetCompilationAsync();

                if (_compilation == null)
                    throw new Exception($"Failed to compile project: {csprojFile}");

                Console.WriteLine("Successfully loaded project into Roslyn.");
                CacheProjectDeclarations();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing Roslyn Compilation: {ex.Message}");
                throw;
            }
        }

        private void CacheProjectDeclarations()
        {
            foreach (var syntaxTree in _compilation.SyntaxTrees)
            {
                var root = syntaxTree.GetRoot();
                foreach (var classDecl in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
                {
                    _classCache[classDecl.Identifier.Text] = classDecl;
                }

                foreach (var interfaceDecl in root.DescendantNodes().OfType<InterfaceDeclarationSyntax>())
                {
                    _interfaceCache[interfaceDecl.Identifier.Text] = interfaceDecl;
                }
            }
        }

        public Compilation GetCompilation() => _compilation;

        public Dictionary<string, ClassDeclarationSyntax> GetClassCache() => _classCache;

        public Dictionary<string, InterfaceDeclarationSyntax> GetInterfaceCache() => _interfaceCache;

        private string NormalizeDirectory(string directory)
        {
            return Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar).ToLowerInvariant();
        }
    }
}
