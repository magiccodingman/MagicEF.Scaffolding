using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MagicEf.Scaffold.Helpers
{
    public static class RoslynHelper
    {
        public static SyntaxNode ParseCode(string code)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(code);
            return syntaxTree.GetRoot();
        }

        public static string GetNamespace(SyntaxNode root)
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
