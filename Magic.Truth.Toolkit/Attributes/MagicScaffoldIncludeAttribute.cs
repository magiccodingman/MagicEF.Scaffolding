using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Magic.Truth.Toolkit.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Method,
                AllowMultiple = false, Inherited = true)]
    public class MagicScaffoldIncludeAttribute : Attribute
    {
    }
}
