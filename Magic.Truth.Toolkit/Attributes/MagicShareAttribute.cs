using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Magic.Truth.Toolkit.Attributes
{
    /// <summary>
    /// Target class will be added to the share protocol scaffolding process.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class MagicShareAttribute : Attribute
    {
    }
}
