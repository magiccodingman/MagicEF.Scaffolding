using MagicEf.Scaffold.Helpers;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

namespace MagicEf.Scaffold.CommandActions
{
    public class ShareScaffoldProtocolHandler : CommandHandlerBase
    {
        private readonly string MagicReadOnlyName = "MagicReadOnly";
        public override void Handle(string[] args)
        {
            // Parse required arguments
            string? dbModelsPath = FileHelper.NormalizePath(ArgumentHelper.GetArgumentValue(args, "--dbModelsPath"));
            string? shareNamespace = ArgumentHelper.GetArgumentValue(args, "--shareNamespace");
            string? dbNamespace = ArgumentHelper.GetArgumentValue(args, "--dbNamespace");
            string? shareReadOnlyInterfacesPath = FileHelper.NormalizePath(ArgumentHelper.GetArgumentValue(args, "--shareReadOnlyInterfacesPath"));
            string? shareInterfaceExtensionsPath = FileHelper.NormalizePath(ArgumentHelper.GetArgumentValue(args, "--shareInterfaceExtensionsPath"));
            string? shareReadOnlyModelsPath = FileHelper.NormalizePath(ArgumentHelper.GetArgumentValue(args, "--shareReadOnlyModelsPath"));
            string? shareMetadataClassesPath = FileHelper.NormalizePath(ArgumentHelper.GetArgumentValue(args, "--shareMetadataClassesPath"));
            string? shareViewDtoModelsPath = FileHelper.NormalizePath(ArgumentHelper.GetArgumentValue(args, "--shareViewDtoModelsPath"));
            //string? dbMapperPath = FileHelper.NormalizePath(ArgumentHelper.GetArgumentValue(args, "--dbMapperPath"));
            string? shareSharedExtensionsPath = FileHelper.NormalizePath(ArgumentHelper.GetArgumentValue(args, "--shareSharedExtensionsPath"));
            string? shareSharedMetadataPath = FileHelper.NormalizePath(ArgumentHelper.GetArgumentValue(args, "--shareSharedMetadataPath"));


            string? dbExtensionsPath = null;
            string? dbextensiondirectory = ArgumentHelper.GetArgumentValue(args, "--dbExtensionsPath");

            if (!string.IsNullOrWhiteSpace(dbextensiondirectory))
            {
                dbExtensionsPath = FileHelper.NormalizePath(dbextensiondirectory);
            }

            string? dbMetadataPath = null;
            string? dbmetadatadirectory = ArgumentHelper.GetArgumentValue(args, "--dbMetadataPath");

            if (!string.IsNullOrWhiteSpace(dbmetadatadirectory))
            {
                dbMetadataPath = FileHelper.NormalizePath(dbmetadatadirectory);
            }

            // Optional


            // Basic validation
            if (string.IsNullOrEmpty(dbModelsPath) ||
                string.IsNullOrEmpty(shareNamespace) ||
                string.IsNullOrEmpty(dbNamespace) ||
                string.IsNullOrEmpty(shareReadOnlyInterfacesPath) ||
                string.IsNullOrEmpty(shareInterfaceExtensionsPath) ||
                string.IsNullOrEmpty(shareReadOnlyModelsPath) ||
                string.IsNullOrEmpty(shareMetadataClassesPath) ||
                string.IsNullOrEmpty(shareViewDtoModelsPath) ||
                string.IsNullOrEmpty(shareSharedExtensionsPath) ||
                string.IsNullOrEmpty(shareSharedMetadataPath)
                )
            {
                Console.WriteLine("Error: All arguments are required (except dbExtensionsPath which is optional).");
                return;
            }
            Console.WriteLine("Starting path checks");
            // Ensure the primary directories exist
            EnsureDirectoryExists(dbModelsPath);
            EnsureDirectoryExists(shareReadOnlyInterfacesPath);
            EnsureDirectoryExists(shareInterfaceExtensionsPath);
            EnsureDirectoryExists(shareReadOnlyModelsPath);
            EnsureDirectoryExists(shareMetadataClassesPath);
            EnsureDirectoryExists(shareViewDtoModelsPath);
            EnsureDirectoryExists(shareSharedExtensionsPath);
            EnsureDirectoryExists(shareSharedMetadataPath);

            if (!string.IsNullOrWhiteSpace(dbExtensionsPath))
            {
                Console.WriteLine($"Optional Extension parameter entered");
                EnsureDirectoryExists(dbExtensionsPath);
            }

            if (!string.IsNullOrWhiteSpace(dbMetadataPath))
            {
                Console.WriteLine($"Optional Extension parameter entered");
                EnsureDirectoryExists(dbMetadataPath);
            }

            Console.WriteLine("All paths exists");
            // Gather all .cs files in the database models path
            var modelFiles = Directory.GetFiles(dbModelsPath, "*.cs");

            // Filter out any “SeparatedVirtual.cs” if both the base and SeparatedVirtual exist
            var filteredFiles = modelFiles
                .GroupBy(file =>
                {
                    var fileName = Path.GetFileNameWithoutExtension(file);
                    return fileName.EndsWith("SeparatedVirtual")
                        ? fileName[..^"SeparatedVirtual".Length]
                        : fileName;
                })
                .SelectMany(group =>
                {
                    // If a group has both a base file and a "SeparatedVirtual" file, keep only the base
                    if (group.Count() > 1 && group.Any(f => !Path.GetFileNameWithoutExtension(f).EndsWith("SeparatedVirtual")))
                    {
                        return group.Where(f => !Path.GetFileNameWithoutExtension(f).EndsWith("SeparatedVirtual"));
                    }
                    return group;
                })
                .ToList();
            Console.WriteLine("Filtering successful");

            string projectDirectoryPath = GetNamespaceDirectory(shareReadOnlyInterfacesPath, shareNamespace);
            var magicReadOnlyPath = Path.Combine(projectDirectoryPath, MagicReadOnlyName);


            // no long utilizing created attributes. The use of Magic.Truth.Toolkit is now enforced.

            /* Console.WriteLine($"magic path: {magicReadOnlyPath}");
             return;*/
            //Directory.CreateDirectory(magicReadOnlyPath);

            /*CreateMagiViewDtoAttribute(shareNamespace, magicReadOnlyPath);
            CreateMagicFlattenRemoveAttribute(shareNamespace, magicReadOnlyPath);
            CreateMagicFlattenInterfaceRemoveAttribute(shareNamespace, magicReadOnlyPath);
            CreateMagicOrphanAttribute(shareNamespace, magicReadOnlyPath);*/

            // Process each filtered model file
            foreach (var modelFile in filteredFiles)
            {
                ProcessModelFile(
                    modelFile,
                    shareNamespace!,
                    dbNamespace!,
                    shareReadOnlyInterfacesPath!,
                    shareInterfaceExtensionsPath!,
                    shareReadOnlyModelsPath!,
                    shareMetadataClassesPath!,
                    shareViewDtoModelsPath!,
                    dbExtensionsPath,
                    shareSharedExtensionsPath!,
                    dbMetadataPath!,
                    shareSharedMetadataPath
                );
            }
        }

        private string GetNamespaceDirectory(string fullPath, string namespaceName)
        {
            if (string.IsNullOrWhiteSpace(fullPath))
                throw new ArgumentException("Full path cannot be null or empty.", nameof(fullPath));

            if (string.IsNullOrWhiteSpace(namespaceName))
                throw new ArgumentException("Namespace name cannot be null or empty.", nameof(namespaceName));

            var currentDirectory = new DirectoryInfo(fullPath);

            while (currentDirectory != null)
            {
                if (currentDirectory.Name.Equals(namespaceName, StringComparison.OrdinalIgnoreCase))
                {
                    return currentDirectory.FullName;
                }

                currentDirectory = currentDirectory.Parent;
            }

            throw new DirectoryNotFoundException($"The namespace '{namespaceName}' was expected to match a folder name, but was not found in the provided path '{fullPath}'.");
        }

        /// <summary>
        /// Generates an public interface that matches the original class minus any virtual/inverse properties.
        /// Always recreated from scratch.
        /// </summary>
        private void CreateMagiViewDtoAttribute(
    string shareNamespace,
    string magicReadOnlyPath)
        {
            var fileName = $"MagicMapAttributeReadOnly.cs";
            var filePath = Path.Combine(magicReadOnlyPath, fileName);
            // Define required usings for this file
            var predefinedUsings = new string[]
            {
                // Add any necessary usings here if required for all read-only interfaces
            };

            // Write the file while preserving any extra usings
            WriteFilePreservingExtraUsings(filePath, predefinedUsings, sb =>
            {
                sb.AppendLine($"namespace {shareNamespace}");
                sb.AppendLine("{");
                sb.AppendLine("    /// <summary>");
                sb.AppendLine("    /// Allows Magic EF to recognize this as a class desired to be part of the flattening share protocol ");
                sb.AppendLine("    /// which will auto create your flattened DTO without any auto mapping requirements.");
                sb.AppendLine("    /// </summary>");
                sb.AppendLine("    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]");
                sb.AppendLine("    public sealed class MagicMapAttribute : Attribute");
                sb.AppendLine("    {");
                sb.AppendLine($"        public string ProjectName {{ get => \"{shareNamespace}\"; }}");
                sb.AppendLine("        public string? CustomViewDtoName { get; }");
                sb.AppendLine("        public bool IgnoreWhenFlattening { get; }");
                sb.AppendLine("        public Type InterfaceType { get; }");
                sb.AppendLine();
                sb.AppendLine("        /// <summary>");
                sb.AppendLine("        /// ");
                sb.AppendLine("        /// </summary>");
                sb.AppendLine("        /// <param name=\"interfaceType\">The interface of the end desired connected model (aka the generated ReadOnly interfaces)</param>");
                sb.AppendLine("        /// <param name=\"ignoreWhenFlattening\">When true, the variable will not be added to the flattened model</param>");
                sb.AppendLine("        /// <exception cref=\"ArgumentException\"></exception>");
                sb.AppendLine($"        public MagicMapAttribute(Type interfaceType, bool ignoreWhenFlattening = false)");
                sb.AppendLine("        {");
                sb.AppendLine("            if (!interfaceType.IsInterface)");
                sb.AppendLine("                throw new ArgumentException($\"The type '{interfaceType.Name}' must be an interface.\", nameof(interfaceType));");
                sb.AppendLine();
                sb.AppendLine("            InterfaceType = interfaceType;");
                sb.AppendLine("            IgnoreWhenFlattening = ignoreWhenFlattening;");
                sb.AppendLine("        }");
                sb.AppendLine();
                sb.AppendLine("        /// <summary>");
                sb.AppendLine("        /// By default the flattened view DTO class name will match the class this attribute is attached to.");
                sb.AppendLine("        /// </summary>");
                sb.AppendLine("        /// <param name=\"interfaceType\">The interface of the end desired connected model (aka the generated ReadOnly interfaces)</param>");
                sb.AppendLine("        /// <param name=\"customViewDtoName\">The desired flattened view DTO class name</param>");
                sb.AppendLine("        /// <exception cref=\"ArgumentException\"></exception>");
                sb.AppendLine($"        public MagicMapAttribute(Type interfaceType, string customViewDtoName)");
                sb.AppendLine("        {");
                sb.AppendLine("            if (!interfaceType.IsInterface)");
                sb.AppendLine("                throw new ArgumentException($\"The type '{interfaceType.Name}' must be an interface.\", nameof(interfaceType));");
                sb.AppendLine();
                sb.AppendLine("            InterfaceType = interfaceType;");
                sb.AppendLine("            CustomViewDtoName = customViewDtoName;");
                sb.AppendLine("            IgnoreWhenFlattening = false;");
                sb.AppendLine("        }");
                sb.AppendLine("    }");
                sb.AppendLine("}");
            });

            Console.WriteLine($"Created/Refreshed read-only interface: {fileName}");
        }


        private void CreateMagicFlattenRemoveAttribute(
    string shareNamespace,
    string magicReadOnlyPath)
        {
            var fileName = $"MagicFlattenRemoveReadOnly.cs";
            var filePath = Path.Combine(magicReadOnlyPath, fileName);
            // Define required usings for this file
            var predefinedUsings = new string[]
            {
                // Add any necessary usings here if required for all read-only interfaces
            };

            // Write the file while preserving any extra usings
            WriteFilePreservingExtraUsings(filePath, predefinedUsings, sb =>
            {
                sb.AppendLine($"namespace {shareNamespace}");
                sb.AppendLine("{");
                sb.AppendLine("    /// <summary>");
                sb.AppendLine("    /// Removes this variable from being added to the auto generated flat view DTO and the generated interface.");
                sb.AppendLine("    /// </summary>");
                sb.AppendLine("    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]");
                sb.AppendLine("    public sealed class MagicFlattenRemoveAttribute : Attribute");
                sb.AppendLine("    {");
                // removed because this was too needlessly complex.
/*                sb.AppendLine("        public bool Orphan { get; }");
                sb.AppendLine("        public bool RemoveFromFlattenDto { get; }");
                sb.AppendLine();
                sb.AppendLine("        /// <summary>");
                sb.AppendLine("        /// ");
                sb.AppendLine("        /// </summary>");
                sb.AppendLine("        /// <param name=\"orphan\">Purposely orphan this variable, which removes the variable from validation testing</param>");
                sb.AppendLine("        public MagicFlattenRemoveAttribute(bool removeFromFlattenDto = true, bool orphan = false)");
                sb.AppendLine("        {");
                sb.AppendLine("            RemoveFromFlattenDto = removeFromFlattenDto;");
                sb.AppendLine("            Orphan = orphan;");
                sb.AppendLine("        }");*/
                sb.AppendLine("    }");
                sb.AppendLine("}");
            });

            Console.WriteLine($"Created/Refreshed read-only interface: {fileName}");
        }

        private void CreateMagicOrphanAttribute(
    string shareNamespace,
    string magicReadOnlyPath)
        {
            var fileName = $"MagicOrphanReadOnly.cs";
            var filePath = Path.Combine(magicReadOnlyPath, fileName);
            // Define required usings for this file
            var predefinedUsings = new string[]
            {
                // Add any necessary usings here if required for all read-only interfaces
            };

            // Write the file while preserving any extra usings
            WriteFilePreservingExtraUsings(filePath, predefinedUsings, sb =>
            {
                sb.AppendLine($"namespace {shareNamespace}");
                sb.AppendLine("{");
                sb.AppendLine("    /// <summary>");
                sb.AppendLine("    /// Purposely orphan this variable, which removes the variable from validation testing");
                sb.AppendLine("    /// </summary>");
                sb.AppendLine("    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]");
                sb.AppendLine("    public sealed class MagicOrphanAttribute : Attribute");
                sb.AppendLine("    {");
                sb.AppendLine("    }");
                sb.AppendLine("}");
            });

            Console.WriteLine($"Created/Refreshed read-only interface: {fileName}");
        }

        private void CreateMagicFlattenInterfaceRemoveAttribute(
    string shareNamespace,
    string magicReadOnlyPath)
        {
            var fileName = $"MagicFlattenInterfaceRemoveReadOnly.cs";
            var filePath = Path.Combine(magicReadOnlyPath, fileName);
            // Define required usings for this file
            var predefinedUsings = new string[]
            {
                // Add any necessary usings here if required for all read-only interfaces
            };

            // Write the file while preserving any extra usings
            WriteFilePreservingExtraUsings(filePath, predefinedUsings, sb =>
            {
                sb.AppendLine($"namespace {shareNamespace}");
                sb.AppendLine("{");
                sb.AppendLine("    /// <summary>");
                sb.AppendLine("    /// Removes this variable from being added to the flattened DTO interface that'll be created.");
                sb.AppendLine("    /// </summary>");
                sb.AppendLine("    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]");
                sb.AppendLine("    public sealed class MagicFlattenInterfaceRemoveAttribute : Attribute");
                sb.AppendLine("    {");
                sb.AppendLine("    }");
                sb.AppendLine("}");
            });

            Console.WriteLine($"Created/Refreshed read-only interface: {fileName}");
        }

        private void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                string msg = $"The following path doesn't exist: {path}";
                Console.WriteLine(msg);
                throw new DirectoryNotFoundException(msg);
            }
        }

        private void ProcessModelFile(
            string modelFilePath,
            string shareNamespace,
            string dbNamespace,
            string shareReadOnlyInterfacesPath,
            string shareInterfaceExtensionsPath,
            string shareReadOnlyModelsPath,
            string shareMetadataClassesPath,
            string shareViewDtoModelsPath,
            string? dbExtensionsPath,
            string shareSharedExtensionsPath,
            string dbMetadataPath,
            string shareSharedMetadataPath
            )
        {

            Console.WriteLine("Starting ShareScaffoldProtocolHandler main process");
            // 1) Parse the model file (e.g., AgencyRegion.cs) to identify the class name and properties
            var originalCode = FileHelper.ReadFile(modelFilePath);
            var syntaxTree = CSharpSyntaxTree.ParseText(originalCode);
            var root = syntaxTree.GetRoot();

            // Grab the first class declaration
            var classDeclaration = root
                .DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .FirstOrDefault();

            if (classDeclaration == null)
            {
                Console.WriteLine($"No class declaration found in file {modelFilePath}. Skipping...");
                return;
            }

            // Original model name (e.g., "AgencyRegion")
            var originalName = classDeclaration.Identifier.Text;

            // 2) Create or recreate the ReadOnly interface file: I{originalName}ReadOnly.cs
            CreateOrRefreshReadOnlyInterface(
                originalName,
                shareNamespace,
                shareReadOnlyInterfacesPath,
                classDeclaration
            );

            // 3) Create (if missing) the "interface extension" file: I{originalName}.cs
            //    This is the public interface that extends the read-only interface
            CreateInterfaceExtensionIfMissing(
                originalName,
                shareNamespace,
                shareInterfaceExtensionsPath
            );

            // 4) Create or skip the metadata class: {originalName}MetaData.cs (internal, never overwritten if exists)
            CreateMetaDataClassIfMissing(
                originalName,
                shareNamespace,
                shareMetadataClassesPath
            );

            /*CreateSharedMetaDataClassIfMissing(
                originalName,
                shareNamespace,
                shareSharedMetadataPath
                );*/

            // 5) Create or recreate the read-only model: {originalName}ReadOnly.cs (internal) from the original minus virtuals
            CreateOrRefreshReadOnlyModel(
                originalName,
                shareNamespace,
                shareReadOnlyModelsPath,
                classDeclaration
            );

            // 6) Create (if missing) the shared extension file: {originalName}SharedExtension.cs
            /*CreateSharedExtensionIfMissing(
                originalName,
                dbNamespace,
                shareSharedExtensionsPath
            );*/

            // 7) Create (if missing) the view DTO: {originalName}ViewDTO.cs
            //    which implements I{originalName}, is partial, has [Preserve], and [MetadataType(...)]
            CreateViewDtoIfMissing(
                originalName,
                shareNamespace,
                shareViewDtoModelsPath
            );

            // 8) Create (if missing) the mapper profile: {originalName}MapperProfile.cs
            // This has been outdated due to the new flatten protocol making Automapper no longer necessary
            /*CreateMapperProfileIfMissing(
                originalName,
                shareNamespace,
                dbNamespace,
                dbMapperPath
            );*/

            // 9) Optionally fix up the {originalName}Extension.cs in the dbExtensionsPath if provided
            if (!string.IsNullOrEmpty(dbExtensionsPath))
            {
                TryUpdateDbExtensionFile(
                    originalName,
                    shareNamespace,
                    dbExtensionsPath!
                );
            }

            if (!string.IsNullOrEmpty(dbExtensionsPath))
            {
                //shareSharedMetadataPath                
               /* TryUpdateDbMetadataFile(
                    originalName,
                    shareNamespace,
                    dbMetadataPath!
                );*/
            }


        }

        /// <summary>
        /// Writes a file at <paramref name="filePath"/> by combining a list of predefined using statements 
        /// with any additional usings found at the top of an existing file (if any), then appending the content 
        /// generated by <paramref name="buildContent"/>.
        /// </summary>
        /// <param name="filePath">Full path of the file to write.</param>
        /// <param name="predefinedUsings">The set of using statements you always want at the top (order matters).</param>
        /// <param name="buildContent">A callback that writes the remainder of the file into the provided StringBuilder.</param>
        private void WriteFilePreservingExtraUsings(
    string filePath,
    IEnumerable<string> predefinedUsings,
    Action<StringBuilder> buildContent)
        {
            var existingUsings = new HashSet<string>(StringComparer.Ordinal);

            // Read the existing file to extract using statements
            if (File.Exists(filePath))
            {
                var lines = File.ReadAllLines(filePath);
                foreach (var line in lines)
                {
                    var trimmed = line.Trim();
                    if (trimmed.StartsWith("using ") && trimmed.EndsWith(";"))
                    {
                        existingUsings.Add(trimmed);
                    }
                }
            }

            // Create the final ordered using list
            var orderedUsings = new List<string>();

            // Add predefined usings in the given order (ensuring no duplicates)
            foreach (var predefined in predefinedUsings)
            {
                if (!existingUsings.Contains(predefined))
                {
                    orderedUsings.Add(predefined);
                }
            }

            // Add extra usings that were found in the file but not predefined
            foreach (var extraUsing in existingUsings)
            {
                if (!orderedUsings.Contains(extraUsing))
                {
                    orderedUsings.Add(extraUsing);
                }
            }

            // Start writing the file content
            var sb = new StringBuilder();

            // Write the ordered using statements
            foreach (var u in orderedUsings)
            {
                sb.AppendLine(u);
            }

            // Insert a blank line after the usings (only if we actually wrote any usings)
            if (orderedUsings.Count > 0)
            {
                sb.AppendLine();
            }

            // Call the provided method to build the rest of the file
            buildContent(sb);

            // Write the final content to file
            File.WriteAllText(filePath, sb.ToString());
        }

        #region Step 2: Create/Recreate I{originalName}ReadOnly.cs

        /// <summary>
        /// Generates an public interface that matches the original class minus any virtual/inverse properties.
        /// Always recreated from scratch.
        /// </summary>
        private void CreateOrRefreshReadOnlyInterface(
    string originalName,
    string shareNamespace,
    string shareReadOnlyInterfacesPath,
    ClassDeclarationSyntax classDeclaration)
        {
            var interfaceName = $"I{originalName}ReadOnly";
            var fileName = $"{interfaceName}.cs";
            var filePath = Path.Combine(shareReadOnlyInterfacesPath, fileName);

            // Collect all non-virtual, non-inverse properties from the original class
            var properties = classDeclaration.Members
                .OfType<PropertyDeclarationSyntax>()
                .Where(prop => !IsVirtualOrInverse(prop))
                .ToList();

            // Define required usings for this file
            var predefinedUsings = new string[]
            {
                // Add any necessary usings here if required for all read-only interfaces
            };

            // Write the file while preserving any extra usings
            WriteFilePreservingExtraUsings(filePath, predefinedUsings, sb =>
            {
                sb.AppendLine($"namespace {shareNamespace}");
                sb.AppendLine("{");
                sb.AppendLine($"    public interface {interfaceName}");
                sb.AppendLine("    {");

                foreach (var prop in properties)
                {
                    var propType = prop.Type.ToString();
                    var propName = prop.Identifier.Text;
                    sb.AppendLine($"        {propType} {propName} {{ get; set; }}");
                }

                sb.AppendLine("    }");
                sb.AppendLine("}");
            });

            Console.WriteLine($"Created/Refreshed read-only interface: {filePath}");
        }


        /// <summary>
        /// Checks if a property is declared `virtual` or is likely an inverse navigation property.
        /// Heuristics: 
        ///   - Marked virtual
        ///   - Collection type (ICollection<T>)
        ///   - Possibly `[InverseProperty]`, `[ForeignKey]`
        /// </summary>
        private bool IsVirtualOrInverse(PropertyDeclarationSyntax prop)
        {
            // If property is declared virtual
            if (prop.Modifiers.Any(m => m.Text == "virtual"))
                return true;

            // If property type is a collection, e.g., ICollection<>, List<>, etc.
            var propType = prop.Type.ToString();
            if (propType.Contains("ICollection<") || propType.Contains("IEnumerable<") || propType.Contains("List<"))
                return true;

            // If it has [InverseProperty] or [ForeignKey] attributes
            var hasInverseProperty = prop.AttributeLists
                .SelectMany(a => a.Attributes)
                .Any(a => a.Name.ToString().Contains("InverseProperty") || a.Name.ToString().Contains("ForeignKey"));

            if (hasInverseProperty)
                return true;

            return false;
        }

        #endregion

        #region Step 3: Create I{originalName}.cs (the extension interface) if missing

        /// <summary>
        /// Creates a public interface "I{originalName}" that extends "I{originalName}ReadOnly".
        /// This file is never overwritten if it already exists (we skip).
        /// </summary>
        private void CreateInterfaceExtensionIfMissing(
             string originalName,
             string shareNamespace,
             string shareInterfaceExtensionsPath)
        {
            var extensionFileName = $"I{originalName}ShareExtension.cs";
            var extensionFilePath = Path.Combine(shareInterfaceExtensionsPath, extensionFileName);

            if (File.Exists(extensionFilePath))
            {
                Console.WriteLine($"Interface extension already exists: {extensionFilePath} -> Skipping creation");
                return;
            }

            // Build a partial interface "I{originalName}" that extends "I{originalName}ReadOnly"
            var interfaceName = $"I{originalName}";
            var readOnlyName = $"I{originalName}ReadOnly";

            var sb = new StringBuilder();
            sb.AppendLine($"namespace {shareNamespace}");
            sb.AppendLine("{");
            sb.AppendLine($"    // This interface extends the read-only interface and allows future expansions.");
            sb.AppendLine($"    public partial interface {interfaceName} : {readOnlyName}");
            sb.AppendLine("    {");
            sb.AppendLine("        // Add additional non-readonly properties or methods here if needed");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            File.WriteAllText(extensionFilePath, sb.ToString());
            Console.WriteLine($"Created interface extension: {extensionFilePath}");
        }


        #endregion

        #region Step 4: Create {originalName}MetaData.cs if missing

        private void CreateMetaDataClassIfMissing(
            string originalName,
            string shareNamespace,
            string shareMetadataClassesPath)
        {
            var metaDataFile = Path.Combine(shareMetadataClassesPath, $"{NameBuilder.ShareModelMetaDataExtensionName(originalName)}.cs");

            if (File.Exists(metaDataFile))
            {
                Console.WriteLine($"Metadata class already exists: {metaDataFile} -> Skipping creation");
                return;
            }

            // Build the internal metadata class
            var sb = new StringBuilder();
            sb.AppendLine($"namespace {shareNamespace}");
            sb.AppendLine("{");
            sb.AppendLine($"    internal class {NameBuilder.ShareModelMetaDataExtensionName(originalName)}");
            sb.AppendLine("    {");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            File.WriteAllText(metaDataFile, sb.ToString());
            Console.WriteLine($"Created MetaData class: {metaDataFile}");
        }

        #endregion



       

        #region Step 5: Create/Recreate {originalName}ReadOnly.cs

        private void CreateOrRefreshReadOnlyModel(
    string originalName,
    string shareNamespace,
    string shareReadOnlyModelsPath,
    ClassDeclarationSyntax classDeclaration)
        {
            var fileName = $"{originalName}ReadOnly.cs";
            var filePath = Path.Combine(shareReadOnlyModelsPath, fileName);

            // Collect all properties except those declared as virtual/inverse.
            var properties = classDeclaration.Members
                .OfType<PropertyDeclarationSyntax>()
                .Where(prop => !IsVirtualOrInverse(prop))
                .ToList();

            // Define the usings that you always want at the top for this file.
            var predefinedUsings = new string[]
            {
        "using System;",
        "using System.Diagnostics.CodeAnalysis; // For [ExcludeFromCodeCoverage] if needed",
        "using System.ComponentModel;"
            };

            // Build the file, preserving any extra using statements from an existing file.
            WriteFilePreservingExtraUsings(filePath, predefinedUsings, sb =>
            {
                sb.AppendLine($"namespace {shareNamespace}");
                sb.AppendLine("{");
                sb.AppendLine("    /// <summary>");
                sb.AppendLine("    /// This class is intended for internal use within the library.");
                sb.AppendLine($"    /// External projects should use <see cref=\"{originalName}ViewDTO\"/> instead.");
                sb.AppendLine("    /// </summary>");
                sb.AppendLine("    [EditorBrowsable(EditorBrowsableState.Never)]");
                sb.AppendLine($"    public partial class {originalName}ReadOnly : I{originalName}ReadOnly");
                sb.AppendLine("    {");

                // Add each non-virtual property.
                foreach (var prop in properties)
                {
                    var propType = prop.Type.ToString();
                    var propName = prop.Identifier.Text;
                    sb.AppendLine($"        public {propType} {propName} {{ get; set; }}");
                }

                sb.AppendLine("    }");
                sb.AppendLine("}");
            });

            Console.WriteLine($"Created read-only model: {filePath}");
        }


        #endregion

       

        #region Step 7: Create (if missing) {originalName}ViewDTO.cs

        private void CreateViewDtoIfMissing(
            string originalName,
            string shareNamespace,
            string shareViewDtoModelsPath)
        {
            var fileName = $"{originalName}ViewDto.cs";
            var filePath = Path.Combine(shareViewDtoModelsPath, fileName);

            if (File.Exists(filePath))
            {
                Console.WriteLine($"ViewDTO already exists: {filePath} -> Skipping creation");
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine("using System.ComponentModel.DataAnnotations;");
            sb.AppendLine("using Magic.Truth.Toolkit.Attributes;");
            sb.AppendLine();
            sb.AppendLine($"namespace {shareNamespace}");
            sb.AppendLine("{");
            //sb.AppendLine("    // Mark with [Preserve] to keep it from being stripped in AOT scenarios");
            //sb.AppendLine("    [Preserve]");
            sb.AppendLine($"    {CodeBuilder.MagicMapAttributeCode(originalName)}");
            sb.AppendLine($"    {CodeBuilder.ShareModelMetaDataExtensionCode(originalName)}");
            sb.AppendLine($"    public partial class {NameBuilder.ShareModel(originalName)} : {NameBuilder.ShareTruthInterface(originalName)}, {NameBuilder.ShareExtensionInterface(originalName)}");
            sb.AppendLine("    {");
            sb.AppendLine("        // Implement or override any properties beyond the read-only interface here if needed");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            File.WriteAllText(filePath, sb.ToString());
            Console.WriteLine($"Created ViewDTO class: {filePath}");
        }

        #endregion

        #region Step 8: Create (if missing) the mapper profile: {originalName}MapperProfile.cs


        #region Step 9 (Optional): Patch the dbExtensions file to include I{originalName}ReadOnly

        /// <summary>
        /// If dbExtensionsPath is provided, this checks the associated extension file
        /// for the presence of the I{originalName}ReadOnly interface and a using statement for shareNamespace.
        /// If missing, it injects them safely without overwriting user code.
        /// </summary>
        private void TryUpdateDbExtensionFile(
     string originalName,
     string shareNamespace,
     string dbExtensionsPath)
        {
            var expectedFileName = $"{originalName}Extension.cs";
            var extensionFilePath = Path.Combine(dbExtensionsPath, expectedFileName);

            if (!File.Exists(extensionFilePath))
            {
                Console.WriteLine($"No extension file found for {originalName} in {dbExtensionsPath}. Nothing to patch.");
                return;
            }

            // Read the existing file content
            var code = File.ReadAllText(extensionFilePath);

            // Split the file into lines for safe modifications
            var lines = code.Split(new[] { Environment.NewLine }, StringSplitOptions.None).ToList();

            // 1) Ensure the "using {shareNamespace};" is present and correctly positioned
            if (!lines.Any(line => line.Trim() == $"using {shareNamespace};"))
            {
                // Find the index after the last existing "using" statement
                var lastUsingIndex = lines.FindLastIndex(line => line.TrimStart().StartsWith("using "));
                if (lastUsingIndex >= 0)
                {
                    // Insert the new using statement right after the last "using" statement
                    lines.Insert(lastUsingIndex + 1, $"using {shareNamespace};");
                }
                else
                {
                    // If no "using" statements exist, add it at the very top
                    lines.Insert(0, $"using {shareNamespace};");
                }
            }

            // 2) Ensure the class declaration includes "I{originalName}ReadOnly" while preserving spacing
            var classDeclarationIndex = lines.FindIndex(line => line.Contains($"partial class {originalName}"));

            if (classDeclarationIndex >= 0)
            {
                var classDeclarationLine = lines[classDeclarationIndex];
                var originalIndentation = classDeclarationLine.Substring(0, classDeclarationLine.IndexOf("public")).Replace("\t", "    ");

                // Check if the interface is already connected
                if (!classDeclarationLine.Contains(NameBuilder.ShareTruthInterface(originalName)))
                {
                    if (classDeclarationLine.Contains(":"))
                    {
                        // If the class already inherits/implements something, append the interface
                        classDeclarationLine = classDeclarationLine.Insert(
                            classDeclarationLine.LastIndexOf(":") + 1,
                            $" {NameBuilder.ShareTruthInterface(originalName)},"
                        );
                    }
                    else
                    {
                        // Otherwise, add the colon and interface
                        classDeclarationLine = classDeclarationLine.Replace(
                            $"partial class {originalName}",
                            $"partial class {originalName} : {NameBuilder.ShareTruthInterface(originalName)}"
                        );
                    }

                    // Restore original spacing
                    lines[classDeclarationIndex] = originalIndentation + classDeclarationLine.Trim();
                }
            }

            // Rebuild the code with the updated lines
            code = string.Join(Environment.NewLine, lines);

            // Write the updated content back to the file
            File.WriteAllText(extensionFilePath, code);
            Console.WriteLine($"Safely updated extension file: {extensionFilePath}");
        }

        #endregion

        
        #endregion

    }
}
