using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MagicEf.Scaffold.Helpers
{
    public static class ProjectHelper
    {
        public static string? GetProjectNamespace(string projectFilePath)
        {
            if (string.IsNullOrEmpty(projectFilePath))
            {
                throw new ArgumentException("File path wasn't provided.");
            }

            if (!File.Exists(projectFilePath))
            {
                throw new ArgumentException($"File path doesn't exist: {projectFilePath}");
            }

            try
            {
                // Load the .csproj file as XML
                XDocument csproj = XDocument.Load(projectFilePath);
                XNamespace ns = csproj.Root?.Name.Namespace ?? throw new Exception("Could not determine XML namespace.");

                // Attempt to find the <AssemblyName> element
                var assemblyNameElement = csproj.Descendants(ns + "AssemblyName").FirstOrDefault();
                if (assemblyNameElement != null && !string.IsNullOrEmpty(assemblyNameElement.Value))
                {
                    return assemblyNameElement.Value.Trim();
                }

                // Attempt to find the <RootNamespace> element
                var rootNamespaceElement = csproj.Descendants(ns + "RootNamespace").FirstOrDefault();
                if (rootNamespaceElement != null && !string.IsNullOrEmpty(rootNamespaceElement.Value))
                {
                    return rootNamespaceElement.Value.Trim();
                }

                // Fallback: Use the file name without the ".csproj" extension as the namespace
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(projectFilePath);
                if (string.IsNullOrEmpty(fileNameWithoutExtension))
                {
                    throw new Exception("Failed to extract file name from project file path.");
                }

                // Return the full file name as the namespace
                return fileNameWithoutExtension;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error in GetProjectNamespace: {ex.Message}");
                return null;
            }
        }

    }
}
