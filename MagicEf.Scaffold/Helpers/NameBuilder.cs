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

        public static string ShareInterface(string originalModelName)
        {
            return $"I{ShareModel(originalModelName)}";
        }

        public static string ShareModelMetaDataExtension(string originalModelName)
        {
            return $"{ShareModel(originalModelName)}Metadata";
        }
    }
}
