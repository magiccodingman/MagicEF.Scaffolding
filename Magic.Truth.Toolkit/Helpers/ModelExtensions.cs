using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Magic.Truth.Toolkit
{
    public static class ModelExtensions
    {
        public static IEnumerable<TTarget> MagicConvert<TTarget>(this IEnumerable<object> sourceList)
        where TTarget : class, new()
        {
            return sourceList?.Select(item => (TTarget)(object)item) ?? Enumerable.Empty<TTarget>();
        }
    }
}
