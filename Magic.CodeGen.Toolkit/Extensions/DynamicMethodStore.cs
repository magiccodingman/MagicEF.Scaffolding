using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Magic.CodeGen.Toolkit.Extensions
{
    public static class DynamicMethodStore
    {
        private static readonly Dictionary<string, MethodInfo> _genericMethods = new();

        /// <summary> Registers a generic method dynamically, allowing runtime type specification. </summary>
        public static void RegisterGenericMethod<T>(string key, string methodName)
        {
            MethodInfo method = typeof(T).GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
            if (method == null) throw new ArgumentException($"Method {methodName} not found in {typeof(T).Name}");

            _genericMethods[key] = method;
        }

        /// <summary> Invokes a registered generic method with a runtime-specified type. </summary>
        public static object InvokeGeneric(string key, Type runtimeType, params object[] args)
        {
            if (!_genericMethods.TryGetValue(key, out var methodInfo))
            {
                throw new InvalidOperationException($"Generic method '{key}' not found.");
            }

            // Make the method specific to the runtime type
            MethodInfo genericMethod = methodInfo.MakeGenericMethod(runtimeType);

            return genericMethod.Invoke(null, args);
        }
    }
}
