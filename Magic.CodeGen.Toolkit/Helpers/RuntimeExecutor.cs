using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Magic.CodeGen.Toolkit.Extensions;

namespace Magic.CodeGen.Toolkit.Helpers
{
    public class RuntimeExecutor
    {
        public static object? ExecuteMethod<T>(
            Compilation compilation, INamedTypeSymbol classSymbol, string methodName, params object[] parameters)
        {
            parameters ??= Array.Empty<object>();

            // **🔹 Step 1: Find the Type in Runtime**
            Type? runtimeType = Type.GetType(classSymbol.ToString());

            if (runtimeType == null)
            {
                Console.WriteLine($"[INFO] '{classSymbol.Name}' is not in runtime. Trying to reconstruct from compilation...");
                runtimeType = RebuildTypeFromCompilation(compilation, classSymbol);
            }

            if (runtimeType == null)
            {
                Console.WriteLine($"[ERROR] Could not reconstruct type '{classSymbol.Name}'. Execution failed.");
                return null;
            }

            // **🔹 Step 2: Create an Instance of the Class**
            object? instance = CreateInstance(runtimeType);
            if (instance == null)
            {
                Console.WriteLine($"[ERROR] Failed to create an instance of '{runtimeType.FullName}'.");
                return null;
            }

            // **🔹 Step 3: Find the Correct Method**
            MethodInfo? method = runtimeType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
            if (method == null)
            {
                Console.WriteLine($"[ERROR] Method '{methodName}' not found on type '{runtimeType.FullName}'.");
                return null;
            }

            // **🔹 Step 4: Handle Parameters (like `x => x.GUIY`)**
            object[] convertedParameters = ConvertExpressionParameters(method, parameters, runtimeType);

            // **🔹 Step 5: Invoke the Method**
            object? result = method.Invoke(instance, convertedParameters);
            return result;
        }

        private static Type? RebuildTypeFromCompilation(Compilation compilation, INamedTypeSymbol classSymbol)
        {
            var classDeclaration = classSymbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() as ClassDeclarationSyntax;
            if (classDeclaration == null)
            {
                Console.WriteLine($"[ERROR] Could not extract class declaration for '{classSymbol.Name}'.");
                return null;
            }

            TypeBuilder builder = new DynamicTypeBuilder().CreateType(classDeclaration);
            return builder?.CreateType();
        }

        private static object? CreateInstance(Type targetType)
        {
            try
            {
                object? instance = Activator.CreateInstance(targetType);
                return instance;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Could not instantiate '{targetType.FullName}': {ex.Message}");
                return null;
            }
        }

        private static object[] ConvertExpressionParameters(MethodInfo method, object[] parameters, Type targetType)
        {
            ParameterInfo[] methodParams = method.GetParameters();
            object[] finalParams = new object[methodParams.Length];

            for (int i = 0; i < methodParams.Length; i++)
            {
                if (parameters.Length > i)
                {
                    if (parameters[i] is LambdaExpression lambda)
                    {
                        var memberExpression = lambda.Body as MemberExpression;
                        if (memberExpression != null)
                        {
                            string propertyName = memberExpression.Member.Name;
                            PropertyInfo? property = targetType.GetProperty(propertyName);
                            finalParams[i] = property?.GetValue(Activator.CreateInstance(targetType));
                        }
                        else
                        {
                            finalParams[i] = parameters[i]; // Keep it as-is if not a known lambda
                        }
                    }
                    else
                    {
                        finalParams[i] = parameters[i];
                    }
                }
            }

            return finalParams;
        }
    }
}
