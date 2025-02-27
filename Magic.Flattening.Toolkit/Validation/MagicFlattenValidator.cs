using Magic.Flattening.Toolkit.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Magic.Flattening.Toolkit.Validation
{
    public static class MagicFlattenValidator
    {
        private static readonly OpCode[] singleByteOpCodes = new OpCode[0x100];
        private static readonly OpCode[] multiByteOpCodes = new OpCode[0x100];

        static MagicFlattenValidator()
        {
            var fields = typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static);
            foreach (var field in fields)
            {
                if (field.GetValue(null) is OpCode opCode)
                {
                    ushort value = (ushort)opCode.Value;
                    if (value < 0x100)
                        singleByteOpCodes[value] = opCode;
                    else if ((value & 0xff00) == 0xfe00)
                        multiByteOpCodes[value & 0xff] = opCode;
                }
            }
        }

        public static List<(string className, string errorMessage)> ValidateFlattenMappings(string? specificProject = null)
        {
            var results = new Dictionary<string, string>(); // ✅ Use Dictionary to merge errors per class.

            var assembliesToScan = AppDomain.CurrentDomain.GetAssemblies()
                .Where(assembly => specificProject == null || assembly.GetName().Name!.Contains(specificProject, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var typesWithAttribute = assembliesToScan
                .SelectMany(assembly => SafeGetTypes(assembly))
                .Where(type => type.GetCustomAttribute<MagicViewDtoAttribute>() != null)
                .ToList();

            foreach (var type in typesWithAttribute)
            {
                var attribute = type.GetCustomAttribute<MagicViewDtoAttribute>();
                if (attribute == null) continue;

                if (attribute.IgnoreWhenFlattening)
                    continue;

                var interfaceType = attribute.InterfaceType;
                if (interfaceType == null)
                {
                    AddError(results, type.FullName!, $"{type.Name} has a null InterfaceType in MagicViewDto.");
                    continue;
                }

                var interfaceProperties = interfaceType.GetProperties();
                var classProperties = GetAllProperties(type);

                var missingProperties = interfaceProperties
                    .Where(p => !classProperties.Any(cp => cp.Name == p.Name && cp.PropertyType == p.PropertyType))
                    .ToList();

                if (missingProperties.Any())
                {
                    AddError(results, type.FullName!, $"Error: {type.FullName} is missing required properties: " +
                        string.Join(", ", missingProperties.Select(p => p.Name)));
                    continue;
                }

                foreach (var prop in classProperties)
                {
                    var flattenRemove = prop.GetCustomAttribute<MagicFlattenRemoveAttribute>();
                    if (flattenRemove != null)
                    {
                        var isValid = ValidateFlattenProperty(type, prop, interfaceType, out bool getFailed, out bool setFailed);

                        if (!isValid)
                        {
                            string errorDetails = getFailed && setFailed
                                ? "Getter and Setter failed validation."
                                : getFailed
                                    ? "Getter failed validation."
                                    : "Setter failed validation.";

                            AddError(results, type.FullName!,
                                $"{type.Name}.{prop.Name} has MagicFlattenRemove but does not map properly to a non-removed property. {errorDetails}");
                        }
                    }
                }
            }

            return results.Select(kv => (kv.Key, kv.Value)).ToList();
        }

        /// <summary>
        /// Adds an error message for a given class, ensuring no duplicates.
        /// If multiple errors exist, they are combined into a single entry.
        /// </summary>
        private static void AddError(Dictionary<string, string> results, string className, string errorMessage)
        {
            if (results.ContainsKey(className))
            {
                // ✅ Merge multiple errors into a single entry instead of duplicating.
                results[className] += Environment.NewLine + errorMessage;
            }
            else
            {
                results[className] = errorMessage;
            }
        }

        private static bool ValidateFlattenProperty(Type type, PropertyInfo property, Type interfaceType, out bool getFailed, out bool setFailed)
        {
            var getMethod = property.GetGetMethod();
            var setMethod = property.GetSetMethod();

            getFailed = false;
            setFailed = false;

            // ✅ Step 1: Ensure this property exists in the interface before validating
            bool existsInInterface = interfaceType.GetProperty(property.Name) != null;
            if (!existsInInterface)
                return true; // Skip validation if it's not in the provided interface!

            bool isPropertyOrphaned = property.GetCustomAttribute<MagicOrphanAttribute>() != null;

            bool validGetter = true;
            bool validSetter = true;

            // ✅ Step 2: Validate the Getter (Detect Muddied Values)
            if (!isPropertyOrphaned && getMethod != null)
            {
                bool isGetterOrphaned = getMethod.GetCustomAttribute<MagicOrphanAttribute>() != null;
                if (!isGetterOrphaned)
                {
                    HashSet<string> visitedMethods = new HashSet<string>();
                    validGetter = HasValidReferenceFromMethod(getMethod, type, visitedMethods, property.Name, out string muddiedError);

                    if (!validGetter)
                    {
                        getFailed = true;
                        if (!string.IsNullOrEmpty(muddiedError))
                            Console.WriteLine($"[ERROR] {type.FullName}.{property.Name} getter is muddied by {muddiedError}");
                    }
                }
            }

            // ✅ Step 3: Validate the Setter (Detect Muddied Values)
            if (!isPropertyOrphaned && setMethod != null)
            {
                bool isSetterOrphaned = setMethod.GetCustomAttribute<MagicOrphanAttribute>() != null;
                if (!isSetterOrphaned)
                {
                    HashSet<string> visitedMethods = new HashSet<string>();
                    validSetter = HasValidReferenceFromMethod(setMethod, type, visitedMethods, property.Name, out string muddiedError);

                    if (!validSetter)
                    {
                        setFailed = true;
                        if (!string.IsNullOrEmpty(muddiedError))
                            Console.WriteLine($"[ERROR] {type.FullName}.{property.Name} setter is muddied by {muddiedError}");
                    }
                }
            }

            return validGetter && validSetter;
        }


        private static bool HasValidReferenceFromMethod(MethodInfo method, Type type, HashSet<string> visitedMethods, string originalPropertyName, out string muddiedError)
        {
            muddiedError = string.Empty;

            if (visitedMethods.Contains(method.Name))
                return false; // Prevent infinite recursion

            visitedMethods.Add(method.Name);

            var allProperties = GetAllProperties(type);

            foreach (var prop in allProperties)
            {
                var flattenRemove = prop.GetCustomAttribute<MagicFlattenRemoveAttribute>();

                if (ILMethodCallsProperty(method, prop))
                {
                    if (flattenRemove == null)
                        return true; // ✅ Valid, found a direct reference to a non-removed property.

                    // ❌ Muddied Case: The method references a MagicFlattenRemove property
                    muddiedError = $"{prop.Name} (MagicFlattenRemove)";

                    // ✅ Recursively check the getter of this muddied property
                    var nestedGetter = prop.GetGetMethod();
                    if (nestedGetter != null)
                    {
                        HashSet<string> nestedVisitedMethods = new HashSet<string>();
                        bool isValid = HasValidReferenceFromMethod(nestedGetter, type, nestedVisitedMethods, prop.Name, out string deeperMuddiedError);
                        if (!isValid)
                        {
                            muddiedError = deeperMuddiedError; // Pass along the deeper error message
                            return false;
                        }
                    }
                }
            }

            // ✅ Check if the method calls other methods that might contain valid references
            foreach (var calledMethod in GetCalledMethods(method, type))
            {
                if (HasValidReferenceFromMethod(calledMethod, type, visitedMethods, originalPropertyName, out string deeperMuddiedError))
                    return true;
            }

            return false;
        }


        private static bool ILMethodCallsProperty(MethodInfo method, PropertyInfo property)
        {
            var methodBody = method.GetMethodBody();
            if (methodBody == null) return false;

            byte[] ilBytes = methodBody.GetILAsByteArray();
            if (ilBytes == null) return false;

            int position = 0;
            while (position < ilBytes.Length)
            {
                OpCode opcode;
                byte code = ilBytes[position++];
                if (code == 0xFE)
                {
                    byte second = ilBytes[position++];
                    opcode = multiByteOpCodes[second];
                }
                else
                {
                    opcode = singleByteOpCodes[code];
                }

                if (opcode == OpCodes.Call || opcode == OpCodes.Callvirt)
                {
                    int metadataToken = BitConverter.ToInt32(ilBytes, position);
                    position += 4;
                    try
                    {
                        MemberInfo? member = method.Module.ResolveMember(metadataToken);
                        if (member is MethodInfo mi)
                        {
                            if (mi == property.GetGetMethod() || mi == property.GetSetMethod())
                                return true;
                        }
                    }
                    catch { }
                }
            }
            return false;
        }

        private static List<MethodInfo> GetCalledMethods(MethodInfo method, Type type)
        {
            List<MethodInfo> calledMethods = new List<MethodInfo>();

            var ilBytes = method.GetMethodBody()?.GetILAsByteArray();
            if (ilBytes == null) return calledMethods;

            int position = 0;
            while (position < ilBytes.Length)
            {
                OpCode opcode;
                byte code = ilBytes[position++];
                if (code == 0xFE)
                {
                    byte second = ilBytes[position++];
                    opcode = multiByteOpCodes[second];
                }
                else
                {
                    opcode = singleByteOpCodes[code];
                }

                if (opcode == OpCodes.Call || opcode == OpCodes.Callvirt)
                {
                    int metadataToken = BitConverter.ToInt32(ilBytes, position);
                    position += 4;
                    try
                    {
                        MemberInfo? member = method.Module.ResolveMember(metadataToken);
                        if (member is MethodInfo mi && mi.DeclaringType == type)
                        {
                            calledMethods.Add(mi);
                        }
                    }
                    catch { }
                }
            }

            return calledMethods;
        }

        private static List<PropertyInfo> GetAllProperties(Type type)
        {
            var properties = new List<PropertyInfo>();
            while (type != null)
            {
                properties.AddRange(type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly));
                type = type.BaseType;
            }
            return properties;
        }

        private static IEnumerable<Type> SafeGetTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(t => t != null)!;
            }
        }
    }
}
