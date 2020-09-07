// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//

using System;

namespace System.Windows
{
    /// <summary>
    ///     Style authors should be able to identify the part type used for styling the specific class.
    /// The part is usually required in the style and should have a specific predefined name.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class TemplatePartAttribute : Attribute
    {
        /// <summary>
        ///     Default constructor
        /// </summary>
        public TemplatePartAttribute()
        {
        }

        /// <summary>
        ///     Part name used by the class to indentify required element in the style
        /// </summary>
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        /// <summary>
        ///     Type of the element that should be used as a part with name specified in TemplatePartAttribute.Name
        /// </summary>
        public Type Type
        {
            get { return _type; }
            set { _type = value; }
        }

        private string _name;
        private Type _type;
    }
}
