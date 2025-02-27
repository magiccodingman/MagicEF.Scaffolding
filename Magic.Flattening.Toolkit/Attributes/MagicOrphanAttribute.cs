using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Magic.Flattening.Toolkit.Attributes
{
    /// <summary>
    /// Purposely orphan this variable, which removes the variable from validation testing
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class MagicOrphanAttribute : Attribute
    {
    }
}
