using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Magic.Flattening.Toolkit.Attributes
{
    /// <summary>
    /// Removes this variable from being added to the auto generated flat view DTO and the generated interface.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class MagicFlattenRemoveAttribute : Attribute
    {
    }
}