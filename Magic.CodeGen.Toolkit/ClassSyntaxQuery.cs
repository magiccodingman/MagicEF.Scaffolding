using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Magic.CodeGen.Toolkit
{
    internal class ClassSyntaxQuery
    {
        private readonly Compilation _compilation;
        private Func<ClassDeclarationSyntax, INamedTypeSymbol, bool> _filter = (_, __) => true; // Default pass-through filter

        public ClassSyntaxQuery(Compilation compilation)
        {
            _compilation = compilation;
        }

        /// <summary>
        /// Filters classes that have a specific attribute type.
        /// </summary>
        public ClassSyntaxQuery WithAttribute<TAttribute>() where TAttribute : Attribute
        {
            var attributeFullName = typeof(TAttribute).FullName!;
            _filter = CombineFilters(_filter, (classSyntax, classSymbol) =>
                classSymbol.GetAttributes().Any(attr => attr.AttributeClass?.ToDisplayString() == attributeFullName));

            return this;
        }

        /// <summary>
        /// Filters classes that implement a specific interface.
        /// </summary>
        public ClassSyntaxQuery WithInterface<TInterface>()
        {
            var interfaceFullName = typeof(TInterface).FullName!;
            _filter = CombineFilters(_filter, (classSyntax, classSymbol) =>
                classSymbol.Interfaces.Any(i => i.ToDisplayString() == interfaceFullName));

            return this;
        }

        /// <summary>
        /// Executes the query and returns matching class symbols and their syntax declarations.
        /// </summary>
        public List<(INamedTypeSymbol Symbol, ClassDeclarationSyntax Syntax)> ToList()
        {
            var results = new List<(INamedTypeSymbol, ClassDeclarationSyntax)>();

            foreach (var syntaxTree in _compilation.SyntaxTrees)
            {
                var semanticModel = _compilation.GetSemanticModel(syntaxTree);
                var root = syntaxTree.GetRoot();

                // Get all class declarations
                var classDeclarations = root.DescendantNodes().OfType<ClassDeclarationSyntax>();

                foreach (var classDecl in classDeclarations)
                {
                    var classSymbol = semanticModel.GetDeclaredSymbol(classDecl) as INamedTypeSymbol;
                    if (classSymbol == null)
                        continue;

                    // Apply the composed filters
                    if (_filter(classDecl, classSymbol))
                    {
                        results.Add((classSymbol, classDecl));
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Combines multiple filters into one.
        /// </summary>
        private static Func<ClassDeclarationSyntax, INamedTypeSymbol, bool> CombineFilters(
            Func<ClassDeclarationSyntax, INamedTypeSymbol, bool> existingFilter,
            Func<ClassDeclarationSyntax, INamedTypeSymbol, bool> newFilter)
        {
            return (classSyntax, classSymbol) => existingFilter(classSyntax, classSymbol) && newFilter(classSyntax, classSymbol);
        }
    }
}
