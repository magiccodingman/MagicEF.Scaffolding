using Magic.Truth.Toolkit.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MagicEf.Scaffold.Helpers
{
    public static class NameBuilder
    {
        public static string ShareModel(string originalModelName)
        {
            return $"{originalModelName}Map";
        }

        public static string ShareExtensionInterface(string originalModelName)
        {
            return $"I{ShareModel(originalModelName)}";
        }

        public static string ShareTruthInterface(string originalModelName)
        {
            return $"I{ShareModel(originalModelName)}ReadOnly";
        }

        public static string ShareModelMetaDataExtensionName(string originalModelName)
        {
            return $"{ShareModel(originalModelName)}Metadata";
        }

        public static string GetAttributeName(Type attributeType)
        {
            string name = attributeType.Name;

            // Check if it ends with "Attribute" and remove it safely
            return name.EndsWith("Attribute") ? name[..^9] : name;
        }
    }
}
