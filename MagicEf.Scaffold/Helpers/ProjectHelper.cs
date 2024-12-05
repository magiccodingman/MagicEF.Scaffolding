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
            try
            {
                XDocument csproj = XDocument.Load(projectFilePath);
                XNamespace? ns = csproj.Root?.Name.Namespace;

                if (ns == null)
                    throw new Exception("Could not find namespace");

                // Check for AssemblyName
                var assemblyNameElement = csproj.Descendants(ns + "AssemblyName").FirstOrDefault();
                if (assemblyNameElement != null && !string.IsNullOrEmpty(assemblyNameElement.Value))
                {
                    return assemblyNameElement.Value;
                }

                // Check for RootNamespace
                var rootNamespaceElement = csproj.Descendants(ns + "RootNamespace").FirstOrDefault();
                if (rootNamespaceElement != null && !string.IsNullOrEmpty(rootNamespaceElement.Value))
                {
                    return rootNamespaceElement.Value;
                }

                // Fallback to project file name
                string fileNameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(projectFilePath);

                // Ensure the file name is treated as the full namespace (e.g., "DataAccess.Reporting")
                if (!string.IsNullOrEmpty(fileNameWithoutExtension))
                {
                    return fileNameWithoutExtension;
                }
            }
            catch
            {
                // Log exception or handle as needed
            }

            return null;
        }

    }
}
