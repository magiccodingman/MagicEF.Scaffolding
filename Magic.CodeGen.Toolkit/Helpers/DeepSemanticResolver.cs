using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace Magic.CodeGen.Toolkit.Helpers
{
    public class DeepSemanticResolver
    {
        private readonly Compilation _compilation;

        public DeepSemanticResolver(Compilation compilation)
        {
            _compilation = compilation;
        }

        public object? ResolveExecutionPath<T>(
    ClassDeclarationSyntax classDeclaration, string methodName, params object[] parameters)
        {
            var semanticModel = _compilation.GetSemanticModel(classDeclaration.SyntaxTree);
            var classSymbol = semanticModel.GetDeclaredSymbol(classDeclaration) as INamedTypeSymbol;

            if (classSymbol == null)
            {
                Console.WriteLine($"[ERROR] Failed to resolve class symbol.");
                return null;
            }

            // **🟢 Attempt Tier 1: Static Code Analysis**
            var result = ResolveMethodExecution<T>(classSymbol, methodName);
            if (result != null)
                return result;

            // **🟠 If static analysis fails, ALWAYS try runtime execution**
            Console.WriteLine($"[INFO] Static analysis failed. Falling back to runtime execution for '{methodName}'.");

            return RuntimeExecutor.ExecuteMethod<T>(_compilation, classSymbol, methodName, parameters);
        }


        private object? ResolveMethodExecution<T>(INamedTypeSymbol classSymbol, string methodName)
        {
            var method = classSymbol.GetMembers()
                .OfType<IMethodSymbol>()
                .FirstOrDefault(m => m.Name == methodName && m.Parameters.Length == 0);

            if (method == null)
            {
                Console.WriteLine($"[INFO] Method '{methodName}' not found in static analysis. Trying runtime execution.");
                return null;
            }

            return TrackExecutionPath<T>(method, classSymbol);
        }

        private object? TrackExecutionPath<T>(IMethodSymbol method, INamedTypeSymbol classSymbol)
        {
            if (method.DeclaringSyntaxReferences.Length == 0)
            {
                Console.WriteLine($"[INFO] Could not analyze method '{method.Name}'. Checking if we can execute it in runtime.");

                // 🟢 Check if we have access to this method in runtime.
                Type? runtimeType = Type.GetType(method.ContainingType.ToString());
                if (runtimeType != null)
                {
                    Console.WriteLine($"[INFO] Found '{method.Name}' in runtime. Attempting execution dynamically.");
                    return RuntimeExecutor.ExecuteMethod<T>(_compilation, classSymbol, method.Name);
                }

                Console.WriteLine($"[ERROR] '{method.Name}' is inaccessible. Cannot execute dynamically.");
                return null;
            }

            var syntaxReference = method.DeclaringSyntaxReferences.FirstOrDefault();
            if (syntaxReference == null)
            {
                Console.WriteLine($"[ERROR] No syntax reference found for method '{method.Name}'.");
                return null;
            }

            var syntaxTree = syntaxReference.SyntaxTree;
            if (!_compilation.SyntaxTrees.Contains(syntaxTree))
            {
                Console.WriteLine($"[INFO] SyntaxTree for method '{method.Name}' is not part of the compilation. Skipping static analysis.");
                return null;
            }

            var semanticModel = _compilation.GetSemanticModel(syntaxTree);
            var methodNode = syntaxTree.GetRoot().DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .FirstOrDefault(m => m.Identifier.Text == method.Name);

            if (methodNode == null)
            {
                Console.WriteLine($"[ERROR] Could not locate method node for '{method.Name}'.");
                return null;
            }

            return ExtractExecutionPath<T>(methodNode, semanticModel, classSymbol);
        }


        private object? ExtractExecutionPath<T>(MethodDeclarationSyntax methodNode, 
            SemanticModel semanticModel, INamedTypeSymbol classSymbol)
        {
            if (methodNode.ExpressionBody != null)
            {
                return TrackExpression<T>(methodNode.ExpressionBody.Expression, semanticModel, classSymbol);
            }

            foreach (var returnStatement in methodNode.DescendantNodes().OfType<ReturnStatementSyntax>())
            {
                var extractedValue = TrackExpression<T>(returnStatement.Expression, semanticModel, classSymbol);
                if (extractedValue != null)
                    return extractedValue;
            }

            return null;
        }

        private object? TrackExpression<T>(ExpressionSyntax expression, SemanticModel semanticModel, INamedTypeSymbol classSymbol)
        {
            if (expression == null) return null;

            if (expression is LiteralExpressionSyntax literal)
            {
                return literal.Token.Value;
            }

            if (expression is InvocationExpressionSyntax invocation)
            {
                var methodSymbol = semanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
                if (methodSymbol != null)
                    return TrackExecutionPath<T>(methodSymbol, classSymbol);
            }

            return null;
        }
    }
}