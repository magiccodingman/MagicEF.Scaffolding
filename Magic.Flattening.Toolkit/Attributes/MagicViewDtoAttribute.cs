using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Magic.Flattening.Toolkit.Attributes
{
    /// <summary>
    /// Allows Magic EF to recognize this as a class desired to be part of the flattening share protocol 
    /// which will auto create your flattened DTO without any auto mapping requirements.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class MagicViewDtoAttribute : Attribute
    {
        public string ProjectName { get => "DataAccess.Primary.Share"; }
        public string? CustomViewDtoName { get; }
        public bool IgnoreWhenFlattening { get; }
        public Type InterfaceType { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="interfaceType">The interface of the end desired connected model (aka the generated ReadOnly interfaces)</param>
        /// <param name="ignoreWhenFlattening">When true, the variable will not be added to the flattened model</param>
        /// <exception cref="ArgumentException"></exception>
        public MagicViewDtoAttribute(Type interfaceType, bool ignoreWhenFlattening = false)
        {
            if (!interfaceType.IsInterface)
                throw new ArgumentException($"The type '{interfaceType.Name}' must be an interface.", nameof(interfaceType));

            InterfaceType = interfaceType;
            IgnoreWhenFlattening = ignoreWhenFlattening;
        }

        /// <summary>
        /// By default the flattened view DTO class name will match the class this attribute is attached to.
        /// </summary>
        /// <param name="interfaceType">The interface of the end desired connected model (aka the generated ReadOnly interfaces)</param>
        /// <param name="customViewDtoName">The desired flattened view DTO class name</param>
        /// <exception cref="ArgumentException"></exception>
        public MagicViewDtoAttribute(Type interfaceType, string customViewDtoName)
        {
            if (!interfaceType.IsInterface)
                throw new ArgumentException($"The type '{interfaceType.Name}' must be an interface.", nameof(interfaceType));

            InterfaceType = interfaceType;
            CustomViewDtoName = customViewDtoName;
            IgnoreWhenFlattening = false;
        }
    }
}
