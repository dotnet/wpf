// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

namespace System.Windows.Markup
{
    /// <summary>
    /// DependsOnAttribute allows declaring that one property
    /// depends on the value of another property. The serialization
    /// system will ensure that the listed property is serialized
    /// prior to the property that this attribute is set on.
    /// Care must be taken to avoid circular dependencies. They
    /// are only detected when writing all the properties in the
    /// cycle.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = true)]
    [TypeForwardedFrom("WindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35")]
    public sealed class DependsOnAttribute : Attribute
    {
        /// <summary>
        /// Constructor for DependsOnAttribute
        /// </summary>
        /// <param name="name">The name of the property that the property depends on</param>
        public DependsOnAttribute(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Override to ensure AttributeCollection perserves all instances
        /// </summary>
        public override object TypeId => this;

        /// <summary>
        /// The name of the property that is declared to depend on
        /// </summary>
        public string Name { get; }
    }
}
