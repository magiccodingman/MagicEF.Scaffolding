using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Magic.CodeGen.Toolkit.Helpers
{
    public class MagicValidatorRunner
    {
         public static bool TryValidateMagicTables(string projectDirectory)
    {
        string projectFile = Directory.GetFiles(projectDirectory, "*.csproj").FirstOrDefault();
        if (string.IsNullOrEmpty(projectFile))
        {
            Console.WriteLine($"ERROR: No .csproj file found in {projectDirectory}");
            return false;
        }

        Console.WriteLine($"Found project file: {projectFile}");

        // 🔹 Step 1: Build the full project (including dependencies)
        if (!BuildProject(projectFile))
        {
            Console.WriteLine("ERROR: Build failed.");
            return false;
        }

        // 🔹 Step 2: Locate Magic.IndexedDb.dll
        string indexedDbDll = FindMagicIndexedDbAssembly(projectFile);
        if (indexedDbDll == null || !File.Exists(indexedDbDll))
        {
            Console.WriteLine("ERROR: Magic.IndexedDb.dll not found.");
            return false;
        }

        Console.WriteLine($"Magic.IndexedDb.dll found: {indexedDbDll}");

        // 🔹 Step 3: Load and invoke ValidateTables()
        return TryInvokeMagicValidator(indexedDbDll);
    }

    private static bool BuildProject(string projectFile)
    {
        Console.WriteLine("Building the project...");

        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"build \"{projectFile}\" --configuration Debug",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using (Process process = Process.Start(psi))
        {
            process.WaitForExit();
            if (process.ExitCode != 0)
            {
                Console.WriteLine("ERROR: Build process failed.");
                Console.WriteLine(process.StandardError.ReadToEnd());
                return false;
            }
        }

        Console.WriteLine("Project built successfully.");
        return true;
    }

    private static string FindMagicIndexedDbAssembly(string projectFile)
    {
        string projectDirectory = Path.GetDirectoryName(projectFile);
        string outputDirectory = Path.Combine(projectDirectory, "bin", "Debug");

        // 🔍 Find Magic.IndexedDb.dll inside bin/Debug/
        string indexedDbDll = Directory.GetFiles(outputDirectory, "Magic.IndexedDb.dll", SearchOption.AllDirectories).FirstOrDefault();
        return indexedDbDll;
    }

    private static bool TryInvokeMagicValidator(string indexedDbDllPath)
    {
        try
        {
            // Load Magic.IndexedDb.dll dynamically
            Assembly indexedDbAssembly = Assembly.LoadFrom(indexedDbDllPath);

            // Find MagicValidator inside it
            Type validatorType = indexedDbAssembly.GetType("Magic.IndexedDb.Helpers.MagicValidator");

            if (validatorType == null)
            {
                Console.WriteLine("ERROR: MagicValidator class not found in Magic.IndexedDb.dll.");
                return false;
            }

            MethodInfo validateMethod = validatorType.GetMethod("ValidateTables", BindingFlags.NonPublic | BindingFlags.Static);
            if (validateMethod == null)
            {
                Console.WriteLine("ERROR: ValidateTables() method not found.");
                return false;
            }

            Console.WriteLine("Running ValidateTables()...");
            validateMethod.Invoke(null, null);

            Console.WriteLine("✅ Validation successful. Proceeding with scaffolding.");
            return true;
        }
        catch (TargetInvocationException ex)
        {
            Console.WriteLine($"❌ Validation failed: {ex.InnerException?.Message ?? ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Unexpected error while validating: {ex.Message}");
            return false;
        }
    }
    }
}
