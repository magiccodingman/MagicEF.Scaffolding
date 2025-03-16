using Magic.CodeGen.Toolkit.Helpers;
using Magic.GeneralSystem.Toolkit;
using Magic.GeneralSystem.Toolkit.Helpers;
using Magic.GeneralSystem.Toolkit.Helpers.Dotnet;
using MagicEf.Scaffold.Models;
using MagicEf.Scaffold.Settings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Magic.IndexedDb;

namespace MagicEf.Scaffold.MagicCli.CommandRunners
{
    public class Concrete_IMagicCompoundKey : IMagicCompoundKey
    {
        public string[]? ColumnNamesInCompoundKey { get; set; }
    }

    public class Concrete_IMagicCompoundIndex : IMagicCompoundIndex
    {
        public string[]? ColumnNamesInCompoundIndex { get; set; }
    }

    public static class MagicIndexedDbRunner
    {
        public static async Task<bool> Run(PrimaryBaseProject primaryBaseProject)
        {
            string workingPath = primaryBaseProject.WorkingPath;
            var compilation = MsBuildHelper.GetProjectCompilation(workingPath);

            if (compilation == null)
            {
                Console.WriteLine("Compilation could not be generated.");
                return false;
            }

            // Get all class declarations that implement IMagicTable<TDbSets>
            List<ClassDeclarationSyntax>? classDeclarations = GetClassesImplementingMagicTable(compilation);

           // var success = MagicValidatorRunner.TryValidateMagicTables(workingPath);

            foreach (var classDeclaration in classDeclarations)
            {
                try
                {
                    var tableName = new DeepSemanticResolver(compilation).ResolveExecutionPath(classDeclaration, "GetTableName");
                    var compoundIndexes = new DeepSemanticResolver(compilation).ResolveExecutionPath(classDeclaration, "GetCompoundIndexes");
                    var compoundKey = new DeepSemanticResolver(compilation).ResolveExecutionPath(classDeclaration, "GetCompoundKey");
                }
                catch(Exception ex)
                {
                    return true;
                }

                var className = classDeclaration.Identifier.Text;

                var flattenedClass = FlattenedClassBuilder.BuildFlattenedClass(
                    compilation,
                    classDeclaration,
                    primaryBaseProject.NameSpace,
                    className, // Using the same class name
                    null,
                    null,
                    null,
                    null
                );

                Console.WriteLine($"Processed: {className}");
            }
            return true;
        }

        private static List<ClassDeclarationSyntax> GetClassesImplementingMagicTable(Compilation compilation)
        {
            var classes = new List<ClassDeclarationSyntax>();

            foreach (var tree in compilation.SyntaxTrees)
            {
                var root = tree.GetRoot();
                var semanticModel = compilation.GetSemanticModel(tree);

                // Find all class declarations
                var classDeclarations = root.DescendantNodes().OfType<ClassDeclarationSyntax>();

                foreach (var classDecl in classDeclarations)
                {
                    var classSymbol = semanticModel.GetDeclaredSymbol(classDecl) as INamedTypeSymbol;
                    if (classSymbol == null) continue; // Skip if not resolvable

                    // Check if class implements IMagicTable<TDbSets>
                    if (classSymbol.Interfaces.Any(i => i.OriginalDefinition.ToString() == "Magic.IndexedDb.IMagicTable<TDbSets>"))
                    {
                        classes.Add(classDecl);
                    }
                }
            }

            return classes;
        }
    }
}
