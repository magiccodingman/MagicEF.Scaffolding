using MagicEf.Scaffold.Helpers;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;


namespace MagicEf.Scaffold.CommandActions
{
    public class SeparateVirtualPropertiesHandler : CommandHandlerBase
    {
        public override void Handle(string[] args)
        {
            string? directoryPath = ArgumentHelper.GetArgumentValue(args, "--directoryPath");
            if (string.IsNullOrEmpty(directoryPath))
            {
                Console.WriteLine("Error: Provide --directoryPath.");
                return;
            }

            if (!Directory.Exists(directoryPath))
            {
                Console.WriteLine($"Error: Directory not found: {directoryPath}");
                return;
            }

            string? outputPath = ArgumentHelper.GetArgumentValue(args, "--outputPath");
            if (!string.IsNullOrEmpty(outputPath) && !Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
                Console.WriteLine($"Created output directory: {outputPath}");
                DeleteSeparatedVirtualFiles(outputPath);
            }

            // Delete files ending with "SeparatedVirtual.cs"
            DeleteSeparatedVirtualFiles(directoryPath);

            // Get all .cs files except those ending with "SeparatedVirtual.cs"
            var csFiles = ScaffoldFileHelper.GetCsFiles(directoryPath)
                .Where(f => !f.EndsWith("SeparatedVirtual.cs", StringComparison.OrdinalIgnoreCase));

            foreach (var csFile in csFiles)
            {
                ProcessFile(csFile, outputPath);
            }
        }

        private void DeleteSeparatedVirtualFiles(string directoryPath)
        {
            var separatedVirtualFiles = Directory.GetFiles(directoryPath, "*SeparatedVirtual.cs");
            foreach (var file in separatedVirtualFiles)
            {
                try
                {
                    if (File.Exists(file))
                    {
                        // Remove read-only attribute if necessary
                        var attributes = File.GetAttributes(file);
                        if ((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                        {
                            attributes &= ~FileAttributes.ReadOnly;
                            File.SetAttributes(file, attributes);
                        }
                        File.Delete(file);
                        Console.WriteLine($"Deleted file: {file}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error deleting file {file}: {ex.Message}");
                }
            }
        }

        private void ProcessFile(string filePath, string? outputPath)
        {
            var code = ScaffoldFileHelper.ReadFile(filePath);
            var root = RoslynHelper.ParseCode(code) as CompilationUnitSyntax;

            // Get usings
            var usings = root?.Usings;

            if (root == null)
                throw new Exception("Could not parse any code");

            // Get namespace
            var namespaceName = RoslynHelper.GetNamespace(root);

            var extractor = new VirtualPropertiesExtractor();
            var newRoot = (CompilationUnitSyntax)extractor.Visit(root);

            if (extractor.ChangesMade)
            {
                // Write modified code back to original file
                ScaffoldFileHelper.WriteFile(filePath, newRoot.NormalizeWhitespace().ToFullString());
                Console.WriteLine($"Updated file: {filePath}");

                // Now create the SeparatedVirtual file
                var originalFileName = Path.GetFileNameWithoutExtension(filePath);
                var directory = Path.GetDirectoryName(filePath);
                if (directory == null)
                    throw new Exception("Directory path cannot be null");

                var newFileName = originalFileName + "SeparatedVirtual.cs";
                var newFilePath = Path.Combine(directory, newFileName);
                if (!string.IsNullOrEmpty(outputPath))
                    newFilePath =Path.Combine(outputPath, newFileName);

                // Create the partial class with virtual properties
                var newRootVirtual = SyntaxFactory.CompilationUnit()
                        .WithUsings(usings ?? SyntaxFactory.List<UsingDirectiveSyntax>());

                if (!string.IsNullOrEmpty(namespaceName))
                {
                    var namespaceDeclaration = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName(namespaceName))
                        .NormalizeWhitespace();

                    var newClassDeclarationsVirtual = new List<MemberDeclarationSyntax>();

                    foreach (var kvp in extractor.ClassVirtualProperties)
                    {
                        var className = kvp.Key;
                        var virtualPropsForClass = kvp.Value;

                        var partialClassDeclaration = SyntaxFactory.ClassDeclaration(className)
                            .WithModifiers(SyntaxFactory.TokenList(new[]
                            {
                            SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                            SyntaxFactory.Token(SyntaxKind.PartialKeyword)
                            }))
                            .WithMembers(SyntaxFactory.List<MemberDeclarationSyntax>(virtualPropsForClass));

                        newClassDeclarationsVirtual.Add(partialClassDeclaration);
                    }

                    namespaceDeclaration = namespaceDeclaration.WithMembers(SyntaxFactory.List(newClassDeclarationsVirtual));
                    newRootVirtual = newRootVirtual.WithMembers(SyntaxFactory.SingletonList<MemberDeclarationSyntax>(namespaceDeclaration));
                }
                else
                {
                    var newClassDeclarationsVirtual = new List<MemberDeclarationSyntax>();

                    foreach (var kvp in extractor.ClassVirtualProperties)
                    {
                        var className = kvp.Key;
                        var virtualPropsForClass = kvp.Value;

                        var partialClassDeclaration = SyntaxFactory.ClassDeclaration(className)
                            .WithModifiers(SyntaxFactory.TokenList(new[]
                            {
                            SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                            SyntaxFactory.Token(SyntaxKind.PartialKeyword)
                            }))
                            .WithMembers(SyntaxFactory.List<MemberDeclarationSyntax>(virtualPropsForClass));

                        newClassDeclarationsVirtual.Add(partialClassDeclaration);
                    }

                    newRootVirtual = newRootVirtual.WithMembers(SyntaxFactory.List(newClassDeclarationsVirtual));
                }

                // Write the new file
                ScaffoldFileHelper.WriteFile(newFilePath, newRootVirtual.NormalizeWhitespace().ToFullString());
                Console.WriteLine($"Created file: {newFilePath}");
            }
            else
            {
                Console.WriteLine($"No virtual properties found in {filePath}");
            }
        }
    }
}
