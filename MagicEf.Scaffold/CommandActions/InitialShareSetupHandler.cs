using MagicEf.Scaffold.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace MagicEf.Scaffold.CommandActions
{
    public class InitialShareSetupHandler : CommandHandlerBase
    {
        private static readonly string[] RequiredFolders = new[]
    {
        "ReadOnlyInterfaces\\",
        "InterfaceExtensions\\",
        "ReadOnlyModels\\",
        "MetadataClasses\\",
        "ViewDtoModels\\",
        "SharedExtensions\\",
        "SharedMetadata\\",
    };

        private static readonly string[] BackendRequiredFolders = new[]
    {
        "MappingProfiles\\",
    };

        private static readonly string[] RequiredPackages = new[]
        {
        "AutoMapper",
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
                Console.WriteLine("Error: No arguments provided. Please include --projectDirectoryPath");
                return;
            }

            string shareProjectFilePath = ScaffoldFileHelper.NormalizePath(ArgumentHelper.GetArgumentValue(args, "--shareProjectFilePath"));
            string shareProjectDirectoryPath = ScaffoldFileHelper.NormalizePath(Path.GetDirectoryName(shareProjectFilePath));
            string dbProjectFilePath = ScaffoldFileHelper.NormalizePath(ArgumentHelper.GetArgumentValue(args, "--dbProjectFilePath"));
            string dbProjectDirectoryPath = ScaffoldFileHelper.NormalizePath(Path.GetDirectoryName(dbProjectFilePath));


            Console.WriteLine("Validating arguments...");
            if (string.IsNullOrEmpty(shareProjectFilePath))
            {
                Console.WriteLine("Error: --shareProjectFilePath argument is missing or invalid.");
                return;
            }           

            if (!Path.Exists(shareProjectDirectoryPath))
            {
                Console.WriteLine($"Error: Unable to determine directory for project file: {shareProjectDirectoryPath}");
                return;
            }

            if (string.IsNullOrEmpty(dbProjectFilePath))
            {
                Console.WriteLine("Error: --dbProjectFilePath argument is missing or invalid.");
                return;
            }

            if (!Path.Exists(dbProjectDirectoryPath))
            {
                Console.WriteLine($"Error: Unable to determine directory for project file: {shareProjectDirectoryPath}");
                return;
            }


            Console.WriteLine($"Project directory: {shareProjectDirectoryPath}");

            
            Console.WriteLine("Ensuring required directories exist...");
            try
            {
                foreach (var folder in RequiredFolders)
                {
                    string path = Path.Combine(shareProjectDirectoryPath, folder);
                    

                    if (!Path.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                        Console.WriteLine($"Creating folder directory: {path}");
                    }
                    else
                    {
                        Console.WriteLine($"Directory already exists: {path}");
                    }
                }

                foreach (var folder in BackendRequiredFolders)
                {
                    string path = Path.Combine(dbProjectDirectoryPath, folder);


                    if (!Path.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                        Console.WriteLine($"Creating folder directory: {path}");
                    }
                    else
                    {
                        Console.WriteLine($"Directory already exists: {path}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: create folder: {ex.Message}");
                return;
            }

            Console.WriteLine("Ensuring folders in .csproj...");
            try
            {
                new InstallerHelper().EnsureFoldersInCsproj(shareProjectFilePath, RequiredFolders);
                new InstallerHelper().EnsureFoldersInCsproj(dbProjectFilePath, BackendRequiredFolders);
                Console.WriteLine("Folders verified and updated in .csproj.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: Failed to update .csproj with folder items: {ex.Message}");
                return;
            }

            var errors = new List<string>();
            Console.WriteLine("Ensuring required packages...");
            try
            {
                //new InstallerHelper().EnsureRequiredPackages(shareProjectDirectoryPath, errors);
                new InstallerHelper().EnsureRequiredPackages(dbProjectFilePath, RequiredPackages, errors);
                Console.WriteLine("Package verification completed.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: Failed to verify or add required packages: {ex.Message}");
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
    }
}