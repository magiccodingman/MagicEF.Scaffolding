using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MagicEf.Scaffold.Services;

namespace MagicEf.Scaffold.Helpers
{
    public static class RoslynHelper
    {
        public static void ModifyClassInterface(
       string classFullyQualifiedName,
       string interfaceName,
       //string? usingStatement,
       Compilation compilation,
       Dictionary<string, ClassDeclarationSyntax> classCache,
       bool remove = false)
        {
            if (string.IsNullOrWhiteSpace(classFullyQualifiedName)
                || string.IsNullOrWhiteSpace(interfaceName)
                //|| string.IsNullOrWhiteSpace(usingStatement)
                )
            {
                Console.WriteLine("Invalid input parameters. Skipping modification.");
                return;
            }

            var className = classFullyQualifiedName.Split('.').Last();
            if (!classCache.TryGetValue(className, out var targetClassDeclaration))
            {
                Console.WriteLine($"Class '{classFullyQualifiedName}' not found in the project.");
                return;
            }

            var syntaxTree = targetClassDeclaration.SyntaxTree;
            var root = syntaxTree.GetRoot();
            var documentPath = syntaxTree.FilePath;

            if (string.IsNullOrWhiteSpace(documentPath) || !File.Exists(documentPath))
            {
                Console.WriteLine($"Unable to locate file for class '{classFullyQualifiedName}'.");
                return;
            }

            // ✅ FIX: Correct extraction of interface names from BaseTypeSyntax
            var existingInterfaces = targetClassDeclaration.BaseList?.Types
                .Select(baseType => baseType.Type.ToString()) // Extract type names
                .ToList() ?? new List<string>();

            bool interfaceExists = existingInterfaces.Contains(interfaceName);

            if (remove)
            {
                if (!interfaceExists)
                {
                    Console.WriteLine($"Interface '{interfaceName}' is not implemented by '{classFullyQualifiedName}'. No changes made.");
                    return;
                }

                // ✅ FIX: Convert interfaces to `BaseTypeSyntax` properly
                var newBaseTypes = existingInterfaces
                    .Where(i => i != interfaceName) // Remove target interface
                    .Select(i => (BaseTypeSyntax)SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName(i))) // Ensure BaseTypeSyntax
                    .ToList();

                var newBaseListSyntax = newBaseTypes.Any()
                    ? SyntaxFactory.BaseList(SyntaxFactory.SeparatedList(newBaseTypes.Cast<BaseTypeSyntax>())) // ✅ Ensure correct type
                    : null;

                var modifiedClass = targetClassDeclaration.WithBaseList(newBaseListSyntax);
                var modifiedRoot = root.ReplaceNode(targetClassDeclaration, modifiedClass);
                SaveModifiedDocument(documentPath, modifiedRoot);
                Console.WriteLine($"Removed interface '{interfaceName}' from class '{classFullyQualifiedName}'.");
            }
            else
            {
                if (interfaceExists)
                {
                    Console.WriteLine($"Interface '{interfaceName}' is already implemented by '{classFullyQualifiedName}'. No changes made.");
                    return;
                }

                var newBaseTypes = existingInterfaces
                    .Append(interfaceName) // Add new interface
                    .Select(i => (BaseTypeSyntax)SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName(i))) // Ensure BaseTypeSyntax
                    .ToList();

                var newBaseListSyntax = SyntaxFactory.BaseList(SyntaxFactory.SeparatedList(newBaseTypes.Cast<BaseTypeSyntax>()));

                var modifiedClass = targetClassDeclaration.WithBaseList(newBaseListSyntax);
                var modifiedRoot = root.ReplaceNode(targetClassDeclaration, modifiedClass);

                /*// Ensure `using` statement is added if missing
                if (root is CompilationUnitSyntax compilationUnit)
                {
                    bool hasUsingDirective = compilationUnit.Usings
                        .Any(u => u.Name?.ToString() == usingStatement);

                    if (!hasUsingDirective)
                    {
                        var newUsingDirective = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(usingStatement));
                        modifiedRoot = compilationUnit.AddUsings(newUsingDirective);
                    }
                }*/

                SaveModifiedDocument(documentPath, modifiedRoot);
                Console.WriteLine($"Added interface '{interfaceName}' to class '{classFullyQualifiedName}'.");
            }
        }

        private static void SaveModifiedDocument(string documentPath, SyntaxNode modifiedRoot)
        {
            try
            {
                File.WriteAllText(documentPath, modifiedRoot.NormalizeWhitespace().ToFullString());
                Console.WriteLine($"Successfully updated file: {documentPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to update file: {documentPath}. Error: {ex.Message}");
            }
        }

        public static ClassDeclarationSyntax? FindClassDeclaration(this Dictionary<string, ClassDeclarationSyntax> classCache, string name)
        {
            classCache.TryGetValue(name, out var classDecl);
            return classDecl;
        }

        public static InterfaceDeclarationSyntax? FindInterfaceDeclaration(this Dictionary<string, InterfaceDeclarationSyntax> interfaceCache, string name)
        {
            interfaceCache.TryGetValue(name, out var interfaceDecl);
            return interfaceDecl;
        }

        public static TypeDeclarationSyntax? FindClassOrInterfaceDeclaration(this RoslynCompilationService roslynCompService, string name)
        {
            var classDecl = FindClassDeclaration(roslynCompService.GetClassCache(), name);
            if (classDecl != null) return classDecl;

            var interfaceDecl = FindInterfaceDeclaration(roslynCompService.GetInterfaceCache(), name);
            if (interfaceDecl != null) return interfaceDecl;

            return null;
        }

        public static SyntaxNode ParseCode(string code)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(code);
            return syntaxTree.GetRoot();
        }

        public static string? GetNamespace(SyntaxNode root)
        {
            // Handle file-scoped namespaces (e.g., "namespace DataAccess.Reporting;")
            var fileScopedNamespace = root.DescendantNodes()
                .OfType<FileScopedNamespaceDeclarationSyntax>()
                .FirstOrDefault();
            if (fileScopedNamespace != null)
            {
                return fileScopedNamespace.Name.ToString();
            }

            // Handle block-scoped namespaces (e.g., "namespace DataAccess.Reporting { ... }")
            var namespaceDeclaration = root.DescendantNodes()
                .OfType<NamespaceDeclarationSyntax>()
                .FirstOrDefault();
            return namespaceDeclaration?.Name.ToString();
        }


        public static List<(string Name, string Type)> GetKeyProperties(ClassDeclarationSyntax classDeclaration)
        {
            var keyProperties = new List<(string Name, string Type)>();

            var properties = classDeclaration.Members.OfType<PropertyDeclarationSyntax>();
            foreach (var prop in properties)
            {
                var hasKeyAttribute = prop.AttributeLists
                    .SelectMany(a => a.Attributes)
                    .Any(attr => attr.Name.ToString() == "Key");

                if (hasKeyAttribute)
                {
                    var propName = prop.Identifier.Text;
                    var propType = prop.Type.ToString();
                    keyProperties.Add((propName, propType));
                }
            }

            return keyProperties;
        }
    }
}
