using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MagicEf.Scaffold.Helpers
{
    public class InstallerHelper
    {
        public void EnsureFoldersInCsproj(string projectFilePath, string[] folders)
        {
            Console.WriteLine($"Parsing .csproj file: {projectFilePath}");
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

        public void EnsureRequiredPackages(string projectFilePath, string[] RequiredPackages, List<string> errors)
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
                        var result = new ShellHelper().RunShellCommand("dotnet", $"add package \"{package}\"", projectDirectory);
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
    }
}
