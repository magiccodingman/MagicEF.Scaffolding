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
        public static string GetProjectNamespace(string projectFilePath)
        {
            try
            {
                XDocument csproj = XDocument.Load(projectFilePath);
                XNamespace ns = csproj.Root.Name.Namespace;

                var assemblyNameElement = csproj.Descendants(ns + "AssemblyName").FirstOrDefault();
                if (assemblyNameElement != null && !string.IsNullOrEmpty(assemblyNameElement.Value))
                {
                    return assemblyNameElement.Value;
                }

                var rootNamespaceElement = csproj.Descendants(ns + "RootNamespace").FirstOrDefault();
                if (rootNamespaceElement != null && !string.IsNullOrEmpty(rootNamespaceElement.Value))
                {
                    return rootNamespaceElement.Value;
                }

                // Fallback to project file name
                return System.IO.Path.GetFileNameWithoutExtension(projectFilePath);
            }
            catch
            {
                return null;
            }
        }
    }
}
