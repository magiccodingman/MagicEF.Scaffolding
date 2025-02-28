using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Magic.Truth.Toolkit.Attributes
{
    /// <summary>
    /// Attribute placed on the single truth based interface. There can only be a single truth ever 
    /// attached to any one class.
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface, Inherited = false, AllowMultiple = false)]
    public sealed class MagicTruthAttribute : Attribute
    {
        public string Description { get; }

        public MagicTruthAttribute(string description = "")
        {
            Description = description;
        }
    }
}
