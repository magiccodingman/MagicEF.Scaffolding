using MagicEf.Scaffold.Helpers;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MagicEf.Scaffold.CommandActions
{
    public class RemoveConstructorsHandler : CommandHandlerBase
    {
        public override void Handle(string[] args)
        {
            string filePath = ArgumentHelper.GetArgumentValue(args, "--filePath");

            if (string.IsNullOrEmpty(filePath))
            {
                Console.WriteLine("Error: --filePath is required.");
                return;
            }

            ProcessFile(filePath);
        }

        private void ProcessFile(string filePath)
        {
            var code = FileHelper.ReadFile(filePath);
            var root = RoslynHelper.ParseCode(code);

            // Get the class name directly from the syntax tree
            var classDeclaration = root.DescendantNodes()
                                       .OfType<ClassDeclarationSyntax>()
                                       .FirstOrDefault();

            if (classDeclaration == null)
            {
                Console.WriteLine("Error: Could not find a class in the file.");
                return;
            }

            var className = classDeclaration.Identifier.Text;

            if (string.IsNullOrEmpty(className))
            {
                Console.WriteLine("Error: Could not find class in the file.");
                return;
            }

            var rewriter = new ConstructorRemover(className);
            var newRoot = rewriter.Visit(root);

            if (rewriter.ChangesMade)
            {
                FileHelper.WriteFile(filePath, newRoot.ToFullString());
                Console.WriteLine($"Removed constructors from: {filePath}");
            }
            else
            {
                Console.WriteLine("No constructors found to remove.");
            }
        }
    }
}
