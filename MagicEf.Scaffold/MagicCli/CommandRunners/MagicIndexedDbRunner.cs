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
using Magic.GeneralSystem.Toolkit.Helpers.AssemblyHelper;
using Magic.IndexedDb.Helpers;
using Microsoft.VisualBasic;
using Magic.Cli.Toolkit;

namespace MagicEf.Scaffold.MagicCli.CommandRunners
{
    public static class MagicIndexedDbRunner
    {
        public static async Task<bool> Run(PrimaryBaseProject primaryBaseProject)
        {
            string workingPath = primaryBaseProject.WorkingPath;

            try
            {

                var loadAssemblies = await DotnetHelper.BuildAndLoadProject(workingPath);
                
                if (loadAssemblies != null && loadAssemblies.Success == true 
                    && loadAssemblies.Result != null && loadAssemblies.Result.Any())
                {
                    AssemblyLoader.EnsureAllAssembliesLoaded(loadAssemblies.Result);

                    Console.Clear();

                    var allTypes = GetAllMagicTablesDynamic();
                    MagicValidator.ValidateTables(allTypes);
                }

            }
            catch(Exception ex)
            {
                MagicConsole.WriteLine("ERROR:");
                MagicConsole.WriteLine();
                MagicConsole.WriteLine(ex.InnerException?.Message ?? ex.Message);
                MagicConsole.WriteLine();
                MagicConsole.Write("Program go back to the profile settings when you click any key...");
                Console.ReadKey();
                return true;
            }


            var compilation = MsBuildHelper.GetProjectCompilation(workingPath);

            if (compilation == null)
            {
                MagicConsole.WriteLine("Compilation could not be generated.");
                return false;
            }

            // Get all class declarations that implement IMagicTable<TDbSets>
            List<ClassDeclarationSyntax>? classDeclarations = GetClassesImplementingMagicTable(compilation);

           // var success = MagicValidatorRunner.TryValidateMagicTables(workingPath);


            foreach (var classDeclaration in classDeclarations)
            {

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

                MagicConsole.WriteLine($"Processed: {className}");
            }
            return true;
        }

        private static List<Type>? GetAllMagicTablesDynamic()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var validTypes = new List<Type>();

            foreach (var assembly in assemblies)
            {
                try
                {
                    var types = assembly.GetTypes();
                    validTypes.AddRange(types.Where(t => t.IsClass && !t.IsAbstract && SchemaHelper.ImplementsIMagicTable(t)));
                }
                catch (ReflectionTypeLoadException ex)
                {
                    Console.WriteLine($"[Skipped Assembly] {assembly.FullName} (Reflection failed: {ex.Message})");

                    // Handle partial success (some types may still be valid)
                    validTypes.AddRange(ex.Types?.Where(t => t != null && t.IsClass && !t.IsAbstract && SchemaHelper.ImplementsIMagicTable(t)) ?? new List<Type>());
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Critical Error] Failed to load types from {assembly.FullName}: {ex.Message}");
                }
            }

            return validTypes;
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
