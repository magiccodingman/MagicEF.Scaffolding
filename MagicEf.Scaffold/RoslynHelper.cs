using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MagicEf.Scaffold
{
    public static class RoslynHelper
    {
        public static SyntaxNode ParseCode(string code)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(code);
            return syntaxTree.GetRoot();
        }
    }
}
