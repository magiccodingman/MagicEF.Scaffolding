using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Truth.Protocol.Tests.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = false, AllowMultiple = false)]
    internal sealed class ShouldPassAttribute : Attribute
    {
        internal bool ShouldPass { get; set; }

        internal ShouldPassAttribute(bool shouldPass)
        {
            ShouldPass = shouldPass;
        }
    }
}
