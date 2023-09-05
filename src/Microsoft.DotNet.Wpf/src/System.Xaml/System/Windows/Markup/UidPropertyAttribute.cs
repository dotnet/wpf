// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

namespace System.Windows.Markup
{
    /// <summary>
    /// An attribute that specifies which property the xml:lang value should
    /// be directed to.
    /// Example:
    ///     [UidProperty("Uid")]
    ///     public class ExampleFrameworkElement
    ///
    ///   Means that when the parser sees:
    ///
    ///     <ExampleFrameworkElement x:Uid="efe1">
    ///
    ///   The parser will set the "Uid" property with the value "efe1".
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple=false)]
    [TypeForwardedFrom("WindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35")]
    public sealed class UidPropertyAttribute : Attribute
    {
        /// <summary>
        /// Creates a new UidPropertyAttribute with the given string as
        /// the property name.
        /// </summary>
        public UidPropertyAttribute(string name)
        {
            Name = name;
        }

        /// <summary>
        /// The name of the property that is designated to accept the x:Uid value
        /// </summary>
        public string Name { get; }
    }    
}
