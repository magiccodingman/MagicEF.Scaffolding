﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Magic.Truth.Toolkit.Attributes
{
    /// <summary>
    /// Removes this variable from being added to the flattened DTO interface that'll be created.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class MagicFlattenInterfaceRemoveAttribute : Attribute
    {
    }
}
