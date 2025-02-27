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
        // IL OpCode lookup arrays for IL parsing
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
            var results = new List<(string className, string errorMessage)>();

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
                    results.Add((type.FullName!, $"{type.Name} has a null InterfaceType in MagicViewDto."));
                    continue;
                }

                var interfaceProperties = interfaceType.GetProperties();
                var classProperties = GetAllProperties(type);

                var missingProperties = interfaceProperties
                    .Where(p => !classProperties.Any(cp => cp.Name == p.Name && cp.PropertyType == p.PropertyType))
                    .ToList();

                if (missingProperties.Any())
                {
                    results.Add((type.FullName!, $"Error: {type.FullName} is missing required properties: " +
                        string.Join(", ", missingProperties.Select(p => p.Name))));
                    continue;
                }

                foreach (var prop in classProperties)
                {
                    var flattenRemove = prop.GetCustomAttribute<MagicFlattenRemoveAttribute>();
                    if (flattenRemove != null)
                    {
                        var isValid = ValidateFlattenProperty(type, prop, out bool getFailed, out bool setFailed);

                        if (!isValid)
                        {
                            string errorDetails = getFailed && setFailed
                                ? "Getter and Setter failed validation."
                                : getFailed
                                    ? "Getter failed validation."
                                    : "Setter failed validation.";

                            results.Add((type.FullName!,
                                $"{type.Name}.{prop.Name} has MagicFlattenRemove but does not map properly to a non-removed property. {errorDetails}"));
                        }
                    }
                }
            }

            return results;
        }

        private static bool ValidateFlattenProperty(Type type, PropertyInfo property, out bool getFailed, out bool setFailed)
        {
            var getMethod = property.GetGetMethod();
            var setMethod = property.GetSetMethod();

            getFailed = false;
            setFailed = false;

            bool isPropertyOrphaned = property.GetCustomAttribute<MagicOrphanAttribute>() != null;

            bool validGetter = true;
            bool validSetter = true;

            if (!isPropertyOrphaned && getMethod != null)
            {
                bool isGetterOrphaned = getMethod.GetCustomAttribute<MagicOrphanAttribute>() != null;
                if (!isGetterOrphaned)
                {
                    HashSet<string> visitedProperties = new HashSet<string>();
                    validGetter = HasValidReferenceFromMethod(getMethod, type, visitedProperties, property.Name);
                    if (!validGetter) getFailed = true;
                }
            }

            if (!isPropertyOrphaned && setMethod != null)
            {
                bool isSetterOrphaned = setMethod.GetCustomAttribute<MagicOrphanAttribute>() != null;
                if (!isSetterOrphaned)
                {
                    HashSet<string> visitedProperties = new HashSet<string>();
                    validSetter = HasValidReferenceFromMethod(setMethod, type, visitedProperties, property.Name);
                    if (!validSetter) setFailed = true;
                }
            }

            return validGetter && validSetter;
        }

        private static bool HasValidReferenceFromMethod(MethodInfo method, Type type, HashSet<string> visitedProperties, string originalPropertyName)
        {
            foreach (var prop in GetAllProperties(type))
            {
                if (prop.Name == originalPropertyName) continue;
                if (visitedProperties.Contains(prop.Name)) continue;

                if (ILMethodCallsProperty(method, prop))
                {
                    if (prop.GetCustomAttribute<MagicFlattenRemoveAttribute>() == null)
                        return true;
                    else
                    {
                        var nestedGetter = prop.GetGetMethod();
                        if (nestedGetter != null)
                        {
                            visitedProperties.Add(prop.Name);
                            if (HasValidReferenceFromMethod(nestedGetter, type, visitedProperties, originalPropertyName))
                                return true;
                        }
                    }
                }
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
