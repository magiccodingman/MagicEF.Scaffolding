using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;

namespace Magic.CodeGen.Toolkit.Helpers
{
    public static class AnalyzerHelper
    {
        public static List<ClassDeclarationSyntax> GetPartialClassDeclarations(
        this Compilation compilation, ClassDeclarationSyntax classDeclaration)
        {
            // Extract class name
            var className = classDeclaration.Identifier.Text;

            // Extract namespace
            var namespaceName = GetNamespace(classDeclaration);

            // If it's not partial, just return this class declaration
            if (!classDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
            {
                return new List<ClassDeclarationSyntax> { classDeclaration };
            }

            // Get all syntax trees in the compilation
            var allTrees = compilation.SyntaxTrees;

            // Find matching partial class declarations
            var partialClasses = new HashSet<ClassDeclarationSyntax>();

            foreach (var tree in allTrees)
            {
                var root = tree.GetRoot();

                // Find all class declarations in this tree
                var classesInTree = root.DescendantNodes().OfType<ClassDeclarationSyntax>();

                foreach (var candidate in classesInTree)
                {
                    // Check if it has the same name and namespace
                    if (candidate.Identifier.Text == className && GetNamespace(candidate) == namespaceName)
                    {
                        partialClasses.Add(candidate);
                    }
                }
            }

            return partialClasses.ToList();
        }

        private static string GetNamespace(ClassDeclarationSyntax classDeclaration)
        {
            // Walk up the syntax tree to find the namespace declaration
            var parent = classDeclaration.Parent;
            while (parent != null)
            {
                if (parent is NamespaceDeclarationSyntax namespaceDeclaration)
                {
                    return namespaceDeclaration.Name.ToString();
                }
                parent = parent.Parent;
            }
            return string.Empty; // Global namespace case
        }
    }
}
