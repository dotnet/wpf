// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

namespace System.Windows.Markup
{
    /// <summary>
    /// Attribute to declare that this associated property can be initialized by a 
    /// constructor parameter and should be ignored for serialization if the constructor
    /// with an argument of the supplied name is used to construct the instance. 
    /// </summary>
    [TypeForwardedFrom("WindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35")]
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public sealed class ConstructorArgumentAttribute : Attribute 
    {
        /// <summary>
        /// Constructor for an ConstructorArgumentAttribute
        /// </summary>
        /// <param name="argumentName">Name of the constructor argument that will initialize this property</param>
        public ConstructorArgumentAttribute(string argumentName)
        {
            ArgumentName = argumentName;
        }

        /// <summary>
        /// Name of the constructor argument that will initialize this property
        /// </summary>
        public string ArgumentName { get; }
    }
}
