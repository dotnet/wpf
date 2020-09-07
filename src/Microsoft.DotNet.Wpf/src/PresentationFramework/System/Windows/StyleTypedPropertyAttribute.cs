// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;

namespace System.Windows
{
    /// <summary>
    ///     This attribute is applied to the class and determine the target type which should be used for the properties of type Style.
    /// The definition inherits to the subclasses or the derived class can redefine the target type for the property already defined in the base class.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class StyleTypedPropertyAttribute : Attribute
    {
        /// <summary>
        ///     Default constructor
        /// </summary>
        public StyleTypedPropertyAttribute()
        {
        }

        /// <summary>
        ///     The property name of type Style
        /// </summary>
        public string Property
        {
            get { return _property; }
            set { _property = value; }
        }

        /// <summary>
        ///     Target type of the Style that should be used for the Property
        /// </summary>
        public Type StyleTargetType
        {
            get { return _styleTargetType; }
            set { _styleTargetType = value; }
        }

        private string _property;
        private Type _styleTargetType;
    }
}
