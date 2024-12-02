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
    public class ConstructorRemover : CSharpSyntaxRewriter
    {
        private readonly string className;
        public bool ChangesMade { get; private set; }

        public ConstructorRemover(string className)
        {
            this.className = className;
        }

        public override SyntaxNode? VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            if (node.Identifier.Text == className)
            {
                ChangesMade = true;
                Console.WriteLine($"Removed constructor: {node.Identifier.Text}");
                return null; // Remove the constructor
            }

            return base.VisitConstructorDeclaration(node);
        }
    }
}
