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
        // Build opcode lookup arrays for IL parsing
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

        /// <summary>
        /// Validates all types marked with MagicViewDtoAttribute (and not ignored) and returns a list of (error, className, errorMessage) tuples.
        /// </summary>
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

                // Check if all interface properties exist (even if inherited)
                var missingProperties = interfaceProperties
                    .Where(p => !classProperties.Any(cp => cp.Name == p.Name && cp.PropertyType == p.PropertyType))
                    .ToList();

                if (missingProperties.Any())
                {
                    results.Add((type.FullName!, $"Error: {type.FullName} is missing required properties: " +
                        string.Join(", ", missingProperties.Select(p => p.Name))));
                    continue;
                }

                // Strict Validation of MagicFlattenRemove
                foreach (var prop in classProperties)
                {
                    var flattenRemove = prop.GetCustomAttribute<MagicFlattenRemoveAttribute>();
                    if (flattenRemove != null)
                    {
                        var orphaned = prop.GetCustomAttribute<MagicOrphanAttribute>() != null;
                        if (!orphaned)
                        {
                            bool isValid = ValidateFlattenProperty(type, prop);
                            if (!isValid)
                            {
                                results.Add((type.FullName!, $"{type.Name}.{prop.Name} has MagicFlattenRemove but does not map properly to a non-removed property."));
                            }
                        }
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Validates that a property marked with MagicFlattenRemove has both getter and setter that (directly or indirectly)
        /// reference at least one property without MagicFlattenRemove.
        /// </summary>
        private static bool ValidateFlattenProperty(Type type, PropertyInfo property)
        {
            var getMethod = property.GetGetMethod();
            var setMethod = property.GetSetMethod();

            if (getMethod == null || setMethod == null)
                return false;

            // Use separate visited sets for get and set to prevent infinite recursion
            bool validGetter = HasValidReferenceFromMethod(getMethod, type, new HashSet<string>(), property.Name);
            bool validSetter = HasValidReferenceFromMethod(setMethod, type, new HashSet<string>(), property.Name);

            return validGetter && validSetter;
        }

        /// <summary>
        /// Recursively inspects a method’s IL to determine if it eventually calls (directly or via nested calls)
        /// a property accessor for a property that is not marked with MagicFlattenRemove.
        /// </summary>
        private static bool HasValidReferenceFromMethod(MethodInfo method, Type type, HashSet<string> visitedProperties, string originalPropertyName)
        {
            // Iterate through all properties of the type (including inherited ones)
            foreach (var prop in GetAllProperties(type))
            {
                if (prop.Name == originalPropertyName)
                    continue; // Avoid self-reference

                if (visitedProperties.Contains(prop.Name))
                    continue; // Prevent infinite recursion

                // If the IL of this method calls an accessor of this property...
                if (ILMethodCallsProperty(method, prop))
                {
                    // If the property is not removed, then we have a valid mapping.
                    if (prop.GetCustomAttribute<MagicFlattenRemoveAttribute>() == null)
                        return true;
                    else
                    {
                        // Otherwise, recursively check the getter of the removed property.
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

        /// <summary>
        /// Analyzes a method’s IL instructions to see if it calls a property accessor (getter or setter) for the given property.
        /// </summary>
        private static bool ILMethodCallsProperty(MethodInfo method, PropertyInfo property)
        {
            var methodBody = method.GetMethodBody();
            if (methodBody == null)
                return false;

            byte[] ilBytes = methodBody.GetILAsByteArray();
            if (ilBytes == null)
                return false;

            int position = 0;
            while (position < ilBytes.Length)
            {
                OpCode opcode;
                // Read the next opcode (handle multi-byte opcodes)
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

                // If the opcode is a call or callvirt then read the metadata token (4 bytes)
                if (opcode == OpCodes.Call || opcode == OpCodes.Callvirt)
                {
                    int metadataToken = BitConverter.ToInt32(ilBytes, position);
                    position += 4;
                    try
                    {
                        MemberInfo? member = method.Module.ResolveMember(metadataToken);
                        if (member is MethodInfo mi)
                        {
                            // Check if the called method is one of the property's accessors.
                            if (mi == property.GetGetMethod() || mi == property.GetSetMethod())
                                return true;
                        }
                    }
                    catch
                    {
                        // ignore resolution errors
                    }
                }
                else
                {
                    // Skip operand bytes based on the opcode’s operand type.
                    switch (opcode.OperandType)
                    {
                        case OperandType.InlineNone:
                            break;
                        case OperandType.ShortInlineBrTarget:
                        case OperandType.ShortInlineI:
                        case OperandType.ShortInlineVar:
                            position += 1;
                            break;
                        case OperandType.InlineVar:
                            position += 2;
                            break;
                        case OperandType.InlineI:
                        case OperandType.InlineBrTarget:
                        case OperandType.InlineField:
                        case OperandType.InlineMethod:
                        case OperandType.InlineSig:
                        case OperandType.InlineString:
                        case OperandType.InlineTok:
                        case OperandType.InlineType:
                            position += 4;
                            break;
                        case OperandType.InlineI8:
                        case OperandType.InlineR:
                            position += 8;
                            break;
                        case OperandType.ShortInlineR:
                            position += 4;
                            break;
                        default:
                            break;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Retrieves all public instance properties from a class, including inherited ones.
        /// </summary>
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

        /// <summary>
        /// Safely retrieves types from an assembly, even if some fail to load.
        /// </summary>
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
