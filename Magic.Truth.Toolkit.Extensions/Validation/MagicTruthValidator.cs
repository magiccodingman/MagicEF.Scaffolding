using Magic.Truth.Toolkit.Attributes;
using Magic.Truth.Toolkit.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Magic.Truth.Toolkit.Extensions.Validation
{
    public static class MagicTruthValidator
    {
        /// <summary>
        /// Validates that all MagicMap attributes were provided a valid MagicTruth interface
        /// </summary>
        /// <returns></returns>
        public static List<ValidationResponse> ValidateMagicMappings()
        {
            var errors = new List<ValidationResponse>();

            // Use HashSet to ensure we only validate an interface once per run
            var uniqueInterfaces = new HashSet<Type>();

            // Find all types with MagicMapAttribute across all loaded assemblies
            var typesWithAttribute = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => type.GetCustomAttribute<MagicMapAttribute>() != null)
                .ToList();

            foreach (var type in typesWithAttribute)
            {
                var attribute = type.GetCustomAttribute<MagicMapAttribute>();
                if (attribute == null) continue;

                var interfaceType = attribute.InterfaceType;

                // Ensure we only check each interface type once
                if (!uniqueInterfaces.Add(interfaceType))
                    continue; // If already added, skip validation

                if (!interfaceType.IsInterface)
                {
                    errors.Add(new ValidationResponse
                    {
                        ClassName = type.FullName,
                        ErrorMessage = $"The provided type '{interfaceType.FullName}' is not an interface."
                    });
                    continue;
                }

                // Ensure interface has MagicTruthAttribute
                if (!interfaceType.GetCustomAttributes(typeof(MagicTruthAttribute), false).Any())
                {
                    errors.Add(new ValidationResponse
                    {
                        ClassName = type.FullName,
                        ErrorMessage = $"The interface '{interfaceType.FullName}' does not have the MagicTruthAttribute."
                    });
                }
            }

            // 🚀 No need to manually "dump memory"—HashSet goes out of scope automatically after the method finishes
            return errors;
        }

        public static List<ValidationResponse> ValidateMagicMapImplementations()
        {
            var errors = new List<ValidationResponse>();

            // Find all classes that have the MagicMapAttribute
            var mappedClasses = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => t.IsClass && t.GetCustomAttribute<MagicMapAttribute>() != null)
                .ToList();

            foreach (var cls in mappedClasses)
            {
                var magicMapAttr = cls.GetCustomAttribute<MagicMapAttribute>();
                if (magicMapAttr == null) continue;

                var expectedInterface = magicMapAttr.InterfaceType;

                // Ensure the expectedInterface is actually implemented by this class
                if (!ImplementsInterfaceRecursively(cls, expectedInterface))
                {
                    errors.Add(new ValidationResponse
                    {
                        ClassName = cls.FullName,
                        ErrorMessage = $"Class '{cls.FullName}' is marked with MagicMap but does not implement '{expectedInterface.FullName}'."
                    });
                }
            }

            return errors;
        }

        public static List<ValidationResponse> ValidateMagicTruthBindings()
        {
            var errors = new List<ValidationResponse>();

            // Find all interfaces that have the MagicTruthAttribute
            var truthInterfaces = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => t.IsInterface && t.GetCustomAttribute<MagicTruthAttribute>() != null)
                .ToList();

            // Dictionary to track which classes are mapped to which MagicTruth interfaces
            var classToTruthMap = new Dictionary<Type, HashSet<Type>>();

            // Iterate through all MagicTruth interfaces
            foreach (var truthInterface in truthInterfaces)
            {
                // Find all classes that implement this interface (directly or indirectly)
                var implementingClasses = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetTypes())
                    .Where(t => t.IsClass && ImplementsInterfaceRecursively(t, truthInterface))
                    .ToList();

                foreach (var cls in implementingClasses)
                {
                    if (!classToTruthMap.ContainsKey(cls))
                    {
                        classToTruthMap[cls] = new HashSet<Type>();
                    }

                    classToTruthMap[cls].Add(truthInterface);
                }
            }

            // Now check for conflicts: If a class is mapped to more than one distinct MagicTruth interface
            foreach (var kvp in classToTruthMap)
            {
                var classType = kvp.Key;
                var connectedTruths = kvp.Value;

                if (connectedTruths.Count > 1)
                {
                    errors.Add(new ValidationResponse
                    {
                        ClassName = classType.FullName,
                        ErrorMessage = $"Class '{classType.FullName}' is connected to multiple MagicTruth interfaces: {string.Join(", ", connectedTruths.Select(t => t.FullName))}. A class can only be connected to one MagicTruth."
                    });
                }
            }

            return errors;
        }

        /// <summary>
        /// Recursively checks if a class or interface implements another interface.
        /// </summary>
        private static bool ImplementsInterfaceRecursively(Type type, Type targetInterface)
        {
            if (type == null || targetInterface == null) return false;

            // Direct implementation
            if (type.GetInterfaces().Contains(targetInterface))
                return true;

            // Recursively check parent interfaces
            foreach (var iface in type.GetInterfaces())
            {
                if (ImplementsInterfaceRecursively(iface, targetInterface))
                    return true;
            }

            // Recursively check base classes
            if (type.BaseType != null)
                return ImplementsInterfaceRecursively(type.BaseType, targetInterface);

            return false;
        }

    }
}
