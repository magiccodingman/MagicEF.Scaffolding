using Magic.Truth.Toolkit.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MagicEf.Scaffold.Helpers
{
    public static class CodeBuilder
    {
        public static string ShareModelMetaDataExtensionCode(string originalModelName)
        {
            string metaDataName = NameBuilder.ShareModelMetaDataExtensionName(originalModelName);
            return $"[MetadataType(typeof({metaDataName}), false)]";
        }

        public static string MagicMapAttributeCode(string originalModelName)
        {
            return $"[{NameBuilder.GetAttributeName(typeof(MagicMapAttribute))}(typeof({$"{NameBuilder.ShareTruthInterface(originalModelName)}"}))]";
        }
    }
}
