using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MagicEf.Scaffold.CommandActions
{
    public class InitialSetupHandler : CommandHandlerBase
    {
        private static readonly string[] RequiredFolders = new[]
    {
        "Concrete\\",
        "DbHelpers\\",
        "DbModels\\",
        "Extensions\\",
        "Interfaces\\",
        "MetaDataClasses\\"
    };

        private static readonly string[] RequiredPackages = new[]
        {
        "Microsoft.EntityFrameworkCore.Design",
        "Microsoft.EntityFrameworkCore.Proxies",
        "Microsoft.EntityFrameworkCore.SqlServer",
        "Microsoft.EntityFrameworkCore.Tools"
    };

        /// <summary>
        /// Main entry method that does:
        /// 1) Argument validation
        /// 2) .csproj modifications
        /// 3) Directory creation
        /// 4) Package reference checks
        /// 5) File creation (ReadOnlyDbContext.cs, {dbContext}.cs)
        /// </summary>
        public override void Handle(string[] args)
        {
            Console.WriteLine("Starting Initial Setup Handler...");

            if (args == null || !args.Any())
            {
                Console.WriteLine("Error: No arguments provided. Please include --projectFilePath, --namespace, and --dbContext.");
                return;
            }

            string projectFilePath = FileHelper.NormalizePath(ArgumentHelper.GetArgumentValue(args, "--projectFilePath"));
            string nameSpace = ArgumentHelper.GetArgumentValue(args, "--namespace");
            string dbContext = ArgumentHelper.GetArgumentValue(args, "--dbContext");

            Console.WriteLine("Validating arguments...");
            if (string.IsNullOrEmpty(projectFilePath))
            {
                Console.WriteLine("Error: --projectFilePath argument is missing or invalid.");
                return;
            }

            if (!File.Exists(projectFilePath))
            {
                Console.WriteLine($"Error: Project file not found at {projectFilePath}");
                return;
            }

            if (!projectFilePath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"Error: The provided path does not point to a .csproj file: {projectFilePath}");
                return;
            }

            if (string.IsNullOrEmpty(nameSpace))
            {
                Console.WriteLine("Error: --namespace argument is missing or invalid.");
                return;
            }

            if (string.IsNullOrEmpty(dbContext))
            {
                Console.WriteLine("Error: --dbContext argument is missing or invalid.");
                return;
            }

            var projectDir = Path.GetDirectoryName(projectFilePath);
            if (string.IsNullOrEmpty(projectDir))
            {
                Console.WriteLine($"Error: Unable to determine directory for project file: {projectFilePath}");
                return;
            }

            Console.WriteLine($"Project directory: {projectDir}");
            Console.WriteLine($"Namespace: {nameSpace}");
            Console.WriteLine($"DbContext: {dbContext}");

            var errors = new List<string>();

            Console.WriteLine("Step 1: Ensuring folders in .csproj...");
            try
            {
                EnsureFoldersInCsproj(projectFilePath, RequiredFolders);
                Console.WriteLine("Folders verified and updated in .csproj.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: Failed to update .csproj with folder items: {ex.Message}");
                return;
            }

            Console.WriteLine("Step 2: Ensuring required packages...");
            try
            {
                EnsureRequiredPackages(projectFilePath, errors);
                Console.WriteLine("Package verification completed.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: Failed to verify or add required packages: {ex.Message}");
            }

            Console.WriteLine("Step 3: Ensuring required directories exist...");
            foreach (var folder in RequiredFolders)
            {
                var fullPath = Path.Combine(projectDir, folder.TrimEnd('\\', '/'));
                if (!Directory.Exists(fullPath))
                {
                    Directory.CreateDirectory(fullPath);
                    Console.WriteLine($"Created missing directory: {fullPath}");
                }
                else
                {
                    Console.WriteLine($"Directory already exists: {fullPath}");
                }
            }

            Console.WriteLine("Step 4: Verifying or creating required files...");
            try
            {
                Console.WriteLine("Checking/creating ReadOnlyDbContext.cs...");
                CreateReadOnlyDbContextIfMissing(projectDir, nameSpace);
                Console.WriteLine("ReadOnlyDbContext.cs verification completed.");

                Console.WriteLine($"Checking/creating {dbContext}.cs...");
                CreateDbContextIfMissing(projectDir, nameSpace, dbContext);
                Console.WriteLine($"{dbContext}.cs verification completed.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: Failed to create DbContext files: {ex.Message}");
            }

            if (errors.Count > 0)
            {
                Console.WriteLine("The following errors occurred while adding packages:");
                foreach (var error in errors)
                {
                    Console.WriteLine($" - {error}");
                }
                Console.WriteLine("Please resolve these issues manually.");
            }

            Console.WriteLine("Initial Setup Handler completed successfully!");
        }

        private void EnsureFoldersInCsproj(string projectFilePath, string[] folders)
        {
            Console.WriteLine("Parsing .csproj file...");
            var doc = XDocument.Load(projectFilePath);
            var projectElement = doc.Root;
            if (projectElement == null) throw new Exception("Invalid .csproj structure - missing root <Project> element.");

            Console.WriteLine("Checking for existing folder includes...");
            var folderIncludes = projectElement.Descendants("Folder")
                .Select(elem => elem.Attribute("Include")?.Value)
                .Where(val => !string.IsNullOrEmpty(val))
                .ToList();

            XElement itemGroup = projectElement.Elements("ItemGroup").FirstOrDefault();
            if (itemGroup == null)
            {
                Console.WriteLine("No <ItemGroup> found. Creating a new one...");
                itemGroup = new XElement("ItemGroup");
                projectElement.Add(itemGroup);
            }

            foreach (var folder in folders)
            {
                if (!folderIncludes.Contains(folder))
                {
                    Console.WriteLine($"Adding missing folder to .csproj: {folder}");
                    var folderElement = new XElement("Folder", new XAttribute("Include", folder));
                    itemGroup.Add(folderElement);
                }
                else
                {
                    Console.WriteLine($"Folder already exists in .csproj: {folder}");
                }
            }

            Console.WriteLine("Saving updated .csproj file...");
            doc.Save(projectFilePath);
            Console.WriteLine(".csproj file updated successfully.");
        }

        private void EnsureRequiredPackages(string projectFilePath, List<string> errors)
        {
            Console.WriteLine("Parsing .csproj file for package references...");
            var doc = XDocument.Load(projectFilePath);
            var projectElement = doc.Root;
            if (projectElement == null) throw new Exception("Invalid .csproj structure - missing root <Project> element.");

            var existingPackageReferences = projectElement
                .Descendants("PackageReference")
                .Select(pr => pr.Attribute("Include")?.Value)
                .Where(val => !string.IsNullOrEmpty(val))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var projectDirectory = Path.GetDirectoryName(projectFilePath);
            if (string.IsNullOrEmpty(projectDirectory))
            {
                throw new Exception($"Cannot determine directory for {projectFilePath}");
            }

            foreach (var package in RequiredPackages)
            {
                if (!existingPackageReferences.Contains(package))
                {
                    Console.WriteLine($"Package missing: {package}. Attempting to install...");
                    try
                    {
                        var result = RunShellCommand("dotnet", $"add package \"{package}\"", projectDirectory);
                        if (result.exitCode != 0)
                        {
                            Console.WriteLine($"Error installing package {package}: {result.output}");
                            errors.Add($"{package} - {result.output}");
                        }
                        else
                        {
                            Console.WriteLine($"Successfully installed package: {package}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to install package {package}: {ex.Message}");
                        errors.Add($"{package} - {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine($"Package already referenced: {package}");
                }
            }
        }

        private void CreateReadOnlyDbContextIfMissing(string projectDir, string nameSpace)
        {
            string readOnlyDbContextPath = Path.Combine(projectDir, "ReadOnlyDbContext.cs");
            Console.WriteLine($"Checking for ReadOnlyDbContext.cs at {readOnlyDbContextPath}...");
            if (!File.Exists(readOnlyDbContextPath))
            {
                Console.WriteLine("ReadOnlyDbContext.cs not found. Creating...");
                string content = $@"using Microsoft.EntityFrameworkCore;

namespace {nameSpace}
{{
    public partial class ReadOnlyDbContext : DbContext
    {{
        public ReadOnlyDbContext()
        {{
        }}

        public ReadOnlyDbContext(DbContextOptions<ReadOnlyDbContext> options)
            : base(options)
        {{
        }}

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseSqlServer("""");
    }}
}}
";
                File.WriteAllText(readOnlyDbContextPath, content);
                Console.WriteLine("ReadOnlyDbContext.cs created successfully.");
            }
            else
            {
                Console.WriteLine("ReadOnlyDbContext.cs already exists.");
            }
        }

        private void CreateDbContextIfMissing(string projectDir, string nameSpace, string dbContext)
        {
            string dbContextPath = Path.Combine(projectDir, $"{dbContext}.cs");
            Console.WriteLine($"Checking for {dbContext}.cs at {dbContextPath}...");
            if (!File.Exists(dbContextPath))
            {
                Console.WriteLine($"{dbContext}.cs not found. Creating...");
                string content = $@"using Microsoft.EntityFrameworkCore;

namespace {nameSpace}
{{
    public partial class {dbContext} : ReadOnlyDbContext
    {{
        public {dbContext}()
        {{
        }}

        public {dbContext}(DbContextOptions<ReadOnlyDbContext> options)
            : base(options) // Pass the correct type to the base class constructor
        {{
        }}

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseSqlServer(GetConnectionString());

        public string GetConnectionString()
        {{
            // Write your logic to return the connection string
            return null;
        }}
    }}
}}
";
                File.WriteAllText(dbContextPath, content);
                Console.WriteLine($"{dbContext}.cs created successfully.");
            }
            else
            {
                Console.WriteLine($"{dbContext}.cs already exists.");
            }
        }

        /// <summary>
        /// Utility: runs a shell command synchronously, returns stdout/stderr and exit code.
        /// </summary>
        private (int exitCode, string output) RunShellCommand(string command, string arguments, string workingDirectory)
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
