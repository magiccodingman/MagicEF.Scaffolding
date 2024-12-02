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
    public class OnConfiguringRemover : CSharpSyntaxRewriter
    {
        public bool ChangesMade { get; private set; }

        public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            if (node.Identifier.Text == "OnConfiguring" &&
                node.Modifiers.Any(SyntaxKind.OverrideKeyword) &&
                node.ParameterList.Parameters.Count == 1 &&
                node.ParameterList.Parameters[0].Type?.ToString() == "DbContextOptionsBuilder")
            {
                ChangesMade = true;
                Console.WriteLine($"Removed method: {node.Identifier.Text}");
                return null; // Remove the method
            }

            return base.VisitMethodDeclaration(node);
        }
    }
}
