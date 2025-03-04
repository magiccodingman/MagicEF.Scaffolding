using MagicEf.Scaffold.Helpers;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MagicEf.Scaffold.CommandActions
{
    public class MigrationRunnerHandler : CommandHandlerBase
    {
        public override void Handle(string[] args)
        {
            Console.WriteLine("Starting MigrationRunnerHandler...");

            string? projectFilePath = ScaffoldFileHelper.NormalizePath(ArgumentHelper.GetArgumentValue(args, "--projectFilePath"));
            string? _namespace = ArgumentHelper.GetArgumentValue(args, "--namespace");

            if (string.IsNullOrEmpty(projectFilePath))
            {
                Console.WriteLine("Error: --projectFilePath is required.");
                return;
            }

            if (string.IsNullOrEmpty(_namespace))
            {
                Console.WriteLine("Error: --namespace is required.");
                return;
            }

            if (!Directory.Exists(projectFilePath))
            {
                Console.WriteLine($"Error: Specified project directory does not exist: {projectFilePath}");
                return;
            }

            Console.WriteLine($"Project directory located: {projectFilePath}");

            try
            {
                // Step 1: Compile and load the assembly
                Assembly assembly = CompileAndLoadAssembly(projectFilePath);

                // Step 2: Locate and set the cache directory
                SetScriptDirectoryCache(assembly, projectFilePath, _namespace);

                // Step 3: Invoke the MigrationRunner
                InvokeMigrationRunner(assembly, _namespace);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }

        private Assembly CompileAndLoadAssembly(string projectFilePath)
        {
            Console.WriteLine("Compiling and loading the assembly...");

            // Read all source files
            var sourceFiles = Directory.GetFiles(projectFilePath, "*.cs", SearchOption.AllDirectories);
            if (!sourceFiles.Any())
            {
                throw new FileNotFoundException("No source files (*.cs) found in the specified project directory.");
            }

            Console.WriteLine($"Found {sourceFiles.Length} source files. Preparing to compile...");

            var syntaxTrees = sourceFiles.Select(filePath => CSharpSyntaxTree.ParseText(File.ReadAllText(filePath))).ToArray();

            // Gather all necessary references
            var references = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
                .Select(a => MetadataReference.CreateFromFile(a.Location))
                .Cast<MetadataReference>()
                .ToList();

            // Include additional references from the output folder (bin)
            string binPath = Path.Combine(projectFilePath, "bin", "Debug", "net8.0"); // Adjust as necessary
            if (Directory.Exists(binPath))
            {
                foreach (var dll in Directory.GetFiles(binPath, "*.dll"))
                {
                    references.Add(MetadataReference.CreateFromFile(dll));
                }
            }

            // Add essential system references
            references.Add(MetadataReference.CreateFromFile(typeof(object).Assembly.Location)); // mscorlib
            references.Add(MetadataReference.CreateFromFile(typeof(Console).Assembly.Location)); // System.Console
            references.Add(MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location)); // System.Core

            // Compile the source files into an in-memory assembly
            var compilation = CSharpCompilation.Create("DynamicAssembly",
                syntaxTrees,
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            using var memoryStream = new MemoryStream();
            var result = compilation.Emit(memoryStream);

            if (!result.Success)
            {
                Console.WriteLine("Compilation failed. Errors:");
                foreach (var diagnostic in result.Diagnostics)
                {
                    Console.WriteLine(diagnostic.ToString());
                }
                throw new InvalidOperationException("Compilation failed.");
            }

            memoryStream.Seek(0, SeekOrigin.Begin);
            Console.WriteLine("Compilation successful. Loading assembly into memory...");

            return Assembly.Load(memoryStream.ToArray());
        }




        private void SetScriptDirectoryCache(Assembly assembly, string projectFilePath, string _namespace)
        {
            Console.WriteLine("Locating 'Scripts' directory and setting cache...");

            string? migrationsPath = Directory.GetDirectories(projectFilePath, "Migrations", SearchOption.AllDirectories)
                .FirstOrDefault();

            if (string.IsNullOrEmpty(migrationsPath))
            {
                throw new DirectoryNotFoundException("Migrations folder not found in the specified project directory.");
            }

            Console.WriteLine($"Found 'Migrations' folder at: {migrationsPath}");

            string scriptsPath = Path.Combine(migrationsPath, "Scripts");
            if (!Directory.Exists(scriptsPath))
            {
                throw new DirectoryNotFoundException("Scripts folder not found within the Migrations folder.");
            }

            Console.WriteLine($"Found 'Scripts' folder at: {scriptsPath}");

            string cacheClassFullName = $"{_namespace}.Migrations.MigrationCache";
            var cacheType = assembly.GetType(cacheClassFullName);

            if (cacheType == null)
            {
                throw new TypeLoadException($"Type '{cacheClassFullName}' not found in the dynamically loaded assembly.");
            }

            var scriptDirectoryProperty = cacheType.GetProperty("ScriptDirectoryPath", BindingFlags.Public | BindingFlags.Static);
            if (scriptDirectoryProperty == null)
            {
                throw new MissingMemberException($"Property 'ScriptDirectoryPath' not found in type '{cacheClassFullName}'.");
            }

            scriptDirectoryProperty.SetValue(null, scriptsPath);
            Console.WriteLine($"ScriptDirectoryPath set to: {scriptsPath}");
        }


        private void InvokeMigrationRunner(Assembly assembly, string _namespace)
        {
            Console.WriteLine("Invoking MigrationRunner...");

            string targetNamespace = $"{_namespace}.Migrations";
            string targetClassName = "MigrationRunner";
            string targetMethodName = "RunMigrations";

            string classFullName = $"{targetNamespace}.{targetClassName}";
            var runnerType = assembly.GetType(classFullName);

            if (runnerType == null)
            {
                throw new TypeLoadException($"Type '{classFullName}' not found in the dynamically loaded assembly.");
            }

            var runMigrationsMethod = runnerType.GetMethod(targetMethodName, BindingFlags.Public | BindingFlags.Static);
            if (runMigrationsMethod == null)
            {
                throw new MissingMemberException($"Method '{targetMethodName}' not found in type '{classFullName}'.");
            }

            Console.WriteLine("Invoking the method...");
            runMigrationsMethod.Invoke(null, null);
            Console.WriteLine("RunMigrations completed successfully.");
        }

    }
}
