﻿using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.MSBuild;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Build.Locator;

namespace MagicEf.Scaffold.CommandActions
{
    /// <summary>
    /// This command handler “flattens” the share project’s view DTO classes. It:
    /// 
    ///  - Expects three parameters:
    ///       --namespace
    ///       --shareProjectDirectory
    ///       --flattenViewDtoDirectoryPath
    /// 
    ///  - Searches the share project for any C# file that contains the [MagicViewDto] attribute.
    ///  - For each found file, it determines the target (flattened) class name (using the custom name provided via
    ///    the attribute if available), and then creates two files:
    ///      1. The flattened view DTO file (named e.g. ActionTermiteEmailStatisticReadOnly.cs)
    ///         with the provided namespace and implementing I{FlattenedName}.
    ///      2. The corresponding interface file (named I{FlattenedName}.cs) placed in a .Interfaces namespace (the provided namespace + ".Interfaces").
    ///  - In the flattening process, any properties decorated with [MagicFlattenRemove] are omitted from the flattened class,
    ///    and any property marked with [MagicFlattenInterfaceRemove] is omitted from the interface.
    ///  - Also, any attributes on the class or properties that are in the “ignore list” are skipped.
    /// 
    /// This implementation uses Roslyn to parse the original files.
    /// </summary>
    public class FlattenShareProtocolHandler : CommandHandlerBase
    {
        // List of attribute names that should NOT be copied to the flattened files.
        private static readonly string[] IgnoredAttributeNames = new[]
        {
            "MagicViewDto",
            "MagicFlattenRemove",
            "MagicFlattenInterfaceRemove",
        };

        public override void Handle(string[] args)
        {
            Console.WriteLine("Starting FlattenShareProtocolHandler...");

            // Read and validate required command arguments.
            string? targetNamespace = ArgumentHelper.GetArgumentValue(args, "--namespace");
            string shareProjectDirectory = FileHelper.NormalizePath(ArgumentHelper.GetArgumentValue(args, "--shareProjectDirectory"));
            string flattenDirectory = FileHelper.NormalizePath(ArgumentHelper.GetArgumentValue(args, "--flattenViewDtoDirectoryPath"));

            if (string.IsNullOrWhiteSpace(targetNamespace) ||
                string.IsNullOrWhiteSpace(shareProjectDirectory) ||
                string.IsNullOrWhiteSpace(flattenDirectory))
            {
                Console.WriteLine("Error: All arguments (--namespace, --shareProjectDirectory, --flattenViewDtoDirectoryPath) are required.");
                return;
            }

            if (!Directory.Exists(shareProjectDirectory))
            {
                Console.WriteLine($"Error: Share project directory does not exist: {shareProjectDirectory}");
                return;
            }

            InitializeRoslynCompilation(shareProjectDirectory);

            // Build the base output folder: FlattenedReadOnly.
            string flattenedBaseFolder = Path.Combine(flattenDirectory, "FlattenedReadOnly");

            try
            {
                // Clean or create the flattened base folder.
                if (Directory.Exists(flattenedBaseFolder))
                {
                    Console.WriteLine($"Cleaning existing folder: {flattenedBaseFolder}");
                    DeleteDirectoryContents(flattenedBaseFolder);
                }
                else
                {
                    Directory.CreateDirectory(flattenedBaseFolder);
                    Console.WriteLine($"Created folder: {flattenedBaseFolder}");
                }

                // Create subfolders: one for the flattened view DTO files and one for the interfaces.
                string flattenedViewDtoFolder = Path.Combine(flattenedBaseFolder, "ViewDTO");
                string flattenedInterfaceFolder = Path.Combine(flattenedBaseFolder, "ServicesViewDTO");
                Directory.CreateDirectory(flattenedViewDtoFolder);
                Directory.CreateDirectory(flattenedInterfaceFolder);
                Console.WriteLine("Created subfolders for ViewDTO and ServicesViewDTO.");

                // Find all .cs files in the share project directory recursively.
                var csFiles = Directory.GetFiles(shareProjectDirectory, "*.cs", SearchOption.AllDirectories);

                // Filter to only files that contain [MagicViewDto] text.
                var viewDtoFiles = csFiles.Where(f => File.ReadAllText(f).Contains("[MagicViewDto]")).ToList();

                Console.WriteLine($"Found {viewDtoFiles.Count} file(s) containing [MagicViewDto].");

                foreach (var file in viewDtoFiles)
                {
                    try
                    {
                        ProcessViewDtoFile(file, targetNamespace, flattenedViewDtoFolder, flattenedInterfaceFolder);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing file {file}: {ex.Message}");
                    }
                }

                Console.WriteLine("FlattenShareProtocolHandler completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fatal error: {ex.Message}");
            }
        }

        #region Helpers

        /// <summary>
        /// Deletes all files and subdirectories under the given directory (a true delete, not recycle).
        /// </summary>
        private void DeleteDirectoryContents(string directoryPath)
        {
            foreach (var file in Directory.GetFiles(directoryPath))
            {
                File.Delete(file);
            }
            foreach (var dir in Directory.GetDirectories(directoryPath))
            {
                Directory.Delete(dir, true);
            }
        }

        /// <summary>
        /// Processes a single view DTO file:
        ///  - Parses the file via Roslyn.
        ///  - Finds the first class declaration decorated with [MagicViewDto].
        ///  - Determines the “flattened” class name (using any CustomViewDtoName provided via the attribute).
        ///  - Generates two files: a flattened view DTO file and the corresponding interface file.
        /// </summary>
        private void ProcessViewDtoFile(string filePath, string targetNamespace, string flattenedViewDtoFolder, string flattenedInterfaceFolder)
        {
            Console.WriteLine($"Processing file: {filePath}");
            var originalCode = File.ReadAllText(filePath);
            var syntaxTree = CSharpSyntaxTree.ParseText(originalCode);
            var root = syntaxTree.GetRoot();

            // Extract all `using` statements from the ViewDTO class
            var usings = ExtractUsingStatements(root);

            var classDeclaration = root.DescendantNodes().OfType<ClassDeclarationSyntax>()
                .FirstOrDefault(cd => HasAttribute(cd.AttributeLists, "MagicViewDto"));

            if (classDeclaration == null)
            {
                Console.WriteLine($"No class with [MagicViewDto] found in {filePath}. Skipping...");
                return;
            }

            string originalClassName = classDeclaration.Identifier.Text;
            string flattenedName = originalClassName;

            var magicAttr = GetAttributeSyntax(classDeclaration.AttributeLists, "MagicViewDto");
            if (magicAttr != null)
            {
                var argExpr = magicAttr.ArgumentList?.Arguments.FirstOrDefault()?.Expression;
                if (argExpr is LiteralExpressionSyntax literal &&
                    !string.IsNullOrWhiteSpace(literal.Token.ValueText))
                {
                    flattenedName = literal.Token.ValueText;
                }
            }

            // Locate and load metadata class
            ClassDeclarationSyntax metadataClassDeclaration = GetMetadataClassDeclaration(classDeclaration);

            // Extract `using` statements from metadata class (if available)
            if (metadataClassDeclaration != null)
            {
                var metadataTree = metadataClassDeclaration.SyntaxTree;
                var metadataRoot = metadataTree.GetRoot();
                usings.UnionWith(ExtractUsingStatements(metadataRoot));
            }

            // Resolve conflicts AFTER merging all `using` statements**
            usings = RemoveConflictingUsings(root, usings);

            // Generate flattened files with merged metadata
            string flattenedClassContent = GenerateFlattenedClassContent(classDeclaration, metadataClassDeclaration, usings, targetNamespace, flattenedName);
            string flattenedClassFileName = Path.Combine(flattenedViewDtoFolder, $"{flattenedName}ReadOnly.cs");
            File.WriteAllText(flattenedClassFileName, flattenedClassContent);
            Console.WriteLine($"Created flattened view DTO: {flattenedClassFileName}");

            string flattenedInterfaceContent = GenerateFlattenedInterfaceContent(classDeclaration, metadataClassDeclaration, usings, targetNamespace, flattenedName);
            string flattenedInterfaceFileName = Path.Combine(flattenedInterfaceFolder, $"I{flattenedName}.cs");
            File.WriteAllText(flattenedInterfaceFileName, flattenedInterfaceContent);
            Console.WriteLine($"Created flattened interface: {flattenedInterfaceFileName}");
        }



        private ClassDeclarationSyntax GetMetadataClassDeclaration(ClassDeclarationSyntax classDeclaration)
        {
            var metadataAttr = GetAttributeSyntax(classDeclaration.AttributeLists, "MetadataType");

            if (metadataAttr != null)
            {
                var metadataTypeArg = metadataAttr.ArgumentList?.Arguments.FirstOrDefault()?.Expression;
                if (metadataTypeArg is TypeOfExpressionSyntax typeOfExpr)
                {
                    var metadataTypeName = typeOfExpr.Type.ToString().Split('.').Last();
                    return FindClassDeclaration(metadataTypeName);
                }
            }
            return null;
        }



        /// <summary>
        /// Generates the content of the flattened view DTO class.
        /// The class will be placed into the targetNamespace,
        /// will be named {flattenedName}, and will implement I{flattenedName}.
        /// Only properties not marked with [MagicFlattenRemove] are transferred.
        /// Attributes on the class and properties are copied except for those in the ignore list.
        /// </summary>
        private string GenerateFlattenedClassContent(ClassDeclarationSyntax originalClass,
    ClassDeclarationSyntax metadataClass, HashSet<string> usings, string targetNamespace, string flattenedName)
        {
            var sb = new StringBuilder();

            // Write unique `using` statements
            foreach (var u in usings)
                sb.AppendLine(u);

            sb.AppendLine($"using {targetNamespace}.Interfaces;");
            sb.AppendLine();
            sb.AppendLine($"namespace {targetNamespace}");
            sb.AppendLine("{");

            // **Extract and Keep Allowed Class-Level Attributes**
            var classAttributes = GetPreservedClassAttributes(originalClass);

            foreach (var attr in classAttributes)
            {
                sb.AppendLine($"    [{attr}]");
            }

            sb.AppendLine($"    public partial class {flattenedName} : I{flattenedName}");
            sb.AppendLine("    {");

            var properties = GetAllProperties(originalClass, metadataClass);

            foreach (var prop in properties)
            {
                if (HasAttribute(prop.AttributeLists, "MagicFlattenRemove"))
                    continue; // Skip properties marked with MagicFlattenRemove

                // Deduplicate attributes while maintaining order
                var mergedAttributes = GetMergedAttributes(prop, metadataClass).DistinctBy(attr => attr.ToString());

                foreach (var attr in mergedAttributes)
                {
                    sb.AppendLine($"        [{attr}]");
                }

                sb.AppendLine($"        public {prop.Type} {prop.Identifier.Text} {{ get; set; }}");
                sb.AppendLine();
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");
            return sb.ToString();
        }




        private IEnumerable<AttributeSyntax> GetPreservedClassAttributes(ClassDeclarationSyntax originalClass)
        {
            var ignoredAttributes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "MagicViewDto",
        "MetadataType"
    };

            return originalClass.AttributeLists
                .SelectMany(attrList => attrList.Attributes)
                .Where(attr =>
                {
                    string attrName = attr.Name.ToString();
                    return !ignoredAttributes.Contains(attrName) && !ignoredAttributes.Contains(attrName + "Attribute");
                });
        }








        private HashSet<string> GetRequiredUsings(List<PropertyDeclarationSyntax> properties, ClassDeclarationSyntax? metadataClass)
        {
            var requiredUsings = new HashSet<string>();

            void AddNamespace(ISymbol? symbol, string sourceHint)
            {
                if (symbol is INamedTypeSymbol namedTypeSymbol)
                {
                    string? namespaceName = namedTypeSymbol.ContainingNamespace?.ToString();
                    if (!string.IsNullOrWhiteSpace(namespaceName) && namespaceName != "System")
                    {
                        requiredUsings.Add(namespaceName);
                    }
                    else if (!IsBuiltInType(namedTypeSymbol.Name))
                    {
                        throw new Exception($"⚠️ Error: Could not determine namespace for type '{namedTypeSymbol.Name}' from {sourceHint}!");
                    }
                }
            }

            // Resolve namespaces for all property types
            foreach (var prop in properties)
            {
                var typeSymbol = FindTypeSymbol(prop.Type.ToString());
                if (typeSymbol != null) AddNamespace(typeSymbol, $"Property '{prop.Identifier.Text}'");

                // Resolve namespaces for attributes on properties
                foreach (var attrList in prop.AttributeLists)
                {
                    foreach (var attr in attrList.Attributes)
                    {
                        var attrSymbol = FindTypeSymbol(attr.Name.ToString());
                        if (attrSymbol != null) AddNamespace(attrSymbol, $"Attribute '{attr.Name}'");
                    }
                }
            }

            // Resolve namespaces for metadata class attributes
            if (metadataClass != null)
            {
                foreach (var metadataProp in metadataClass.Members.OfType<PropertyDeclarationSyntax>())
                {
                    foreach (var attr in metadataProp.AttributeLists.SelectMany(al => al.Attributes))
                    {
                        var attrSymbol = FindTypeSymbol(attr.Name.ToString());
                        if (attrSymbol != null) AddNamespace(attrSymbol, $"Metadata attribute '{attr.Name}'");
                    }
                }
            }

            return requiredUsings;
        }




        private static readonly HashSet<string> BuiltInTypes = new HashSet<string>
{
    "int", "long", "short", "byte", "bool", "double", "float", "decimal",
    "char", "string", "object", "void", "sbyte", "uint", "ulong", "ushort",
    "DateTime", "Guid", "TimeSpan"
};

        private INamedTypeSymbol? FindTypeSymbol(string typeName)
        {
            if (IsBuiltInType(typeName))
                return null; // Skip built-in types

            // First, try resolving via metadata name
            var typeSymbol = shareProjectCompilation.GetTypeByMetadataName(typeName);
            if (typeSymbol != null)
                return typeSymbol;

            // Next, attempt resolution in referenced assemblies
            foreach (var reference in shareProjectCompilation.References)
            {
                if (shareProjectCompilation.GetAssemblyOrModuleSymbol(reference) is IAssemblySymbol assemblySymbol)
                {
                    var foundType = assemblySymbol.GlobalNamespace
                        .GetNamespaceMembers()
                        .SelectMany(ns => ns.GetTypeMembers())
                        .FirstOrDefault(type => type.Name == typeName);

                    if (foundType != null)
                        return foundType;
                }
            }

            // Handle generic types (e.g., List<T>)
            if (typeName.Contains("<"))
            {
                var genericBaseName = typeName.Substring(0, typeName.IndexOf('<')).Trim();
                var resolvedGenericBase = FindTypeSymbol(genericBaseName);
                if (resolvedGenericBase != null)
                    return resolvedGenericBase;
            }

            Console.WriteLine($"🚨 CRITICAL ERROR: Type '{typeName}' could not be resolved! Please check project references.");
            return null; // This allows graceful fallback instead of throwing
        }

        private bool IsBuiltInType(string typeName)
        {
            return BuiltInTypes.Contains(typeName) || typeName.StartsWith("System.");
        }








        private PropertyDeclarationSyntax MergePropertyAttributes(PropertyDeclarationSyntax original, PropertyDeclarationSyntax metadata)
        {
            var originalAttributes = original.AttributeLists.SelectMany(a => a.Attributes).ToList();

            var metadataAttributes = metadata.AttributeLists
                .SelectMany(a => a.Attributes)
                .Where(attr => !originalAttributes.Any(orig => orig.Name.ToString() == attr.Name.ToString()))
                .Where(attr => !IsIgnoredAttribute(attr)) // Ensure ignored attributes aren't included
                .ToList();

            originalAttributes.AddRange(metadataAttributes);

            var attributeListSyntax = SyntaxFactory.AttributeList(SyntaxFactory.SeparatedList(originalAttributes));
            return original.WithAttributeLists(SyntaxFactory.SingletonList(attributeListSyntax));
        }





        class AttributeComparer : IEqualityComparer<AttributeSyntax>
        {
            public bool Equals(AttributeSyntax x, AttributeSyntax y)
                => x?.ToString() == y?.ToString();

            public int GetHashCode(AttributeSyntax obj)
                => obj.ToString().GetHashCode();
        }

        private HashSet<string> ExtractUsingStatements(SyntaxNode root)
        {
            var usingDirectives = new HashSet<string>();
            bool insideExcludedRegion = false;

            foreach (var usingDirective in root.DescendantNodes().OfType<UsingDirectiveSyntax>())
            {
                var leadingTrivia = usingDirective.GetLeadingTrivia();

                // Check for #if !MagicFlattenProtocol
                foreach (var trivia in leadingTrivia)
                {
                    if (trivia.Kind() == SyntaxKind.IfDirectiveTrivia)
                    {
                        var directive = (IfDirectiveTriviaSyntax)trivia.GetStructure();
                        if (directive.Condition.ToString().Contains("!MagicFlattenProtocol", StringComparison.OrdinalIgnoreCase))
                        {
                            insideExcludedRegion = true;
                        }
                    }
                    else if (trivia.Kind() == SyntaxKind.EndIfDirectiveTrivia)
                    {
                        insideExcludedRegion = false;
                    }
                }

                // Skip if inside a disabled region
                if (insideExcludedRegion)
                    continue;

                // Store full "using XYZ;" syntax instead of just the namespace
                usingDirectives.Add(usingDirective.ToFullString().Trim());
            }

            return RemoveConflictingUsings(root, usingDirectives);
        }

        /// <summary>
        /// Ensures no conflicting using statements exist based on the conflict mapping.
        /// </summary>
        private HashSet<string> RemoveConflictingUsings(SyntaxNode root, HashSet<string> usings)
        {
            // Define conflict mapping where keys override anything starting with values in the set.
            var conflictMap = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase)
    {
        { "Newtonsoft.Json", new HashSet<string> { "System.Text.Json" } }, // Remove all System.Text.Json.* if Newtonsoft.Json is present
        { "Microsoft.Data.SqlClient", new HashSet<string> { "System.Data.SqlClient" } } // Remove System.Data.SqlClient if Microsoft.Data.SqlClient is present
    };

            var usingsToRemove = new HashSet<string>();

            // Convert "using XYZ;" lines to their namespace names for easy comparison
            var namespaceSet = usings
                .Where(u => u.StartsWith("using "))
                .Select(u => u.Substring(6).TrimEnd(';'))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var (overrideNamespace, conflicts) in conflictMap)
            {
                if (namespaceSet.Any(n => n.Equals(overrideNamespace, StringComparison.OrdinalIgnoreCase)))
                {
                    foreach (var conflict in conflicts)
                    {
                        // Remove any `using ...` that starts with a conflicting namespace
                        foreach (var @using in usings)
                        {
                            if (@using.StartsWith($"using {conflict}", StringComparison.OrdinalIgnoreCase))
                            {
                                usingsToRemove.Add(@using);
                                Console.WriteLine($"Conflict detected: Removing \"{@using}\" because \"{overrideNamespace}\" is present.");
                            }
                        }
                    }
                }
            }

            // **NEW: Ensure attributes match the correct namespace**
            if (namespaceSet.Contains("Newtonsoft.Json"))
            {
                // If Newtonsoft.Json is present, make sure `[JsonIgnore]` is from Newtonsoft.Json
                var jsonIgnoreAttributes = root.DescendantNodes().OfType<AttributeSyntax>()
                    .Where(attr => attr.Name.ToString().Equals("JsonIgnore", StringComparison.OrdinalIgnoreCase));

                if (jsonIgnoreAttributes.Any())
                {
                    foreach (var @using in usings)
                    {
                        if (@using.StartsWith("using System.Text.Json.Serialization", StringComparison.OrdinalIgnoreCase))
                        {
                            usingsToRemove.Add(@using);
                            Console.WriteLine($"Conflict detected: Removing \"{@using}\" because Newtonsoft.Json is present and `[JsonIgnore]` is being used.");
                        }
                    }
                }
            }

            // Remove conflicting usings
            usings.ExceptWith(usingsToRemove);

            return usings;
        }


        private bool IsExcludedByPreprocessor(string usingText)
        {
            // Check if the using statement is wrapped in a preprocessor directive
            return usingText.Contains("#if !MagicFlattenProtocol") || usingText.Contains("#endif");
        }


        private List<PropertyDeclarationSyntax> GetAllProperties(TypeDeclarationSyntax typeDecl, ClassDeclarationSyntax? metadataClass = null)
        {
            var props = new List<PropertyDeclarationSyntax>();

            // Extract properties from the current type (whether it's a class or interface)
            if (typeDecl is ClassDeclarationSyntax classDecl)
            {
                props.AddRange(classDecl.Members.OfType<PropertyDeclarationSyntax>());
            }
            else if (typeDecl is InterfaceDeclarationSyntax interfaceDecl)
            {
                props.AddRange(interfaceDecl.Members.OfType<PropertyDeclarationSyntax>());
            }

            // Apply metadata attributes if available
            if (metadataClass != null)
            {
                foreach (var metadataProp in metadataClass.Members.OfType<PropertyDeclarationSyntax>())
                {
                    var targetProp = props.FirstOrDefault(p => p.Identifier.Text == metadataProp.Identifier.Text);
                    if (targetProp != null)
                    {
                        props.Remove(targetProp);
                        props.Add(MergePropertyAttributes(targetProp, metadataProp));
                    }
                }
            }

            // Recursively retrieve properties from base classes and interfaces
            var baseList = typeDecl.BaseList?.Types;
            if (baseList != null)
            {
                foreach (var baseTypeSyntax in baseList)
                {
                    var baseTypeName = baseTypeSyntax.Type.ToString();
                    var baseTypeDeclaration = FindClassOrInterfaceDeclaration(baseTypeName);
                    if (baseTypeDeclaration != null)
                        props.AddRange(GetAllProperties(baseTypeDeclaration));
                }
            }

            return props.GroupBy(p => p.Identifier.Text).Select(g => g.First()).ToList();
        }




        /// <summary>
        /// Generates the content of the flattened interface.
        /// The interface will be in the namespace: targetNamespace + ".Interfaces",
        /// named I{flattenedName}, and will contain all properties from the original class that
        /// are NOT marked with [MagicFlattenInterfaceRemove]. (Note: if a property is removed from the
        /// flattened class via [MagicFlattenRemove], it would naturally not appear here.)
        /// </summary>
        private string GenerateFlattenedInterfaceContent(ClassDeclarationSyntax originalClass,
    ClassDeclarationSyntax metadataClass, HashSet<string> usings, string targetNamespace, string flattenedName)
        {
            var sb = new StringBuilder();

            // Write all unique `using` statements
            foreach (var u in usings)
                sb.AppendLine(u);

            sb.AppendLine();
            string interfaceNamespace = targetNamespace + ".Interfaces";
            sb.AppendLine($"namespace {interfaceNamespace}");
            sb.AppendLine("{");

            sb.AppendLine($"    public interface I{flattenedName}");
            sb.AppendLine("    {");

            foreach (var prop in GetAllProperties(originalClass))
            {
                if (HasAttribute(prop.AttributeLists, "MagicFlattenRemove") ||
                    HasAttribute(prop.AttributeLists, "MagicFlattenInterfaceRemove"))
                    continue; // Skip properties not meant for the interface

                sb.AppendLine($"        {prop.Type} {prop.Identifier.Text} {{ get; set; }}");
                sb.AppendLine();
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");
            return sb.ToString();
        }





        private IEnumerable<AttributeSyntax> GetMergedAttributes(PropertyDeclarationSyntax originalProp, ClassDeclarationSyntax metadataClass)
        {
            var originalAttributes = originalProp.AttributeLists
                .SelectMany(al => al.Attributes)
                .Where(attr => !IsIgnoredAttribute(attr))
                .ToList();

            if (metadataClass != null)
            {
                var metadataProp = metadataClass.Members
                    .OfType<PropertyDeclarationSyntax>()
                    .FirstOrDefault(p => p.Identifier.Text == originalProp.Identifier.Text);

                if (metadataProp != null)
                {
                    var metadataAttributes = metadataProp.AttributeLists
                        .SelectMany(al => al.Attributes)
                        .Where(attr => !IsIgnoredAttribute(attr));

                    // Deduplicate attributes before adding them
                    foreach (var attr in metadataAttributes)
                    {
                        if (!originalAttributes.Any(existing => existing.ToString() == attr.ToString()))
                        {
                            originalAttributes.Add(attr);
                        }
                    }
                }
            }

            return originalAttributes;
        }



        /// <summary>
        /// Checks whether any attribute in the given lists matches the given attribute name.
        /// (It does a simple string check on the attribute’s name.)
        /// </summary>
        private bool HasAttribute(SyntaxList<AttributeListSyntax> attributeLists, string attributeName)
        {
            foreach (var attrList in attributeLists)
            {
                foreach (var attr in attrList.Attributes)
                {
                    string name = attr.Name.ToString();
                    // In C#, the attribute can be referenced with or without the "Attribute" suffix.
                    if (name.Equals(attributeName, StringComparison.OrdinalIgnoreCase) ||
                        name.Equals(attributeName + "Attribute", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Returns the first attribute syntax from the provided lists that matches the attributeName.
        /// </summary>
        private AttributeSyntax GetAttributeSyntax(SyntaxList<AttributeListSyntax> attributeLists, string attributeName)
        {
            foreach (var attrList in attributeLists)
            {
                foreach (var attr in attrList.Attributes)
                {
                    string name = attr.Name.ToString();
                    if (name.Equals(attributeName, StringComparison.OrdinalIgnoreCase) ||
                        name.Equals(attributeName + "Attribute", StringComparison.OrdinalIgnoreCase))
                    {
                        return attr;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Determines whether a given attribute should be ignored (not copied to the flattened file).
        /// </summary>
        private bool IsIgnoredAttribute(AttributeSyntax attr)
        {
            string name = attr.Name.ToString();
            string[] ignored = {
        "MagicViewDto",
        "MagicFlattenRemove",
        "MagicFlattenInterfaceRemove",
        "MetadataType" // Explicitly add back here for the class-level
    };

            return ignored.Any(i => name.StartsWith(i));
        }


        #endregion

        private Dictionary<string, ClassDeclarationSyntax> classCache = new();
        private Dictionary<string, InterfaceDeclarationSyntax> interfaceCache = new();
        private Compilation shareProjectCompilation;

        // Call this method at the start of your handler to initialize Roslyn Compilation:
        private void InitializeRoslynCompilation(string shareProjectDirectory)
        {
            try
            {
                var _ = typeof(Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions);


                // 🔹 Ensure MSBuild is properly registered
                if (!MSBuildLocator.IsRegistered)
                {
                    var instances = MSBuildLocator.QueryVisualStudioInstances().ToList();

                    // Choose the most recent version of MSBuild
                    var instance = instances.OrderByDescending(i => i.Version).FirstOrDefault();
                    if (instance == null)
                        throw new InvalidOperationException("No valid MSBuild instances found!");

                    MSBuildLocator.RegisterInstance(instance);
                    Console.WriteLine($"Registered MSBuild from: {instance.MSBuildPath}");
                }

                using var workspace = MSBuildWorkspace.Create();

                // 🔹 Find a valid `.csproj` file
                var csprojFiles = Directory.GetFiles(shareProjectDirectory, "*.csproj", SearchOption.TopDirectoryOnly);
                if (csprojFiles.Length == 0)
                {
                    throw new FileNotFoundException("No .csproj file found in the provided share project directory.");
                }

                string csprojFile = csprojFiles.FirstOrDefault(f => !f.Contains("Backup", StringComparison.OrdinalIgnoreCase))
                                    ?? csprojFiles.First();

                Console.WriteLine($"Opening project: {csprojFile}");

                var project = workspace.OpenProjectAsync(csprojFile).GetAwaiter().GetResult();
                if (project == null)
                {
                    throw new Exception($"Failed to open project: {csprojFile}");
                }

                shareProjectCompilation = project.GetCompilationAsync().GetAwaiter().GetResult();
                if (shareProjectCompilation == null)
                {
                    throw new Exception($"Failed to compile project: {csprojFile}");
                }

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
            foreach (var syntaxTree in shareProjectCompilation.SyntaxTrees)
            {
                var root = syntaxTree.GetRoot();

                var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>();
                foreach (var classDecl in classes)
                {
                    var semanticModel = shareProjectCompilation.GetSemanticModel(syntaxTree);
                    var symbol = semanticModel.GetDeclaredSymbol(classDecl);
                    if (symbol != null && !classCache.ContainsKey(symbol.Name))
                        classCache[symbol.Name] = classDecl;
                }

                var interfaces = root.DescendantNodes().OfType<InterfaceDeclarationSyntax>();
                foreach (var interfaceDecl in interfaces)
                {
                    var semanticModel = shareProjectCompilation.GetSemanticModel(syntaxTree);
                    var symbol = semanticModel.GetDeclaredSymbol(interfaceDecl);
                    if (symbol != null && !interfaceCache.ContainsKey(symbol.Name))
                        interfaceCache[symbol.Name] = interfaceDecl;
                }
            }
        }

        private ClassDeclarationSyntax FindClassDeclaration(string name)
        {
            classCache.TryGetValue(name, out var classDecl);
            return classDecl;
        }

        private InterfaceDeclarationSyntax FindInterfaceDeclaration(string name)
        {
            interfaceCache.TryGetValue(name, out var interfaceDecl);
            return interfaceDecl;
        }

        private TypeDeclarationSyntax FindClassOrInterfaceDeclaration(string name)
        {
            var classDecl = FindClassDeclaration(name);
            if (classDecl != null) return classDecl;

            var interfaceDecl = FindInterfaceDeclaration(name);
            if (interfaceDecl != null) return interfaceDecl;

            return null;
        }

    }
}
