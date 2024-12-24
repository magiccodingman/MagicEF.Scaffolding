using MagicEf.Scaffold.Helpers;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MagicEf.Scaffold.CommandActions
{
    public class ShareScaffoldProtocolHandler : CommandHandlerBase
    {
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
            string? dbMapperPath = FileHelper.NormalizePath(ArgumentHelper.GetArgumentValue(args, "--dbMapperPath"));
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
                string.IsNullOrEmpty(dbMapperPath) ||
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
            EnsureDirectoryExists(dbMapperPath);
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
                    dbMapperPath!,
                    dbExtensionsPath,
                    shareSharedExtensionsPath!,
                    dbMetadataPath!,
                    shareSharedMetadataPath
                );
            }
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
            string dbMapperPath,
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

            CreateSharedMetaDataClassIfMissing(
                originalName,
                shareNamespace,
                shareSharedMetadataPath
                );

            // 5) Create or recreate the read-only model: {originalName}ReadOnly.cs (internal) from the original minus virtuals
            CreateOrRefreshReadOnlyModel(
                originalName,
                shareNamespace,
                shareReadOnlyModelsPath,
                classDeclaration
            );

            // 6) Create (if missing) the shared extension file: {originalName}SharedExtension.cs
            CreateSharedExtensionIfMissing(
                originalName,
                dbNamespace,
                shareSharedExtensionsPath
            );

            // 7) Create (if missing) the view DTO: {originalName}ViewDTO.cs
            //    which implements I{originalName}, is partial, has [Preserve], and [MetadataType(...)]
            CreateViewDtoIfMissing(
                originalName,
                shareNamespace,
                shareViewDtoModelsPath
            );

            // 8) Create (if missing) the mapper profile: {originalName}MapperProfile.cs
            CreateMapperProfileIfMissing(
                originalName,
                shareNamespace,
                dbNamespace,
                dbMapperPath
            );

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
                TryUpdateDbMetadataFile(
                    originalName,
                    shareNamespace,
                    dbMetadataPath!
                );
            }

            
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

            // 1) Delete if it exists
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                Console.WriteLine($"Deleted existing file: {filePath}");
            }

            // 2) Collect all non-virtual, non-inverse properties from the original class
            var properties = classDeclaration.Members
                .OfType<PropertyDeclarationSyntax>()
                .Where(prop => !IsVirtualOrInverse(prop))
                .ToList();

            // 3) Build the interface code
            var sb = new StringBuilder();
            sb.AppendLine($"namespace {shareNamespace}");
            sb.AppendLine("{");
            sb.AppendLine($"    public interface {interfaceName}");
            sb.AppendLine("    {");

            foreach (var prop in properties)
            {
                // Example: `int Id { get; set; }`
                // We replicate type, name, and accessibility (always public in an interface).
                var propType = prop.Type.ToString();
                var propName = prop.Identifier.Text;
                sb.AppendLine($"        {propType} {propName} {{ get; set; }}");
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            // 4) Write to file
            File.WriteAllText(filePath, sb.ToString());
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
            var extensionFileName = $"I{originalName}Extension.cs";
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
            var metaDataName = $"{originalName}Metadata";
            var metaDataFile = Path.Combine(shareMetadataClassesPath, $"{metaDataName}.cs");

            if (File.Exists(metaDataFile))
            {
                Console.WriteLine($"Metadata class already exists: {metaDataFile} -> Skipping creation");
                return;
            }

            // Build the internal metadata class
            var sb = new StringBuilder();
            sb.AppendLine($"namespace {shareNamespace}");
            sb.AppendLine("{");
            sb.AppendLine($"    internal class {metaDataName}");
            sb.AppendLine("    {");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            File.WriteAllText(metaDataFile, sb.ToString());
            Console.WriteLine($"Created MetaData class: {metaDataFile}");
        }

        #endregion

        

        #region Create Shared {originalName}SharedMetaData.cs if missing

        private void CreateSharedMetaDataClassIfMissing(
            string originalName,
            string shareNamespace,
            string shareSharedMetadataPath)
        {
            var metaDataName = $"{originalName}SharedMetadata";
            var metaDataFile = Path.Combine(shareSharedMetadataPath, $"{metaDataName}.cs");

            if (File.Exists(metaDataFile))
            {
                Console.WriteLine($"Metadata class already exists: {metaDataFile} -> Skipping creation");
                return;
            }

            // Build the internal metadata class
            var sb = new StringBuilder();
            sb.AppendLine($"namespace {shareNamespace}");
            sb.AppendLine("{");
            sb.AppendLine($"    public class {metaDataName}");
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

            // Delete existing file if any
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                Console.WriteLine($"Deleted existing read-only model file: {filePath}");
            }

            // Collect all properties except those declared virtual/inverse
            var properties = classDeclaration.Members
                .OfType<PropertyDeclarationSyntax>()
                .Where(prop => !IsVirtualOrInverse(prop))
                .ToList();

            // Build a new read-only model that implements I{originalName}ReadOnly
            var sb = new StringBuilder();
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Diagnostics.CodeAnalysis; // For [ExcludeFromCodeCoverage] if needed");
            sb.AppendLine("using System.ComponentModel;");
            sb.AppendLine();
            sb.AppendLine($"namespace {shareNamespace}");
            sb.AppendLine("{");
            //sb.AppendLine("    // Ensure this is recognized by AOT/linker. Adjust if your environment requires a different attribute.");
            //sb.AppendLine("    [Preserve]");
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine($"    /// This class is intended for internal use within the library. ");
            sb.AppendLine($"    /// External projects should use <see cref=\"{originalName}ViewDTO\"/> instead.");
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine($"    [EditorBrowsable(EditorBrowsableState.Never)]");
            sb.AppendLine($"    public partial class {originalName}ReadOnly : I{originalName}ReadOnly");
            sb.AppendLine("    {");

            // Add each non-virtual property
            foreach (var prop in properties)
            {
                var propType = prop.Type.ToString();
                var propName = prop.Identifier.Text;
                sb.AppendLine($"        public {propType} {propName} {{ get; set; }}");
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            File.WriteAllText(filePath, sb.ToString());
            Console.WriteLine($"Created read-only model: {filePath}");
        }

        #endregion

        #region Step 6: Create (if missing) {originalName}SharedExtension.cs

        private void CreateSharedExtensionIfMissing(
            string originalName,
            string dbNamespace,
            string shareSharedExtensionsPath)
        {
            var fileName = $"{originalName}SharedExtension.cs";
            var filePath = Path.Combine(shareSharedExtensionsPath, fileName);

            if (File.Exists(filePath))
            {
                Console.WriteLine($"Shared extension already exists: {filePath} -> Skipping creation");
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine($"namespace {dbNamespace}");
            sb.AppendLine("{");
            sb.AppendLine($"    public static class {originalName}SharedExtension");
            sb.AppendLine("    {");
            sb.AppendLine($"        //public static [ReturnType] [YourMethod](this I{originalName}ReadOnly _interface)");
            sb.AppendLine("        //{");
            sb.AppendLine("        //    // Example usage of extension method on the read-only interface");
            sb.AppendLine("        //    // return something;");
            sb.AppendLine("        //}");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            File.WriteAllText(filePath, sb.ToString());
            Console.WriteLine($"Created shared extension: {filePath}");
        }

        #endregion

        #region Step 7: Create (if missing) {originalName}ViewDTO.cs

        private void CreateViewDtoIfMissing(
            string originalName,
            string shareNamespace,
            string shareViewDtoModelsPath)
        {
            var fileName = $"{originalName}ViewDTO.cs";
            var filePath = Path.Combine(shareViewDtoModelsPath, fileName);

            if (File.Exists(filePath))
            {
                Console.WriteLine($"ViewDTO already exists: {filePath} -> Skipping creation");
                return;
            }

            var metaDataName = $"{originalName}Metadata";
            var interfaceName = $"I{originalName}";

            var sb = new StringBuilder();
            sb.AppendLine("using System.ComponentModel.DataAnnotations;");
            sb.AppendLine();
            sb.AppendLine($"namespace {shareNamespace}");
            sb.AppendLine("{");
            //sb.AppendLine("    // Mark with [Preserve] to keep it from being stripped in AOT scenarios");
            //sb.AppendLine("    [Preserve]");
            sb.AppendLine($"    [MetadataType(typeof({metaDataName}))]");
            sb.AppendLine($"    public partial class {originalName}ViewDTO : {originalName}ReadOnly, {interfaceName}");
            sb.AppendLine("    {");
            sb.AppendLine("        // Implement or override any properties beyond the read-only interface here if needed");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            File.WriteAllText(filePath, sb.ToString());
            Console.WriteLine($"Created ViewDTO class: {filePath}");
        }

        #endregion

        #region Step 8: Create (if missing) the mapper profile: {originalName}MapperProfile.cs

        private void CreateMapperProfileIfMissing(
            string originalName,
            string shareNamespace,
            string dbNamespace,
            string dbMapperPath)
        {
            var fileName = $"{originalName}MapperProfile.cs";
            var filePath = Path.Combine(dbMapperPath, fileName);

            if (File.Exists(filePath))
            {
                Console.WriteLine($"Mapper profile already exists: {filePath} -> Skipping creation");
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine("using AutoMapper;");
            sb.AppendLine($"using {shareNamespace};");
            sb.AppendLine();
            sb.AppendLine($"namespace {dbNamespace}");
            sb.AppendLine("{");
            sb.AppendLine($"    public class {originalName}ToDtoProfile : Profile");
            sb.AppendLine("    {");
            sb.AppendLine($"        public {originalName}ToDtoProfile()");
            sb.AppendLine("        {");
            sb.AppendLine($"            // Use interface-first mapping by default for I{originalName}ReadOnly");
            sb.AppendLine($"            CreateMap<I{originalName}ReadOnly, I{originalName}ReadOnly>()");
            sb.AppendLine("                .IncludeAllDerived(); // Automates mapping for shared interface properties");
            sb.AppendLine();
            sb.AppendLine("            // Specific mapping for custom logic can be added here:");
            sb.AppendLine($"            //CreateMap<{originalName}ReadOnly, {originalName}ViewDTO>()");
            sb.AppendLine($"            //    .IncludeBase<I{originalName}ReadOnly, I{originalName}ReadOnly>()");
            sb.AppendLine("            //    .ForMember(dest => dest.YourField, opt => opt.MapFrom(src => \"Custom Value\"));");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine($"    public class DtoTo{originalName}Profile : Profile");
            sb.AppendLine("    {");
            sb.AppendLine($"        public DtoTo{originalName}Profile()");
            sb.AppendLine("        {");
            sb.AppendLine($"            // Use interface-first mapping by default for I{originalName}ReadOnly");
            sb.AppendLine($"            CreateMap<I{originalName}ReadOnly, I{originalName}ReadOnly>()");
            sb.AppendLine("                .IncludeAllDerived(); // Automates mapping for shared interface properties");
            sb.AppendLine();
            sb.AppendLine("            // Specific mapping for DTO -> model logic can be added here:");
            sb.AppendLine($"            //CreateMap<{originalName}ViewDTO, {originalName}ReadOnly>()");
            sb.AppendLine("            //    .IncludeBase<I{originalName}ReadOnly, I{originalName}ReadOnly>()");
            sb.AppendLine("            //    .ForMember(dest => dest.YourField, opt => opt.MapFrom(src => \"Something\"));");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            File.WriteAllText(filePath, sb.ToString());
            Console.WriteLine($"Created mapper profile: {filePath}");
        }

        #endregion

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
            var interfaceToConnect = $"I{originalName}ReadOnly";
            var classDeclarationIndex = lines.FindIndex(line => line.Contains($"partial class {originalName}"));

            if (classDeclarationIndex >= 0)
            {
                var classDeclarationLine = lines[classDeclarationIndex];
                var originalIndentation = classDeclarationLine.Substring(0, classDeclarationLine.IndexOf("public")).Replace("\t", "    ");

                // Check if the interface is already connected
                if (!classDeclarationLine.Contains(interfaceToConnect))
                {
                    if (classDeclarationLine.Contains(":"))
                    {
                        // If the class already inherits/implements something, append the interface
                        classDeclarationLine = classDeclarationLine.Insert(
                            classDeclarationLine.LastIndexOf(":") + 1,
                            $" {interfaceToConnect},"
                        );
                    }
                    else
                    {
                        // Otherwise, add the colon and interface
                        classDeclarationLine = classDeclarationLine.Replace(
                            $"partial class {originalName}",
                            $"partial class {originalName} : {interfaceToConnect}"
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

        #region Step 9 (Optional): Patch the dbExtensions file to include I{originalName}ReadOnly

        /// <summary>
        /// If dbExtensionsPath is provided, this checks the associated extension file
        /// for the presence of the I{originalName}ReadOnly interface and a using statement for shareNamespace.
        /// If missing, it injects them safely without overwriting user code.
        /// </summary>
        private void TryUpdateDbMetadataFile(
    string originalName,
    string shareNamespace,
    string dbMetadataPath)
        {
            var expectedFileName = $"{originalName}Metadata.cs";
            var metadataFilePath = Path.Combine(dbMetadataPath, expectedFileName);

            if (!File.Exists(metadataFilePath))
            {
                Console.WriteLine($"No metadata file found for {originalName} in {dbMetadataPath}. Nothing to patch.");
                return;
            }

            // Read the existing file content
            var code = File.ReadAllText(metadataFilePath);

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

            // 2) Ensure the class declaration includes ": {originalName}SharedMetadata" while preserving spacing
            var extensionToConnect = $"{originalName}SharedMetadata";
            var classDeclarationIndex = lines.FindIndex(line => line.Contains($"class {originalName}Metadata"));

            if (classDeclarationIndex >= 0)
            {
                var classDeclarationLine = lines[classDeclarationIndex];

                // Extract and preserve the original indentation
                var originalIndentation = classDeclarationLine.Substring(0, classDeclarationLine.IndexOf("public"));

                // Check if the class already extends/implements the desired metadata
                if (!classDeclarationLine.Contains(extensionToConnect))
                {
                    if (classDeclarationLine.Contains(":"))
                    {
                        // If the class already inherits/implements something, append the new extension
                        var colonIndex = classDeclarationLine.IndexOf(":");
                        classDeclarationLine = classDeclarationLine.Insert(
                            colonIndex + 1,
                            $" {extensionToConnect},"
                        );
                    }
                    else
                    {
                        // Otherwise, add the colon and the extension
                        classDeclarationLine = classDeclarationLine.Replace(
                            $"class {originalName}Metadata",
                            $"class {originalName}Metadata : {extensionToConnect}"
                        );
                    }
                }

                // Restore the updated line with the original indentation
                lines[classDeclarationIndex] = originalIndentation + classDeclarationLine.Trim();
            }

            // Rebuild the code with the updated lines
            code = string.Join(Environment.NewLine, lines);

            // Write the updated content back to the file
            File.WriteAllText(metadataFilePath, code);
            Console.WriteLine($"Safely updated metadata file: {metadataFilePath}");
        }








        #endregion

    }
}
