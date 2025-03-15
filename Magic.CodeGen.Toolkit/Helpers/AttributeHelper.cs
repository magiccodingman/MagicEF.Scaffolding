using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Magic.CodeGen.Toolkit.Helpers
{
    public static class AttributeHelper
    {
        public static T? CreateAttributeInstance<T>(AttributeSyntax attributeSyntax) where T : Attribute
        {
            var attributeType = typeof(T);
            var constructors = attributeType.GetConstructors();

            // Extract arguments from the syntax node
            object[] extractedArguments = ExtractAttributeArguments(attributeSyntax, constructors);

            // Find a matching constructor
            var matchingConstructor = constructors.FirstOrDefault(c =>
            {
                var parameters = c.GetParameters();
                return parameters.Length == extractedArguments.Length &&
                       parameters.Select(p => p.ParameterType).SequenceEqual(extractedArguments.Select(a => a?.GetType()));
            });

            if (matchingConstructor == null)
                throw new InvalidOperationException($"No matching constructor found for {attributeType.Name} with given parameters.");

            // Instantiate the attribute with extracted parameters
            return (T?)matchingConstructor.Invoke(extractedArguments);
        }

        private static object[] ExtractAttributeArguments(AttributeSyntax attributeSyntax, ConstructorInfo[] constructors)
        {
            if (attributeSyntax.ArgumentList == null || attributeSyntax.ArgumentList.Arguments.Count == 0)
                return Array.Empty<object>();

            var args = attributeSyntax.ArgumentList.Arguments;
            var extractedValues = new object[args.Count];

            for (int i = 0; i < args.Count; i++)
            {
                var expression = args[i].Expression;
                if (expression is LiteralExpressionSyntax literal)
                {
                    extractedValues[i] = literal.Token.Value; // Extracts literal values (string, int, bool, etc.)
                }
                else if (expression is TypeOfExpressionSyntax typeOfExpr)
                {
                    extractedValues[i] = GetTypeFromTypeSyntax(typeOfExpr.Type);
                }
                else
                {
                    // Handle unknown cases
                    extractedValues[i] = null!;
                }
            }

            return extractedValues;
        }

        private static Type? GetTypeFromTypeSyntax(TypeSyntax typeSyntax)
        {
            return Type.GetType(typeSyntax.ToString()); // Convert TypeSyntax to actual Type (basic approach)
        }
    }
}
