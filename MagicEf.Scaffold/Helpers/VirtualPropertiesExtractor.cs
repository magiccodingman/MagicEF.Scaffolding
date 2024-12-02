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
    public class VirtualPropertiesExtractor : CSharpSyntaxRewriter
    {
        public Dictionary<string, List<PropertyDeclarationSyntax>> ClassVirtualProperties { get; private set; }
        public bool ChangesMade { get; private set; }

        public VirtualPropertiesExtractor()
        {
            ClassVirtualProperties = new Dictionary<string, List<PropertyDeclarationSyntax>>();
        }

        public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            var className = node.Identifier.Text;
            var virtualProps = node.Members.OfType<PropertyDeclarationSyntax>()
                .Where(p => p.Modifiers.Any(SyntaxKind.VirtualKeyword))
                .ToList();

            if (virtualProps.Any())
            {
                ChangesMade = true;

                if (!ClassVirtualProperties.ContainsKey(className))
                {
                    ClassVirtualProperties[className] = new List<PropertyDeclarationSyntax>();
                }

                ClassVirtualProperties[className].AddRange(virtualProps);

                // Remove virtual properties from the class
                var newMembers = SyntaxFactory.List(node.Members.Where(m => !virtualProps.Contains(m)));

                // Adjust formatting to ensure only one newline between closing bracket and the member above
                newMembers = AdjustFormatting(newMembers);

                var newNode = node.WithMembers(newMembers);
                return base.VisitClassDeclaration(newNode);
            }

            return base.VisitClassDeclaration(node);
        }

        private SyntaxList<MemberDeclarationSyntax> AdjustFormatting(SyntaxList<MemberDeclarationSyntax> members)
        {
            if (members.Count > 0)
            {
                var lastMember = members.Last();

                // Remove extra blank lines at the end
                var trailingTrivia = lastMember.GetTrailingTrivia()
                    .Where(trivia => !trivia.IsKind(SyntaxKind.EndOfLineTrivia))
                    .ToSyntaxTriviaList();

                lastMember = lastMember.WithTrailingTrivia(trailingTrivia);
                members = members.Replace(members.Last(), lastMember);
            }

            return members;
        }
    }
}
