using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MagicEf.Scaffold
{
    public class IndexRewriter : CSharpSyntaxRewriter
    {
        public bool ChangesMade { get; private set; }

        public override SyntaxNode VisitAttribute(AttributeSyntax node)
        {
            if (node.Name.ToString() == "Index" && !node.Name.ToString().StartsWith("Microsoft.EntityFrameworkCore."))
            {
                var newName = SyntaxFactory.ParseName("Microsoft.EntityFrameworkCore.Index");
                var newNode = node.WithName(newName);

                ChangesMade = true;
                Console.WriteLine($"Updated: {node}");
                return newNode;
            }

            return base.VisitAttribute(node);
        }
    }
}
