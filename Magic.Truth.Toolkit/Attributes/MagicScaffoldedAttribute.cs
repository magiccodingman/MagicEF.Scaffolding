using System;
using System.Collections.Generic;
using System.Text;

namespace Magic.Truth.Toolkit.Attributes
{
    public enum ScaffoldMode : byte
    {
        /// <summary>
        /// The entity is fully managed by the Magic Protocol and will be regenerated when scaffolding is run.
        /// </summary>
        ReadOnly = 0,

        /// <summary>
        /// The entity is scaffolded but allows user modifications. The protocol will respect user changes.
        /// </summary>
        Editable = 1,
    }

    /// <summary>
    /// An attribute created to signify that the Magic Protocol has generated this attributed item.
    /// </summary>
    [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = false)]
    public sealed class MagicScaffoldedAttribute : Attribute
    {
        public ScaffoldMode Mode { get; }

        public MagicScaffoldedAttribute(ScaffoldMode mode)
        {
            Mode = mode;
        }
    }
}
